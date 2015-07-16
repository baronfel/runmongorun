using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chessie.ErrorHandling;
using Chessie.ErrorHandling.CSharp;

using static Func.Utils;
using static Migrator.ProcessUtil;


namespace Migrator
{
    public class Migrator
    {
        public static async Task<Result<string, string>> Migrate(string mongoPath, string hostname, int port, string database, string manifestPath, bool warn, string changeSetCollectionName, Action<string> logFunc, Action<string> errFunc)
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
                var result = ExecCommandChangeSet(mongoPath, hostname, port, database, changeSet);
                if (result.IsBad) return Result<string,string>.FailWith(result.FailedWith());

                var upsertResult = Result<ChangeSet, Exception>.Try(() => repo.Upsert(changeSet).Result).MapError(e => e.Message);
                if (upsertResult.IsBad)
                {
                    var messages = new[] { result.SucceededWith() }.Concat(upsertResult.FailedWith().Select(Id));
                    return Result<string, string>.FailWith(messages);
                }

                result.Match((s, ms) => info.Append(s), fs => { return; });
            }

            return Result<string, string>.Succeed(info.ToString());
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

        static Result<string, string> ExecCommandChangeSet(string mongoPath, string hostName, int port, string database, ChangeSet changeSet)
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

    public static class ResultExt
    {
        /// <summary>
        /// transforms the error type of a result according to some function. if the result is Ok, rewraps the success;
        /// </summary>
        /// <typeparam name="TSuccess"></typeparam>
        /// <typeparam name="TOrigMessage"></typeparam>
        /// <typeparam name="TTransformedMessage"></typeparam>
        /// <param name="r"></param>
        /// <param name="errorTransform"></param>
        /// <returns></returns>
        public static Result<TSuccess, TTransformedMessage> MapError<TSuccess, TOrigMessage, TTransformedMessage>(this Result<TSuccess, TOrigMessage> r, Func<TOrigMessage, TTransformedMessage> errorTransform)
        {
            Result<TSuccess, TTransformedMessage> output = null;
            r.Match(
                (s, msgs) => output = Result<TSuccess, TTransformedMessage>.Succeed(s),
                fails => output = Result<TSuccess, TTransformedMessage>.FailWith(fails.Select(errorTransform)));
            return output;
        }
    }
}
