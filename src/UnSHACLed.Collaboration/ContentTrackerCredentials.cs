using System;
using System.Threading.Tasks;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// Data pertaining to this application's role as a content tracker client.
    /// </summary>
    public static class ContentTrackerCredentials
    {
        /// <summary>
        /// Gets the content tracker to use.
        /// </summary>
        /// <returns>The content tracker to use.</returns>
        public static ContentTracker ContentTracker { get; internal set; }

        /// <summary>
        /// Acquires and uses a user client.
        /// </summary>
        /// <param name="userToken">
        /// The token of the user to acquire a client for.
        /// </param>
        /// <param name="use">
        /// A function that takes a client and produces a task.
        /// </param>
        /// <typeparam name="T">
        /// The type of value to produce.
        /// </typeparam>
        /// <returns>
        /// A task that produces a value.
        /// </returns>
        public static Task<T> UseClientAsync<T>(OauthToken userToken, Func<GitHubClient, Task<T>> use)
        {
            var userClient = new GitHubClient(new ProductHeaderValue("UnSHACLed"));
            userClient.Credentials = new Credentials(userToken.AccessToken);
            return use(userClient);
        }
    }
}
