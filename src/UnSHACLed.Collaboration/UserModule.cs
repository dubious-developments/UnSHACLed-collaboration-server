using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A Nancy module that provides information about authenticated users.
    /// </summary>
    public class UserModule : GitHubAccessModule
    {
        public UserModule()
            : base("user")
        {
            RegisterGetUserData("/login/{token}", user => user.Login);
            RegisterGetUserData("/name/{token}", user => user.Name);
            RegisterGetUserData("/email/{token}", user => user.Email);

            RegisterGitHubGet("/repo-list/{token}", async client =>
            {
                var allRepos = new List<Repository>();
                allRepos.AddRange(await client.Repository.GetAllForCurrent());

                var allOrgs = await client.Organization.GetAllForCurrent();
                var orgRepoLists = await Task.WhenAll(
                    allOrgs.Select(org => client.Repository.GetAllForOrg(org.Name)));

                foreach (var repoList in orgRepoLists)
                {
                    allRepos.AddRange(repoList);
                }

                return allRepos.Select(repo => repo.FullName).ToArray();
            });
        }

        private void RegisterGetUserData<T>(string apiRoute, Func<Octokit.User, T> mapUser)
        {
            RegisterGitHubGet(apiRoute, async client =>
            {
                var ghUser = await client.User.Current();
                return mapUser(ghUser);
            });
        }
    }
}
