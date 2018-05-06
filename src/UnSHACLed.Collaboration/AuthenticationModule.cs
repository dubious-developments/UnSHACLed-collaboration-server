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

            Get["/is-authenticated/{token}"] = args =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    return HttpStatusCode.BadRequest;
                }

                return user.IsAuthenticated;
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
                    RedirectUri = new Uri(GitHubClientData.Domain, "auth/after-auth/" + user.Token)
                };

                return Response.AsRedirect(GitHubClientData.Client.Oauth.GetGitHubLoginUrl(request).AbsoluteUri);
            };

            Post["/after-auth/{token}?code={code}", true] = async (args, ct) =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    // Eh, well.
                    return HttpStatusCode.OK;
                }

                // Extend the user's lifetime so they don't die on us.
                user.ExtendLifetime(AuthenticatedUserLifetime);

                // Request an OAuth token.
                var request = new OauthTokenRequest(
                    GitHubClientData.ClientId,
                    GitHubClientData.ClientSecret,
                    args.code);

                // Store it.
                user.GitHubToken = await GitHubClientData.Client.Oauth.CreateAccessToken(request);

                return HttpStatusCode.OK;
            };
        }

        /// <summary>
        /// The amount of time before an unauthenticated user
        /// token expires.
        /// </summary>
        private readonly TimeSpan UnauthenticatedUserLifetime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The amount of time before an authenticated user
        /// token expires.
        /// </summary>
        private readonly TimeSpan AuthenticatedUserLifetime = TimeSpan.FromHours(2);
    }
}
