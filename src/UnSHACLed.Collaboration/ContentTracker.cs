using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nancy;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A common interface for content trackers.
    /// </summary>
    public abstract class ContentTracker
    {
        /// <summary>
        /// Configures a Nancy module to offer authentication services
        /// for this particular content tracker.
        /// </summary>
        /// <param name="module">The module to configure.</param>
        public abstract void ConfigureAuthenticationModule(NancyModule module);
    }

    /// <summary>
    /// A content tracker authentication token.
    /// </summary>
    public abstract class ContentTrackerToken
    {
        /// <summary>
        /// Runs a function that uses a client.
        /// </summary>
        /// <param name="useClient">
        /// A function that uses a content tracker client
        /// associated with this token.
        /// </param>
        /// <typeparam name="T">
        /// The type of value produced by <paramref name="useClient"/>.
        /// </typeparam>
        /// <returns>
        /// A task that produces a value.
        /// </returns>
        public abstract Task<T> UseClient<T>(Func<ContentTrackerClient, Task<T>> useClient);
    }

    /// <summary>
    /// An authenticated client for a content tracker.
    /// </summary>
    public abstract class ContentTrackerClient
    {
        /// <summary>
        /// Gets the authenticated user's login.
        /// </summary>
        /// <returns>A task that produces the user's login.</returns>
        public abstract Task<string> GetLogin();

        /// <summary>
        /// Gets the authenticated user's name.
        /// </summary>
        /// <returns>A task that produces the user's name.</returns>
        public abstract Task<string> GetName();

        /// <summary>
        /// Gets the authenticated user's email.
        /// </summary>
        /// <returns>A task that produces the user's email.</returns>
        public abstract Task<string> GetEmail();

        /// <summary>
        /// Gets a list of the full names of all repositories
        /// associated with the authenticated user.
        /// </summary>
        /// <returns>A list of all repository names.</returns>
        public abstract Task<IReadOnlyList<string>> GetRepositoryNames();
    }
}
