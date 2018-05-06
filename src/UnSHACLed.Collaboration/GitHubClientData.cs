using System;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// Data pertaining to this application's role as a GitHub client.
    /// </summary>
    public static class GitHubClientData
    {
        /// <summary>
        /// Gets the main domain where the application is hosted.
        /// </summary>
        /// <returns>The domain.</returns>
        public static Uri Domain { get; private set; }

        /// <summary>
        /// Gets the client ID used by the application.
        /// </summary>
        /// <returns>The client ID.</returns>
        public static string ClientId { get; private set; }

        /// <summary>
        /// Gets the client secret of the application.
        /// </summary>
        /// <returns>The client secret.</returns>
        public static string ClientSecret { get; private set; }

        /// <summary>
        /// Gets the GitHub client to use.
        /// </summary>
        /// <returns>The GitHub client.</returns>
        public static GitHubClient Client { get; private set; }

        /// <summary>
        /// Configures the application's GitHub client.
        /// </summary>
        /// <param name="domain">The main domain where the application is hosted.</param>
        /// <param name="clientId">The client ID of the application.</param>
        /// <param name="clientSecret">The client secret of the application.</param>
        public static void Configure(Uri domain, string clientId, string clientSecret)
        {
            Domain = domain;
            ClientId = clientId;
            ClientSecret = clientSecret;
            Client = new GitHubClient(new ProductHeaderValue("UnSHACLed"));
        }
    }
}
