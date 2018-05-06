using System;
using System.Collections.Generic;
using Nancy;

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
            Post["/request-token"] = args => {
                return User.Create(UnauthenticatedUserLifetime).Token;
            };
        }

        /// <summary>
        /// The amount of time before an unauthenticated user
        /// token expires.
        /// </summary>
        private readonly TimeSpan UnauthenticatedUserLifetime = TimeSpan.FromMinutes(1);
    }
}
