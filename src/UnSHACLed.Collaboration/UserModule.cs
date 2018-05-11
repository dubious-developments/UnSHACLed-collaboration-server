using System;
using System.Collections.Generic;
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
        }

        private void RegisterGetUserData<T>(string apiRoute, Func<Octokit.User, T> mapUser)
        {
            RegisterGitHubGet(apiRoute, async (client) => {
                var ghUser = await client.User.Current();
                return mapUser(ghUser);
            });
        }
    }
}
