using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoMigrator
{
    class Options
    {
        [Option('m', "mongoPath", Required = false, HelpText = "The path to the mongo executable to use.  If not set, assumes mongo.exe is on your PATH.")]
        public string MongoPath { get; set; }

        [Option('f', "manifest", Required = true, HelpText = "The path to the file with the list of js scripts to update.")]
        public string ManifestFile { get; set; }

        [Option('h', "host", Required = false, HelpText = "The mongo server hostname.  If not provided defaults to localhost.")]
        public string HostName { get; set; }

        [Option('p', "port", Required = false, HelpText = "The port to connect on. If not provided default to 27017.")]
        public int? Port { get; set; }

        [Option('c', "collection", Required = true, HelpText = "The name of the collection to connect to.")]
        public string CollectionName { get; set; }

        [Option('w', "warn", Required = false, HelpText = "If set, allows scripts marked as one-time to change.")]
        public bool? WarnOnOneTimeScriptChange { get; set; }

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


            if (this.LastParserState.Errors.Any())
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

                var result = Migrate(parsed.MongoPath, parsed.HostName, parsed.Port, parsed.CollectionName, parsed.ManifestFile, parsed.WarnOnOneTimeScriptChange);


            }
            else
            {
                Console.WriteLine(parsed.GetUsage());
                Environment.ExitCode = -1;
            }
        }
    }
}
