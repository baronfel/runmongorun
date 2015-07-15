using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migrator
{
    public class Migrator
    {
        public static async Task<Result<int>> Migrate(string mongoPath, string hostname, int port, string database, string manifestPath, bool warn, string changeSetCollectionName, Action<string> logFunc, Action<string> errFunc)
        {
            var repo = new ChangeSetRepo(hostname, port, database, changeSetCollectionName);
            var changesets = ManifestReader.ReadScripts(manifestPath);

            var dbPairs = await Task.WhenAll(changesets.Select(async x => new { cs = x, dbcs = await repo.GetById(x.ChangeId) }));
            var changesToRun = 
                dbPairs.Where(x => 
                    x.cs.AlwaysRuns 
                    || x.dbcs == null 
                    || SameButWarning(x.cs, x.dbcs, warn, logFunc, errFunc));

            //changesToRun.Aggregate()
            return Result<int>.Pass(0);
        }

        private static bool SameButWarning(ChangeSet cs, ChangeSet dbcs, bool warn, Action<string> logFunc, Action<string> errFunc)
        {
            if ((cs.Hash != dbcs.Hash))
            {
                var message = string.Format("update changeSet [{0}] in script [{1}] has changed since the last time is was run.", cs.ChangeId, cs.File);
                if (!warn)
                {
                    errFunc(message);
                    errFunc("if this is intended change the changeSetId");
                    throw new InvalidOperationException(message);
                }
                else
                {
                    logFunc(message);
                }
            }
            return true;
        }

        static Tuple<ChangeSet, int, string, string> ExecCommandChangeSet(string mongoPath, string hostName, string port, string database, ChangeSet changeSet)
        {
            var fqnMongoPath = Path.Combine(mongoPath, "mongo.exe");
            var connstring = string.Format("{hostName}:{port}/{database}", hostName, port, database);

            var startInfo = new ProcessStartInfo()
            {
                FileName = fqnMongoPath,
                Arguments = connstring,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            var process = new Process
            {
                StartInfo = startInfo,
            };

            var outBuilder = new StringBuilder();
            var errBuilder = new StringBuilder();
            process.OutputDataReceived += (o, d) => outBuilder.AppendLine(d.Data);
            process.ErrorDataReceived += (o, d) => errBuilder.AppendLine(d.Data);
            process.Start();
            process.StandardInput.WriteLine("print ('Begining ChangeSet[{0}] from File[{1}]')", changeSet.ChangeId, changeSet.File);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            foreach (var line in changeSet.Content)
            {
                process.StandardInput.WriteLine(line);
            }

            process.StandardInput.WriteLine("print ('Finishing ChangeSet[{0}] from File[{1}]')", changeSet.ChangeId, changeSet.File);
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();

            return Tuple.Create(changeSet, process.ExitCode, outBuilder.ToString(), errBuilder.ToString());
        }


    }
}
