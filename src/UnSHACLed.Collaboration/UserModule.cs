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
    public class UserModule : ContentTrackerAccessModule
    {
        public UserModule()
            : base("user")
        {
            RegisterContentTrackerGet("/login/{token}", (args, user, client) => client.GetLogin());
            RegisterContentTrackerGet("/name/{token}", (args, user, client) => client.GetName());
            RegisterContentTrackerGet("/email/{token}", (args, user, client) => client.GetEmail());
            RegisterContentTrackerGet("/repo-list/{token}", (args, user, client) => client.GetRepositoryNames());
        }
    }
}
