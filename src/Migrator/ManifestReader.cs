using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Func.Utils;

namespace Migrator
{
    public static class ManifestReader
    {
        static Regex changeSetRE = new Regex(@"//\s*changeset\s+(?<user>[\w\-]+):(?<changeset>[^\s]+)\s*(runAlways\s*:\s*(?<runAlways>\w*)){0,1}",
                RegexOptions.IgnoreCase |
                RegexOptions.ExplicitCapture |
                RegexOptions.Singleline |
                RegexOptions.IgnorePatternWhitespace);

        public static IEnumerable<ChangeSet> ReadScripts(string manifestPath)
        {
            if (!File.Exists(manifestPath)) throw new ArgumentException("no file exists at manifest path", nameof(manifestPath));
            var rootPath = Path.GetDirectoryName(manifestPath);
            var fullFilePaths = File.ReadAllLines(manifestPath).Where(IsNotNullOrWhiteSpace).Select(x => Path.Combine(rootPath, x));
            var changeSets = fullFilePaths.Select(ReadChangeSets).SelectMany(Id);
            return changeSets;
        }

        public static IEnumerable<ChangeSet> ReadFromFileAndLines(string scriptPath, IEnumerable<string> lines)
        {
            var changeSets = new List<ChangeSet>();
            ChangeSet current = null;
            foreach (var line in lines)
            {
                var match = changeSetRE.Match(line);
                if (match.Success)
                {
                    current = new ChangeSet()
                    {
                        File = scriptPath,
                        Author = match.Groups["user"].Value,
                        ChangeId = match.Groups["changeset"].Value,
                        AlwaysRuns = match.Groups["runAlways"].Value == "true",
                        Content = new List<string>()
                    };
                    changeSets.Add(current);
                }
                else if (current != null)
                {
                    current.Content.Add(line);
                }
            }

            return changeSets;
        }

        public static IEnumerable<ChangeSet> ReadChangeSets(string scriptPath)
        {
            if (!File.Exists(scriptPath)) throw new ArgumentException(string.Format("path {0} does not exist", scriptPath), nameof(scriptPath));
            var lines = File.ReadAllLines(scriptPath).Where(IsNotNullOrWhiteSpace);
            return ReadFromFileAndLines(scriptPath, lines);
        }

        static bool IsNotNullOrWhiteSpace(string s) => !string.IsNullOrWhiteSpace(s) && !string.IsNullOrEmpty(s);
    }
}
