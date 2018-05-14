using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A content tracker implementation based on the GitHub API.
    /// </summary>
    public sealed class GitHubContentTracker : ContentTracker
    {
        /// <summary>
        /// Creates a GitHub content tracker implementation.
        /// </summary>
        /// <param name="domain">The main domain where the application is hosted.</param>
        /// <param name="clientId">The client ID of the application.</param>
        /// <param name="clientSecret">The client secret of the application.</param>
        public GitHubContentTracker(Uri domain, string clientId, string clientSecret)
        {
            Domain = domain;
            ClientId = clientId;
            ClientSecret = clientSecret;
            authClient = new GitHubClient(new ProductHeaderValue("UnSHACLed"));
        }

        /// <summary>
        /// Gets the main domain where the application is hosted.
        /// </summary>
        /// <returns>The domain.</returns>
        public Uri Domain { get; private set; }

        /// <summary>
        /// Gets the client ID used by the application.
        /// </summary>
        /// <returns>The client ID.</returns>
        public string ClientId { get; private set; }

        /// <summary>
        /// Gets the client secret of the application.
        /// </summary>
        /// <returns>The client secret.</returns>
        public string ClientSecret { get; private set; }

        /// <summary>
        /// The GitHub client to use for authenticating users.
        /// </summary>
        /// <returns>The authentication GitHub client.</returns>
        private GitHubClient authClient;

        /// <inheritdoc/>
        public override void ConfigureAuthenticationModule(NancyModule module)
        {
            module.Get["/auth/{token}"] = args =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    return HttpStatusCode.BadRequest;
                }

                var request = new OauthLoginRequest(ClientId)
                {
                    RedirectUri = new Uri(Domain, "auth/after-auth/" + user.Token)
                };
                request.Scopes.Add("repo");
                request.Scopes.Add("user:email");

                return module.Response.AsRedirect(authClient.Oauth.GetGitHubLoginUrl(request).AbsoluteUri);
            };

            module.Get["/after-auth/{token}", true] = async (args, ct) =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    // Eh, well.
                    return HtmlHelpers.CreateHtmlPage(
                        "Session expired",
                        "<h1>Oops.</h1> <div>Something went wrong. Your session token probably expired. " +
                        "Trying again might work.</div>");
                }

                // Extend the user's lifetime so they don't die on us.
                user.ExtendLifetime(AuthenticationModule.AuthenticatedUserLifetime);

                // Request an OAuth token.
                var request = new OauthTokenRequest(
                    ClientId,
                    ClientSecret,
                    module.Request.Query.code);

                // Store it.
                user.GitHubToken = await authClient.Oauth.CreateAccessToken(request);

                return HtmlHelpers.CreateHtmlPage(
                    "Authentication successful",
                    "<h1>You did it!</h1> <div>Yay! You successfully managed to authenticate! 🎉</div>");
            };
        }
    }

    /// <summary>
    /// A content tracker token implementation for the GitHub API.
    /// </summary>
    public sealed class GitHubContentTrackerToken : ContentTrackerToken
    {
        /// <summary>
        /// Creates a GitHub content tracker token.
        /// </summary>
        /// <param name="token">A GitHub API OAuth token.</param>
        public GitHubContentTrackerToken(OauthToken token)
        {
            this.Token = token;
        }

        /// <summary>
        /// Gets the GitHub OAuth token wrapped by this content tracker token.
        /// </summary>
        /// <returns>An OAuth token.</returns>
        public OauthToken Token { get; private set; }

        /// <inheritdoc/>
        public override Task<T> UseClient<T>(Func<ContentTrackerClient, Task<T>> use)
        {
            var userClient = new GitHubClient(new ProductHeaderValue("UnSHACLed"));
            userClient.Credentials = new Credentials(Token.AccessToken);
            return use(new GitHubContentTrackerClient(userClient));
        }
    }

    /// <summary>
    /// A generic content tracker client that wraps a GitHub API client.
    /// </summary>
    public sealed class GitHubContentTrackerClient : ContentTrackerClient
    {
        /// <summary>
        /// Creates a content tracker client for the GitHub API.
        /// </summary>
        /// <param name="client">The GitHub API client to wrap.</param>
        public GitHubContentTrackerClient(GitHubClient client)
        {
            this.Client = client;
        }

        /// <summary>
        /// Gets the GitHub API client wrapped by this content tracker client.
        /// </summary>
        /// <returns>A GitHub API client.</returns>
        public GitHubClient Client { get; private set; }

        private async Task<T> GetUserInfo<T>(Func<Octokit.User, T> query)
        {
            var user = await Client.User.Current();
            return query(user);
        }

        /// <inheritdoc/>
        public override Task<string> GetEmail()
        {
            return GetUserInfo(user => user.Email);
        }

        /// <inheritdoc/>
        public override Task<string> GetLogin()
        {
            return GetUserInfo(user => user.Login);
        }

        /// <inheritdoc/>
        public override Task<string> GetName()
        {
            return GetUserInfo(user => user.Name);
        }

        /// <inheritdoc/>
        public override async Task<IReadOnlyList<string>> GetRepositoryNames()
        {
            var allRepos = await Client.Repository.GetAllForCurrent(
                new RepositoryRequest { Affiliation = RepositoryAffiliation.All });
            return allRepos.Select(repo => repo.FullName).ToArray();
        }
    }
}
