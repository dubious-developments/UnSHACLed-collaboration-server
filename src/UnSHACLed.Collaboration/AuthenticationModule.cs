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

            // Configure authentication workflow.
            ContentTrackerCredentials.ContentTracker.ConfigureAuthenticationModule(this);
        }

        /// <summary>
        /// The amount of time before an unauthenticated user
        /// token expires.
        /// </summary>
        public static readonly TimeSpan UnauthenticatedUserLifetime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The amount of time before an authenticated user
        /// token expires.
        /// </summary>
        public static readonly TimeSpan AuthenticatedUserLifetime = TimeSpan.FromHours(2);
    }
}
