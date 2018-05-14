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
            RegisterContentTrackerGet("/login/{token}", (args, user, client) => client.GetLogin());
            RegisterContentTrackerGet("/name/{token}", (args, user, client) => client.GetName());
            RegisterContentTrackerGet("/email/{token}", (args, user, client) => client.GetEmail());

            RegisterGitHubGet("/repo-list/{token}", async (args, user, client) =>
            {
                var allRepos = await client.Repository.GetAllForCurrent(
                    new RepositoryRequest { Affiliation = RepositoryAffiliation.All });
                return allRepos.Select(repo => repo.FullName).ToArray();
            });
        }
    }
}
