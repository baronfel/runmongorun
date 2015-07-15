using CommandLine;
using CommandLine.Text;
using System;
using System.Linq;

namespace MongoMigrator
{
    class Options
    {
        [Option('m', "mongoPath", Required = false, HelpText = "The path to the mongo executable to use.  If not set, assumes mongo.exe is on your PATH.")]
        public string Server { get; set; }

        [Option('d', "database", Required = true, HelpText = "The mongo database name to connect to.")];
        public string Database { get; set; }

        [Option('f', "manifest", Required = true, HelpText = "The path to the file with the list of js scripts to update.")]
        public string ManifestFile { get; set; }

        [Option('h', "host", Required = false, HelpText = "The mongo server hostname.  If not provided defaults to localhost.")]
        public string HostName { get; set; }

        [Option('p', "port", Required = false, HelpText = "The port to connect on. If not provided default to 27017.")]
        public int? Port { get; set; }

        [Option('w', "warn", Required = false, HelpText = "If set, allows scripts marked as one-time to change.")]
        public bool? WarnOnOneTimeScriptChange { get; set; }

        [Option("changeSetColl", Required = false, HelpText = "If set, changesets will be retrieved/persisted to the given collection.")]
        public string ChangeSetCollectionName { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("MongoMigrator", "1.0"),
                Copyright = new CopyrightInfo("me!", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };

            if (LastParserState.Errors.Any())
            {
                var errors = help.RenderParsingErrorsText(this, 2);
                if (!string.IsNullOrEmpty(errors))
                {
                    help.AddPreOptionsLine(string.Concat(Environment.NewLine, "ERROR(S):"));
                    help.AddPreOptionsLine(errors);
                }
            }

            help.AddPreOptionsLine("Usage: mongoMigrator [options]");
            help.AddOptions(this);
            return help;
        }
    }

    class Program
    {
        static ParserSettings _parserSettings = new ParserSettings
        {
            CaseSensitive = false,
            IgnoreUnknownArguments = true,
        };

        static void Main(string[] args)
        {
            var parsed = new Options();
            var parser = new Parser(s => s = _parserSettings);
            if (parser.ParseArguments(args, parsed))
            {
                var warn = parsed.WarnOnOneTimeScriptChange.GetValueOrDefault(false);
                var port = parsed.Port.GetValueOrDefault(27017);
                var csCollectionName = string.IsNullOrEmpty(parsed.ChangeSetCollectionName) ? "migrations" : parsed.ChangeSetCollectionName;

                var result = Migrator.Migrator.Migrate(parsed.Server, parsed.HostName, port, parsed.Database, parsed.ManifestFile, warn, csCollectionName, Console.WriteLine, Console.Error.WriteLine).Result;
                if (result.HasError)
                {
                    Console.Error.WriteLine(result.Error);
                    Environment.ExitCode = -1;
                    return;
                }

                Environment.ExitCode = 0;
                return;

            }
            else
            {
                Console.WriteLine(parsed.GetUsage());
                Environment.ExitCode = -1;
            }
        }
    }
}
