using System;
using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// Describes a user who is either authenticated
    /// with the collaboration server or in the process
    /// of becoming authenticated.
    ///
    /// User instances are ephemeral: they are created on
    /// demand when someone starts an UnSHACLed session
    /// and are deleted when their expiration date is passed.
    /// Do not confuse User instances with accounts on our
    /// service: a new user is created every time someone
    /// logs in; a new account is created only when someone
    /// buys our product.
    /// </summary>
    public sealed class User
    {
        private User(string token, TimeSpan lifetime)
        {
            this.Token = token;
            this.ExpirationDate = DateTime.Now + lifetime;
        }

        /// <summary>
        /// Gets the user's token, which is used to uniquely
        /// identify the user.
        /// </summary>
        /// <returns>The user's token.</returns>
        public string Token { get; private set; }

        /// <summary>
        /// Gets the point at which the user's token expires.
        /// </summary>
        /// <returns>The user's token expiration date.</returns>
        public DateTime ExpirationDate { get; private set; }

        /// <summary>
        /// Gets a GitHub OAuth token for the user, if any.
        /// </summary>
        /// <returns>The user's GitHub OAuth token.</returns>
        public OauthToken GitHubToken { get; set; }

        /// <summary>
        /// Tells if this user is still active, that is, their
        /// expiration date has not come to pass yet.
        /// </summary>
        public bool IsActive => ExpirationDate > DateTime.Now;

        /// <summary>
        /// Tests if this user is authenticated.
        /// </summary>
        public bool IsAuthenticated => GitHubToken != null;

        /// <summary>
        /// Postpones this user's expiration date by a particular
        /// amount.
        /// </summary>
        /// <param name="extension">
        /// The amount of time to extend the user's expiration date by.
        /// </param>
        public void ExtendLifetime(TimeSpan extension)
        {
            ExpirationDate += extension;
        }

        private static readonly Dictionary<string, User> allUsers
            = new Dictionary<string, User>();

        private static int garbageCollectionCounter = 0;

        /// <summary>
        /// The threshold at which garbage collection of
        /// inactive users occurs.
        /// </summary>
        private const int GarbageCollectionThreshold = 32;

        /// <summary>
        /// Looks for and deletes expired users.
        ///
        /// Note: this method is not thread-safe.
        /// </summary>
        private static void DeleteExpiredUsers()
        {
            var inactiveUsers = allUsers.Values
                .Where(user => !user.IsActive)
                .ToArray();

            foreach (var user in inactiveUsers)
            {
                allUsers.Remove(user.Token);
            }
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="lifetime">
        /// The amount of time until the user's token expires.
        /// </param>
        /// <returns>The user to create.</returns>
        public static User Create(TimeSpan lifetime)
        {
            User result;
            lock (allUsers)
            {
                // Delete inactive users every once in a while.
                garbageCollectionCounter++;
                if (garbageCollectionCounter == GarbageCollectionThreshold)
                {
                    garbageCollectionCounter = 0;
                    DeleteExpiredUsers();
                }

                // Actually register the new user.
                result = new User(Guid.NewGuid().ToString(), lifetime);
                allUsers[result.Token] = result;
            }
            return result;
        }

        /// <summary>
        /// Tries to look up a user by their token.
        /// </summary>
        /// <param name="token">The token that identifies the user.</param>
        /// <param name="user">
        /// The user to find. May be <c>null</c> if no user was found.
        /// </param>
        /// <returns>
        /// <c>true</c> if there is an active user with the provided token;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool TryGetByToken(
            string token, out User user)
        {
            lock (allUsers)
            {
                if (allUsers.TryGetValue(token, out user))
                {
                    if (user.IsActive)
                    {
                        // We found an active user.
                        return true;
                    }
                    else
                    {
                        // User's token has expired. Delete it
                        // and act like we didn't find anything.
                        user = null;
                        allUsers.Remove(token);
                        return false;
                    }
                }
                else
                {
                    // Found nothing of interest.
                    return false;
                }
            }
        }
    }
}
