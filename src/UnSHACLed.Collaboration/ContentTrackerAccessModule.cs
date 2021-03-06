using System;
using System.Threading.Tasks;
using Nancy;
using Octokit;
using Pixie;
using Pixie.Markup;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A base class for modules that have access to the
    /// content tracker.
    /// </summary>
    public abstract class ContentTrackerAccessModule : NancyModule
    {
        protected ContentTrackerAccessModule(string moduleName)
            : base(moduleName)
        { }

        /// <summary>
        /// Registers a GET API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useClient">
        /// A function that uses a content tracker client.
        /// </param>
        protected void RegisterContentTrackerGet<T>(
            string apiRoute,
            Func<dynamic, User, ContentTrackerClient, Task<T>> useClient)
        {
            RegisterContentTrackerApi<T>(Get, apiRoute, useClient);
        }

        /// <summary>
        /// Registers a PUT API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useClient">
        /// A function that uses a content tracker client.
        /// </param>
        protected void RegisterContentTrackerPut<T>(
            string apiRoute,
            Func<dynamic, User, ContentTrackerClient, Task<T>> useClient)
        {
            RegisterContentTrackerApi<T>(Put, apiRoute, useClient);
        }

        /// <summary>
        /// Registers a POST API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useClient">
        /// A function that uses a content tracker client.
        /// </param>
        protected void RegisterContentTrackerPost<T>(
            string apiRoute,
            Func<dynamic, User, ContentTrackerClient, Task<T>> useClient)
        {
            RegisterContentTrackerApi<T>(Post, apiRoute, useClient);
        }

        /// <summary>
        /// Registers a GET API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useUser">
        /// A function that uses a user instance.
        /// </param>
        protected void RegisterUserGet<T>(
            string apiRoute,
            Func<dynamic, User, Task<T>> useUser)
        {
            RegisterUserApi<T>(Get, apiRoute, useUser);
        }

        /// <summary>
        /// Registers a PUT API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useUser">
        /// A function that uses a user instance.
        /// </param>
        protected void RegisterUserPut<T>(
            string apiRoute,
            Func<dynamic, User, Task<T>> useUser)
        {
            RegisterUserApi<T>(Put, apiRoute, useUser);
        }

        /// <summary>
        /// Registers a POST API with the module.
        /// </summary>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useUser">
        /// A function that uses a user instance.
        /// </param>
        protected void RegisterUserPost<T>(
            string apiRoute,
            Func<dynamic, User, Task<T>> useUser)
        {
            RegisterUserApi<T>(Post, apiRoute, useUser);
        }

        /// <summary>
        /// Registers an API with the module.
        /// </summary>
        /// <param name="routeBuilder">
        /// The route builder to register a route with.
        /// </param>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useUser">
        /// A function that uses the user.
        /// </param>
        private void RegisterUserApi<T>(
            RouteBuilder routeBuilder,
            string apiRoute,
            Func<dynamic, User, Task<T>> useUser)
        {
            routeBuilder[apiRoute, true] = async (args, ct) =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    Program.GlobalLog.Log(
                        new LogEntry(
                            Severity.Warning,
                            "malformed request",
                            Quotation.QuoteEvenInBold(
                                "token ",
                                args.token,
                                " does not exist.")));
                    return HttpStatusCode.BadRequest;
                }
                else if (user.IsAuthenticated)
                {
                    return await useUser(args, user);
                }
                else
                {
                    return HttpStatusCode.Unauthorized;
                }
            };
        }

        /// <summary>
        /// Registers an API with the module.
        /// </summary>
        /// <param name="routeBuilder">
        /// The route builder to register a route with.
        /// </param>
        /// <param name="apiRoute">
        /// The route to register.
        /// </param>
        /// <param name="useClient">
        /// A function that uses the client.
        /// </param>
        private void RegisterContentTrackerApi<T>(
            RouteBuilder routeBuilder,
            string apiRoute,
            Func<dynamic, User, ContentTrackerClient, Task<T>> useClient)
        {
            RegisterUserApi<T>(
                routeBuilder,
                apiRoute,
                (args, user) =>
                    user.ContentTrackerToken.UseClient<T>(client =>
                        useClient(args, user, client)));
        }
    }
}
