﻿using CommandLine;
using CommandLine.Text;
using System;
using System.Linq;

using Chessie.ErrorHandling.CSharp;
using System.Reflection;

namespace RunMongoRun
{
    class Options
    {
        [Option('m', "mongoPath", Required = false, DefaultValue = "mongo.exe", HelpText = "The path to the mongo executable to use.  If not set, assumes mongo.exe is on your PATH.")]
        public string MongoPath { get; set; }

        [Option('d', "database", Required = true, HelpText = "The mongo database name to connect to.")]
        public string Database { get; set; }

        [Option('f', "manifest", Required = true, HelpText = "The path to the file with the list of js scripts to update.")]
        public string ManifestFile { get; set; }

        [Option('h', "host", Required = false, DefaultValue = "localhost", HelpText = "The mongo server hostname.  If not provided defaults to localhost.")]
        public string HostName { get; set; }

        [Option('p', "port", Required = false, DefaultValue = 27017, HelpText = "The port to connect on. If not provided default to 27017.")]
        public int Port { get; set; }

        [Option('w', "warn", Required = false, DefaultValue = false, HelpText = "If set, allows scripts marked as one-time to change.")]
        public bool WarnOnOneTimeScriptChange { get; set; }

        [Option("changeSetColl", Required = false, DefaultValue = "migrations", HelpText = "If set, changesets will be retrieved/persisted to the given collection.")]
        public string ChangeSetCollectionName { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var name = Assembly.GetExecutingAssembly().GetName();
            var help = new HelpText
            {
                Heading = new HeadingInfo(name.Name, name.Version.ToString()),
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

            help.AddPreOptionsLine("Usage: RunMongoRun [options]");
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

        static void WriteError(string s)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(s);
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            var parsed = new Options();
            if (new Parser(s => s = _parserSettings).ParseArguments(args, parsed))
            {
                var result = Migrator.Migrator.Migrate(parsed.MongoPath, parsed.HostName, parsed.Port, parsed.Database, parsed.ManifestFile, parsed.WarnOnOneTimeScriptChange, parsed.ChangeSetCollectionName, Console.WriteLine, Console.Error.WriteLine).Result;

                result.Match(
                    (suc, msgs) => Environment.Exit(0),
                    failure =>
                    {
                        WriteError(string.Join(Environment.NewLine, failure.AsEnumerable().Select(x => x.Item2)));
                        Environment.Exit(failure.First().Item1);
                    });
            }
            else
            {
                WriteError(parsed.GetUsage());
                Environment.Exit(1);
            }
        }
    }
}
