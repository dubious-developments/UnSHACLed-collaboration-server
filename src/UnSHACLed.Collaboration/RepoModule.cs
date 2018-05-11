using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A Nancy module that provides information about GitHub repositories.
    /// </summary>
    public class RepoModule : GitHubAccessModule
    {
        public RepoModule()
            : base("repo")
        {
            this.lockDictionary = new Dictionary<string, User>();

            RegisterGitHubGet<dynamic>(
                "/file/{owner}/{repoName}/{token}/{filePath}",
                async (args, client) =>
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

            RegisterUserGet<bool>(
                "/lock/{owner}/{repoName}/{token}/{filePath}",
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
                    if (lockOwner.Token == user.Token)
                    {
                        return Task.FromResult(HttpStatusCode.BadRequest);
                    }
                    else
                    {
                        lockDictionary[CreateLockName(repoOwner, repoName, filePath)] = null;
                        return Task.FromResult(HttpStatusCode.OK);
                    }
                }
            });
        }

        private Dictionary<string, User> lockDictionary;

        private static string CreateLockName(
            string repoOwner,
            string repoName,
            string filePath)
        {
            return repoOwner + "/" + repoName + "/" + filePath;
        }

        private User GetLockOwner(
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
