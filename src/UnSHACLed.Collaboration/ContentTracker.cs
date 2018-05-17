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

        /// <summary>
        /// Gets a list of all files stored in a repository.
        /// </summary>
        /// <param name="repoOwner">
        /// The login of the repository's owner.
        /// </param>
        /// <param name="repoName">
        /// The name of the repository.
        /// </param>
        /// <returns>
        /// A task that produces a list of file names.
        /// </returns>
        public abstract Task<IReadOnlyList<string>> GetFileNames(
            string repoOwner,
            string repoName);

        /// <summary>
        /// Fetches a file's contents.
        /// </summary>
        /// <param name="repoOwner">
        /// The owner of the repository the file lives in.
        /// </param>
        /// <param name="repoName">
        /// The name of the repository the file lives in.
        /// </param>
        /// <param name="filePath">
        /// The path to the file in the repository.
        /// </param>
        /// <returns>
        /// A task that produces the file's contents.
        /// Throws an exception if something goes wrong.
        /// </returns>
        public abstract Task<string> GetFileContents(
            string repoOwner,
            string repoName,
            string filePath);

        /// <summary>
        /// Sets a file's contents. Creates the file if it does
        /// not exist already.
        /// </summary>
        /// <param name="repoOwner">
        /// The owner of the repository the file lives in.
        /// </param>
        /// <param name="repoName">
        /// The name of the repository the file lives in.
        /// </param>
        /// <param name="filePath">
        /// The path to the file in the repository.
        /// </param>
        /// <param name="contents">
        /// The contents to assign to the file.
        /// </param>
        /// <returns>
        /// A task that returns <c>true</c> if a new file was
        /// created; otherwise, a task that returns <c>false</c>.
        /// Throws an exception if something goes wrong.
        /// </returns>
        public abstract Task<bool> SetFileContents(
            string repoOwner,
            string repoName,
            string filePath,
            string contents);
    }
}
