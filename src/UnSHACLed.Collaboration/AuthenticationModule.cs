using System;
using System.Collections.Generic;
using Nancy;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A Nancy module that authenticates users.
    /// </summary>
    public class AuthenticationModule : NancyModule
    {
        public AuthenticationModule()
            : base("auth")
        {
            Post["/request-token"] = args =>
            {
                return User.Create(UnauthenticatedUserLifetime).Token;
            };

            Get["/auth/{token}"] = args =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    return HttpStatusCode.BadRequest;
                }

                var request = new OauthLoginRequest(GitHubClientData.ClientId)
                {
                    RedirectUri = new Uri(GitHubClientData.Domain, "/auth/after-auth/" + user.Token)
                };

                return GitHubClientData.Client.Oauth.GetGitHubLoginUrl(request);
            };

            Post["/after-auth/{token}"] = args =>
            {
                return HttpStatusCode.OK;
            };
        }

        /// <summary>
        /// The amount of time before an unauthenticated user
        /// token expires.
        /// </summary>
        private readonly TimeSpan UnauthenticatedUserLifetime = TimeSpan.FromMinutes(1);
    }
}
