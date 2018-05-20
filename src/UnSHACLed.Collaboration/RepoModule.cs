using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.Extensions;
using Octokit;
using Pixie;
using Pixie.Markup;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A Nancy module that allows clients to access and
    /// manipulate GitHub repositories.
    /// </summary>
    public class RepoModule : ContentTrackerAccessModule
    {
        public RepoModule()
            : base("repo")
        {
            RegisterContentTrackerGet<dynamic>(
                "/file/{owner}/{repoName}/{token}/{filePath}",
                async (args, user, client) =>
            {
                string repoOwner = args.owner;
                string repoName = args.repoName;
                string filePath = args.filePath;

                try
                {
                    return await client.GetFileContents(
                        repoOwner, repoName, filePath);
                }
                catch (ContentTrackerException)
                {
                    Program.GlobalLog.Log(
                        new LogEntry(
                            Severity.Warning,
                            "malformed request",
                            Quotation.QuoteEvenInBold(
                                "request ",
                                "GET " + Request.Url.ToString(),
                                " tried to read an entity that is not a file.")));
                    return HttpStatusCode.BadRequest;
                }
            });

            RegisterContentTrackerPut<dynamic>(
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
                        Program.GlobalLog.Log(
                            new LogEntry(
                                Severity.Warning,
                                "malformed request",
                                Quotation.QuoteEvenInBold(
                                    "request ",
                                    "PUT " + Request.Url.ToString(),
                                    " tried to update a file but didn't hold a lock.")));
                        return HttpStatusCode.BadRequest;
                    }
                }

                string newFileContents = Request.Body.AsString();

                try
                {
                    bool createdNewFile = await client.SetFileContents(
                        repoOwner,
                        repoName,
                        filePath,
                        newFileContents);

                    UpdateFileChangedTimestamp(repoOwner, repoName, filePath);
                    return createdNewFile
                        ? HttpStatusCode.Created
                        : HttpStatusCode.OK;
                }
                catch (ContentTrackerException)
                {
                    Program.GlobalLog.Log(
                        new LogEntry(
                            Severity.Warning,
                            "malformed request",
                            Quotation.QuoteEvenInBold(
                                "request ",
                                "PUT " + Request.Url.ToString(),
                                " tried to update an entity that is a file.")));
                    return HttpStatusCode.BadRequest;
                }
            });

            RegisterContentTrackerGet<dynamic>(
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
                    try
                    {
                        response["contents"] = await client.GetFileContents(
                            repoOwner, repoName, filePath);
                        return response;
                    }
                    catch (ContentTrackerException)
                    {
                        return HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    return response;
                }
            });

            RegisterContentTrackerGet<dynamic>(
                "/file-names/{owner}/{repoName}/{token}",
                async (args, user, client) =>
            {
                string repoOwner = args.owner;
                string repoName = args.repoName;

                return await client.GetFileNames(repoOwner, repoName);
            });

            RegisterContentTrackerPost(
                "/create-repo/{repoName}/{token}",
                (args, user, client) =>
            {
                string repoName = args.repoName;
                return client.CreateRepository(repoName);
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
                    if (lockOwner == null || !lockOwner.IsActive)
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
                    changeTimestamp = previousTimestamp;
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
