using System.Collections.Generic;
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
                    return HttpStatusCode.Created;
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
                        lockDictionary[CreateLockName(repoOwner, repoName, filePath)] = user;
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
                        lockDictionary.Remove(CreateLockName(repoOwner, repoName, filePath));
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

        private static string CreateLockName(
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
            string key = CreateLockName(repoOwner, repoName, filePath);
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
    }
}
