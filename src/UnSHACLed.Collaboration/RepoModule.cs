using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A Nancy module that allows clients to access and
    /// manipulate GitHub repositories.
    /// </summary>
    public class RepoModule : GitHubAccessModule
    {
        public RepoModule()
            : base("repo")
        {
            RegisterGitHubGet<dynamic>(
                "/file/{owner}/{repoName}/{token}/{filePath}",
                async (args, user, client) =>
            {
                string repoOwner = args.owner;
                string repoName = args.repoName;
                string filePath = args.filePath;
                var contents = await client.Repository.Content.GetAllContents(
                    repoOwner, repoName, filePath);

                if (contents.Count == 1)
                {
                    return contents[0].Content;
                }
                else
                {
                    return HttpStatusCode.BadRequest;
                }
            });

            RegisterGitHubPut<dynamic>(
                "/file/{owner}/{repoName}/{token}/{filePath}",
                async (args, user, client) =>
            {
                string repoOwner = args.owner;
                string repoName = args.repoName;
                string filePath = args.filePath;
                lock (lockDictionary)
                {
                    var lockOwner = GetLockOwner(repoOwner, repoName, filePath);
                    if (lockOwner != user)
                    {
                        return HttpStatusCode.BadRequest;
                    }
                }

                string newFileContents = Request.Body.AsString();

                try
                {
                    var contents = await client.Repository.Content.GetAllContents(
                        repoOwner, repoName, filePath);
                    if (contents.Count == 1)
                    {
                        await client.Repository.Content.UpdateFile(
                            repoOwner,
                            repoName,
                            filePath,
                            new UpdateFileRequest(
                                "Update file '" + filePath + "'",
                                newFileContents,
                                contents[0].Sha));
                        UpdateFileChangedTimestamp(repoOwner, repoName, filePath);
                        return HttpStatusCode.OK;
                    }
                    else
                    {
                        return HttpStatusCode.BadRequest;
                    }
                }
                catch (NotFoundException)
                {
                    await client.Repository.Content.CreateFile(
                        repoOwner,
                        repoName,
                        filePath,
                        new CreateFileRequest(
                            "Create file '" + filePath + "'",
                            newFileContents));
                    UpdateFileChangedTimestamp(repoOwner, repoName, filePath);
                    return HttpStatusCode.Created;
                }
            });

            RegisterGitHubGet<dynamic>(
                "/poll-file/{owner}/{repoName}/{token}/{filePath}",
                async (args, user, client) =>
            {
                string repoOwner = args.owner;
                string repoName = args.repoName;
                string filePath = args.filePath;

                string timestampStr = Request.Body.AsString();
                var prevTimestamp = string.IsNullOrWhiteSpace(timestampStr)
                    ? DateTime.MinValue
                    : DateTime.Parse(timestampStr);

                DateTime changeTimestamp;
                bool isModified = HasFileChanged(
                    repoOwner,
                    repoName,
                    filePath,
                    prevTimestamp,
                    out changeTimestamp);

                var response = new Dictionary<string, object>();
                response["isModified"] = isModified;
                response["lastChange"] = changeTimestamp;
                if (isModified)
                {
                    var contents = await client.Repository.Content.GetAllContents(
                    repoOwner, repoName, filePath);

                    if (contents.Count == 1)
                    {
                        response["contents"] = contents[0].Content;
                        return response;
                    }
                    else
                    {
                        return HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    return response;
                }
            });

            RegisterUserGet<bool>(
                "/has-lock/{owner}/{repoName}/{token}/{filePath}",
                (args, user) =>
            {
                string repoOwner = args.owner;
                string repoName = args.repoName;
                string filePath = args.filePath;

                lock (lockDictionary)
                {
                    var lockOwner = GetLockOwner(repoOwner, repoName, filePath);
                    return Task.FromResult(
                        lockOwner != null && lockOwner.Token == user.Token);
                }
            });

            RegisterUserPost<bool>(
                "/request-lock/{owner}/{repoName}/{token}/{filePath}",
                (args, user) =>
            {
                string repoOwner = args.owner;
                string repoName = args.repoName;
                string filePath = args.filePath;

                lock (lockDictionary)
                {
                    var lockOwner = GetLockOwner(repoOwner, repoName, filePath);
                    if (lockOwner == null)
                    {
                        lockDictionary[CreateFileKey(repoOwner, repoName, filePath)] = user;
                        return Task.FromResult(true);
                    }
                    else if (lockOwner.Token == user.Token)
                    {
                        return Task.FromResult(true);
                    }
                    else
                    {
                        return Task.FromResult(false);
                    }
                }
            });

            RegisterUserPost<HttpStatusCode>(
                "/relinquish-lock/{owner}/{repoName}/{token}/{filePath}",
                (args, user) =>
            {
                string repoOwner = args.owner;
                string repoName = args.repoName;
                string filePath = args.filePath;

                lock (lockDictionary)
                {
                    var lockOwner = GetLockOwner(repoOwner, repoName, filePath);
                    if (lockOwner == null || lockOwner.Token == user.Token)
                    {
                        lockDictionary.Remove(CreateFileKey(repoOwner, repoName, filePath));
                        return Task.FromResult(HttpStatusCode.OK);
                    }
                    else
                    {
                        return Task.FromResult(HttpStatusCode.BadRequest);
                    }
                }
            });
        }

        private static readonly Dictionary<string, User> lockDictionary =
            new Dictionary<string, User>();

        private static readonly Dictionary<string, DateTime> fileChangeTimestamps =
            new Dictionary<string, DateTime>();

        private static readonly ReaderWriterLockSlim fileChangeLock
            = new ReaderWriterLockSlim();

        private static string CreateFileKey(
            string repoOwner,
            string repoName,
            string filePath)
        {
            return repoOwner + "/" + repoName + "/" + filePath;
        }

        private static User GetLockOwner(
            string repoOwner,
            string repoName,
            string filePath)
        {
            string key = CreateFileKey(repoOwner, repoName, filePath);
            User owner;
            if (lockDictionary.TryGetValue(key, out owner))
            {
                if (owner.IsActive)
                {
                    return owner;
                }
                else
                {
                    lockDictionary[key] = null;
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private static DateTime UpdateFileChangedTimestamp(
            string repoOwner,
            string repoName,
            string filePath)
        {
            string key = CreateFileKey(repoOwner, repoName, filePath);
            var timestamp = DateTime.Now;
            try
            {
                fileChangeLock.EnterWriteLock();
                fileChangeTimestamps[key] = timestamp;
            }
            finally
            {
                fileChangeLock.ExitWriteLock();
            }
            return timestamp;
        }

        private static bool HasFileChanged(
            string repoOwner,
            string repoName,
            string filePath,
            DateTime previousTimestamp,
            out DateTime changeTimestamp)
        {
            string key = CreateFileKey(repoOwner, repoName, filePath);
            try
            {
                fileChangeLock.EnterReadLock();
                if (fileChangeTimestamps.TryGetValue(key, out changeTimestamp))
                {
                    return changeTimestamp > previousTimestamp;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                fileChangeLock.ExitReadLock();
            }
        }
    }
}
