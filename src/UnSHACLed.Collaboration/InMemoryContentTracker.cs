using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nancy;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// A content tracker that stores content in memory.
    /// Design primarily for testing purposes.
    /// </summary>
    public sealed class InMemoryContentTracker : ContentTracker
    {
        /// <summary>
        /// Creates a new in-memory content tracker.
        /// </summary>
        public InMemoryContentTracker()
        {
            this.fileStorage = new Dictionary<string, string>();
            this.repoNames = new SortedSet<string>();
            this.fileLock = new ReaderWriterLockSlim();
        }

        private Dictionary<string, string> fileStorage;
        private SortedSet<string> repoNames;
        private ReaderWriterLockSlim fileLock;

        /// <summary>
        /// Gets a collection of repository names maintained by this
        /// content tracker.
        /// </summary>
        public IReadOnlyCollection<string> RepositoryNames => repoNames;

        /// <inheritdoc/>
        public override void ConfigureAuthenticationModule(NancyModule module)
        {
            module.Get["auth/{token}/{login}"] = args =>
            {
                User user;
                if (!User.TryGetByToken(args.token, out user))
                {
                    return HttpStatusCode.BadRequest;
                }

                user.ContentTrackerToken = new InMemoryContentTrackerToken(
                    args.login, this);

                return HtmlHelpers.CreateHtmlPage(
                    "Authentication successful",
                    "<h1>You did it!</h1> <div>Yay! You successfully managed to authenticate! 🎉</div>");
            };
        }

        /// <summary>
        /// Gets the contents of a particular file managed by this
        /// content tracker.
        /// </summary>
        /// <param name="repoOwner">
        /// The owner of the repository where the file is stored.
        /// </param>
        /// <param name="repoName">
        /// The name of the repository where the file is stored.
        /// </param>
        /// <param name="filePath">
        /// The path to the file in the repository.
        /// </param>
        /// <returns>
        /// The file's contents.
        /// </returns>
        public string GetFileContents(string repoOwner, string repoName, string filePath)
        {
            try
            {
                fileLock.EnterReadLock();
                string contents;
                if (fileStorage.TryGetValue(GetFileKey(repoOwner, repoName, filePath), out contents))
                {
                    return contents;
                }
                else
                {
                    throw new ContentTrackerException(
                        "File '" + filePath + "' does not exist.");
                }
            }
            finally
            {
                fileLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Sets the contents of a particular file managed by this
        /// content tracker. Creates a new file if the file doesn't
        /// exist already.
        /// </summary>
        /// <param name="repoOwner">
        /// The owner of the repository where the file is stored.
        /// </param>
        /// <param name="repoName">
        /// The name of the repository where the file is stored.
        /// </param>
        /// <param name="filePath">
        /// The path to the file in the repository.
        /// </param>
        /// <param name="contents">
        /// The text to store in the file.
        /// </param>
        public bool SetFileContents(
            string repoOwner,
            string repoName,
            string filePath,
            string contents)
        {
            try
            {
                fileLock.EnterWriteLock();
                string key = GetFileKey(repoOwner, repoName, filePath);
                bool created = !fileStorage.ContainsKey(key);
                fileStorage[key] = contents;
                return created;
            }
            finally
            {
                fileLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets a list of all file names in a particular repository.
        /// </summary>
        /// <param name="repoOwner">
        /// The login of the repository's owner.
        /// </param>
        /// <param name="repoName">
        /// The name of the repository to inspect.
        /// </param>
        /// <returns>
        /// A list of file names.
        /// </returns>
        public IReadOnlyList<string> GetFileNames(
            string repoOwner,
            string repoName)
        {
            var repoSlug = GetRepoKey(repoOwner, repoName) + "/";
            try
            {
                fileLock.EnterReadLock();
                return fileStorage.Keys
                    .Where(k => k.StartsWith(repoSlug))
                    .Select(k => k.Substring(repoSlug.Length))
                    .ToArray();
            }
            finally
            {
                fileLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Creates a new repository.
        /// </summary>
        /// <param name="ownerName">
        /// The login of the repository's owner.
        /// </param>
        /// <param name="repoName">
        /// The name of the repository to create.
        /// </param>
        /// <returns>
        /// <c>true</c> if a new repository is created;
        /// <c>false</c> if it exists already.
        /// </returns>
        public bool CreateRepository(string ownerName, string repoName)
        {
            return repoNames.Add(GetRepoKey(ownerName, repoName));
        }

        private static string GetRepoKey(
            string repoOwner,
            string repoName)
        {
            return repoOwner + "/" + repoName;
        }

        private static string GetFileKey(
            string repoOwner,
            string repoName,
            string filePath)
        {
            return GetRepoKey(repoOwner, repoName) + "/" + filePath;
        }
    }

    /// <summary>
    /// A token for an in-memory content tracker.
    /// </summary>
    public sealed class InMemoryContentTrackerToken : ContentTrackerToken
    {
        /// <summary>
        /// Creates an in-memory content tracker token from a login.
        /// </summary>
        /// <param name="login">
        /// An authenticated user's login.
        /// </param>
        /// <param name="tracker">
        /// A reference to an in-memory content tracker.
        /// </param>
        public InMemoryContentTrackerToken(
            string login,
            InMemoryContentTracker tracker)
        {
            this.Login = login;
            this.Tracker = tracker;
        }

        /// <summary>
        /// Gets the authenticated user's login.
        /// </summary>
        /// <returns>The user's login.</returns>
        public string Login { get; private set; }

        /// <summary>
        /// A reference to the in-memory content tracker that
        /// that created this token.
        /// </summary>
        /// <returns>The content tracker.</returns>
        public InMemoryContentTracker Tracker { get; private set; }

        /// <inheritdoc/>
        public override Task<T> UseClient<T>(
            Func<ContentTrackerClient, Task<T>> useClient)
        {
            var client = new InMemoryContentTrackerClient(this);
            return useClient(client);
        }
    }

    /// <summary>
    /// A client implementation for the in-memory content tracker.
    /// </summary>
    public sealed class InMemoryContentTrackerClient : ContentTrackerClient
    {
        /// <summary>
        /// Creates an in-memory content tracker client.
        /// </summary>
        /// <param name="token">An authenticated user's token.</param>
        public InMemoryContentTrackerClient(
            InMemoryContentTrackerToken token)
        {
            this.token = token;
        }

        private InMemoryContentTrackerToken token;

        /// <inheritdoc/>
        public override Task<string> GetLogin()
        {
            return Task.FromResult(token.Login);
        }

        /// <inheritdoc/>
        public override Task<string> GetName()
        {
            return Task.FromResult(token.Login);
        }

        /// <inheritdoc/>
        public override Task<string> GetEmail()
        {
            return Task.FromResult(token.Login + "@example.com");
        }

        /// <inheritdoc/>
        public override Task<IReadOnlyList<string>> GetRepositoryNames()
        {
            return Task.FromResult<IReadOnlyList<string>>(
                token.Tracker.RepositoryNames.ToArray());
        }

        /// <inheritdoc/>
        public override Task<string> GetFileContents(
            string repoOwner,
            string repoName,
            string filePath)
        {
            return Task.FromResult(
                token.Tracker.GetFileContents(
                    repoOwner,
                    repoName,
                    filePath));
        }

        /// <inheritdoc/>
        public override Task<bool> SetFileContents(
            string repoOwner,
            string repoName,
            string filePath,
            string contents)
        {
            return Task.FromResult(
                token.Tracker.SetFileContents(
                    repoOwner,
                    repoName,
                    filePath,
                    contents));
        }

        /// <inheritdoc/>
        public override Task<IReadOnlyList<string>> GetFileNames(string repoOwner, string repoName)
        {
            return Task.FromResult(
                token.Tracker.GetFileNames(
                    repoOwner,
                    repoName));
        }

        /// <inheritdoc/>
        public override Task<string> CreateRepository(string repoName)
        {
            token.Tracker.CreateRepository(token.Login, repoName);
            return Task.FromResult(token.Login + "/" + repoName);
        }
    }
}