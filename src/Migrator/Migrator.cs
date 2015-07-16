using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Migrator.ProcessUtil;

namespace Migrator
{
    public class Migrator
    {
        public static async Task<Result<ExecResult>> Migrate(string mongoPath, string hostname, int port, string database, string manifestPath, bool warn, string changeSetCollectionName, Action<string> logFunc, Action<string> errFunc)
        {
            var repo = new ChangeSetRepo(hostname, port, database, changeSetCollectionName);
            var changesets = ManifestReader.ReadScripts(manifestPath);

            var dbPairs = await Task.WhenAll(changesets.Select(async x => new { cs = x, dbcs = await repo.GetById(x.ChangeId) }));
            var changesToRun = 
                dbPairs.Where(x => 
                    x.cs.AlwaysRuns 
                    || x.dbcs == null 
                    || SameButWarning(x.cs, x.dbcs, warn, logFunc, errFunc))
                    .Select(x => x.cs);

            var info = new StringBuilder();
            foreach(var changeSet in changesToRun)
            {
                var exec = ExecCommandChangeSet(mongoPath, hostname, port, database, changeSet);
                if (exec.ExitCode != 0) return Lift(exec);

                var upsertResult = await Result<ChangeSet>.LiftAsync(async () => await repo.Upsert(changeSet), () => changeSet);
                if (upsertResult.HasError) return Result<ExecResult>.Fail(new ExecResult(-1, exec.Info, upsertResult.Error.Message), upsertResult.Error);

                info.Append(exec.Info);
            }

            return Result<ExecResult>.Pass(new ExecResult(0, info.ToString(), string.Empty));
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

        static ExecResult ExecCommandChangeSet(string mongoPath, string hostName, int port, string database, ChangeSet changeSet)
        {
            var fqnMongoPath = Path.Combine(mongoPath, "mongo.exe");
            var connstring = string.Format("{hostName}:{port}/{database}", hostName, port, database);
            var ops = new ConsoleOps(
                "print ('Begining ChangeSet[{changeSet.ChangId}] from File[{changeSet.File}]')",
                changeSet.Content,
                "print ('Finishing ChangeSet[{changeSet.ChangId}] from File[{changeSet.File}]')"
                );

            return ExecProcess(fqnMongoPath, connstring, ops);
        }
    }
}
