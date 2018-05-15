using System;
using System.Collections.Generic;
using System.Threading;
using Nancy.Hosting.Self;
using Pixie;
using Pixie.Markup;
using Pixie.Options;
using Pixie.Terminal;
using Pixie.Transforms;

namespace UnSHACLed.Collaboration
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Create a pretty log.
            var log = new RecordingLog(CreateDiagnosticLog(TerminalLog.Acquire()));

            // Parse command-line arguments.
            var optParser = new GnuOptionSetParser(Options.All);

            // Actually parse the options.
            var parsedOptions = optParser.Parse(args, log);

            if (log.Contains(Severity.Error))
            {
                // Ouch. Command-line arguments were bad. Stop testing now.
                return 1;
            }

            if (parsedOptions.GetValue<bool>(Options.Help))
            {
                // Print a cute little help message (to stdout instead of stderr).
                var helpLog = TerminalLog.AcquireStandardOutput();
                helpLog.Log(
                    new Pixie.LogEntry(
                        Severity.Message,
                        new HelpMessage(
                            "Runs an UnSHACLed collaboration server.",
                            "collaboration-server [options...]",
                            Options.All)));
                return 0;
            }

            var domainUris = ParseDomains(parsedOptions, log);

            if (parsedOptions.GetValue<bool>(Options.MockContentTracker))
            {
                ContentTrackerCredentials.ContentTracker = new InMemoryContentTracker();
            }
            else
            {
                var clientId = parsedOptions.GetValue<string>(Options.ClientId);
                var clientSecret = parsedOptions.GetValue<string>(Options.ClientSecret);

                CheckMandatoryStringOptionHasArg(Options.ClientId, parsedOptions, log);
                CheckMandatoryStringOptionHasArg(Options.ClientSecret, parsedOptions, log);

                ContentTrackerCredentials.ContentTracker = new GitHubContentTracker(
                    domainUris[0], clientId, clientSecret);
            }

            if (log.Contains(Severity.Error))
            {
                // Looks like the options were ill-formatted.
                // An error has already been reported. Just exit.
                return 1;
            }

            using (var nancyHost = new NancyHost(domainUris))
            {
                nancyHost.Start();
                log.Log(
                    new LogEntry(
                        Severity.Info,
                        "server started",
                        "server is up now."));

                // Keep serving until the application is closed.
                while (true)
                {
                    Thread.Sleep(Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Parse the domain names as an array of URIs.
        /// </summary>
        /// <param name="parsedOptions">The set of all parsed options.</param>
        /// <param name="log">A log to send errors to.</param>
        /// <returns>An array of URIs.</returns>
        private static Uri[] ParseDomains(OptionSet parsedOptions, ILog log)
        {
            var domainUris = new List<Uri>();

            var domainNames = parsedOptions.GetValue<IReadOnlyList<string>>(Options.Domains);
            if (domainNames.Count == 0)
            {
                // Zero domains doesn't make no sense. Make the user pick a domain.
                log.Log(
                    new LogEntry(
                        Severity.Error,
                        "no domain",
                        "at least one URI must be specified as a base domain for the server."));
                return domainUris.ToArray();
            }

            foreach (var name in domainNames)
            {
                Uri domainUri;
                if (Uri.TryCreate(name, UriKind.Absolute, out domainUri))
                {
                    domainUris.Add(domainUri);
                }
                else
                {
                    log.Log(
                        new LogEntry(
                            Severity.Error,
                            "bad domain",
                            Quotation.QuoteEvenInBold(
                                "all domains must be well-formed absolute URIs, but ",
                                name,
                                " is not a well-formed URI.")));
                }
            }
            return domainUris.ToArray();
        }

        /// <summary>
        /// Takes a raw log and turns it into a log that
        /// always prints diagnostics.
        /// </summary>
        /// <param name="rawLog">The raw log to accept.</param>
        /// <returns>A log that always prints diagnostics.</returns>
        private static ILog CreateDiagnosticLog(ILog rawLog)
        {
            // Turn all entries into diagnostics and word-wrap the output.
            return new TransformLog(
                rawLog,
                entry => {
                    var transformed = DiagnosticExtractor.Transform(entry, "collaboration-server");
                    return new Pixie.LogEntry(
                        transformed.Severity,
                        WrapBox.WordWrap(transformed.Contents));
                });
        }

        /// <summary>
        /// Checks that a mandatory string-valued option actually
        /// has an argument.
        /// </summary>
        /// <param name="option">The mandatory string-valued option.</param>
        /// <param name="parsedOptions">Parsed command-line arguments.</param>
        /// <param name="log">A log to send errors to.</param>
        private static void CheckMandatoryStringOptionHasArg(
            Option option,
            OptionSet parsedOptions,
            ILog log)
        {
            if (string.IsNullOrWhiteSpace(parsedOptions.GetValue<string>(option)))
            {
                log.Log(
                    new Pixie.LogEntry(
                        Severity.Error,
                        "missing option",
                        Quotation.QuoteEvenInBold(
                            "option ",
                            option.Forms[0].ToString(),
                            " is mandatory but left blank.")));
            }
        }
    }
}
