using System;
using System.Threading.Tasks;
using Nancy;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A base class for modules that have GitHub access.
    /// </summary>
    public abstract class GitHubAccessModule : NancyModule
    {
        protected GitHubAccessModule(string moduleName)
            : base(moduleName)
        { }

        /// <summary>
        /// Registers a GET API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useClient">
        /// A function that uses the client.
        /// </param>
        protected void RegisterGitHubGet<T>(
            string apiRoute,
            Func<dynamic, GitHubClient, Task<T>> useClient)
        {
            RegisterGitHubApi<T>(Get, apiRoute, useClient);
        }

        /// <summary>
        /// Registers a PUT API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useClient">
        /// A function that uses the client.
        /// </param>
        protected void RegisterGitHubPut<T>(
            string apiRoute,
            Func<dynamic, GitHubClient, Task<T>> useClient)
        {
            RegisterGitHubApi<T>(Put, apiRoute, useClient);
        }

        /// <summary>
        /// Registers an API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useClient">
        /// A function that uses the client.
        /// </param>
        private void RegisterGitHubApi<T>(
            RouteBuilder routeBuilder,
            string apiRoute,
            Func<dynamic, GitHubClient, Task<T>> useClient)
        {
            routeBuilder[apiRoute, true] = async (args, ct) =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    return HttpStatusCode.BadRequest;
                }
                else if (user.IsAuthenticated)
                {
                    return await GitHubClientData.UseClientAsync<T>(
                        user.GitHubToken,
                        client => useClient(args, client));
                }
                else
                {
                    return HttpStatusCode.Unauthorized;
                }
            };
        }
    }
}
