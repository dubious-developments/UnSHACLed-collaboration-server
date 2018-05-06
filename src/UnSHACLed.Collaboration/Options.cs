using System.Collections.Generic;
using Pixie.Options;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// Contains options accepted by the collaboration server
    /// application.
    /// </summary>
    public static class Options
    {
        /// <summary>
        /// The 'domains' option, which allows users to pick a
        /// list of URIs the application will listen to.
        /// </summary>
        public static readonly Option Domains =
            SequenceOption.CreateStringOption(
                new OptionForm[]
                {
                    OptionForm.Long("domains"),
                    OptionForm.Short("d")
                })
            .WithCategory("Required options")
            .WithParameters(new SymbolicOptionParameter("uri", true))
            .WithDescription(
                "Specifies a list of domains to which the application will listen.");

        /// <summary>
        /// The 'client-id' option, which specifies the GitHub client
        /// ID to use.
        /// </summary>
        public static readonly Option ClientId =
            ValueOption.CreateStringOption(
                new OptionForm[]
                {
                    OptionForm.Long("client-id"),
                    OptionForm.Short("i")
                },
                "")
            .WithCategory("Required options")
            .WithParameter(new SymbolicOptionParameter("id"))
            .WithDescription(
                "Specifies the GitHub client ID to use.");

        /// <summary>
        /// The 'client-secret' option, which specifies the GitHub client
        /// secret to use.
        /// </summary>
        public static readonly Option ClientSecret =
            ValueOption.CreateStringOption(
                new OptionForm[]
                {
                    OptionForm.Long("client-secret"),
                    OptionForm.Short("s")
                },
                "")
            .WithCategory("Required options")
            .WithParameter(new SymbolicOptionParameter("secret"))
            .WithDescription(
                "Specifies the GitHub client secret to use.");

        /// <summary>
        /// The 'help' option.
        /// </summary>
        public static readonly Option Help =
            FlagOption.CreateFlagOption(
                OptionForm.Short("h"),
                OptionForm.Long("help"))
            .WithDescription("Prints a help message.");

        /// <summary>
        /// A read-only list of all options accepted by the collaboration
        /// server.
        /// </summary>
        public static readonly IReadOnlyList<Option> All = new[]
        {
            ClientId,
            ClientSecret,
            Domains,
            Help
        };
    }
}
