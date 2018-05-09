using System;
using System.Collections.Generic;
using Nancy;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A Nancy module that provides information about authenticated users.
    /// </summary>
    public class UserModule : NancyModule
    {
        public UserModule()
            : base("user")
        {
            RegisterGetUserData("login", user => user.Login);
            RegisterGetUserData("name", user => user.Name);
            RegisterGetUserData("email", user => user.Email);
        }

        private void RegisterGetUserData<T>(string apiName, Func<Octokit.User, T> mapUser)
        {
            Get["/" + apiName + "/{token}", true] = async (args, ct) =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    return HttpStatusCode.BadRequest;
                }
                else if (!user.IsAuthenticated)
                {
                    return HttpStatusCode.Unauthorized;
                }

                var ghUser = await GitHubClientData.UseClientAsync(
                    user.GitHubToken,
                    client => client.User.Current());
                return mapUser(ghUser);
            };
        }
    }
}
