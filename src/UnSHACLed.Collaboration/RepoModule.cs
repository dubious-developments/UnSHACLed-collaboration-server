using System;
using System.Linq;
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
            RegisterGitHubGet<dynamic>(
                "/file/{owner}/{repoName}/{token}?file={filePath}",
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
        }
    }
}
