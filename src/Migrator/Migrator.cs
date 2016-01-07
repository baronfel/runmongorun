using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chessie.ErrorHandling;
using Chessie.ErrorHandling.CSharp;

using static Migrator.ProcessUtil;
using MongoDB.Driver;

namespace Migrator
{
    public class Migrator
    {
        public static async Task<Result<bool, Tuple<int, string>>> Migrate(string mongoPath, string hostname, int port, string database, string manifestPath, bool warn, string changeSetCollectionName, Action<string> logFunc, Action<string> errFunc)
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
                var result = ExecCommandChangeSet(mongoPath, hostname, port, database, changeSet, logFunc, errFunc);
                if (result.IsBad) return result.MapError(retCode => Tuple.Create(retCode, string.Format("the mongo process exited with code {0}.", retCode)));
                var upsertResult = Result<ChangeSet, Exception>.Try(() => repo.Upsert(changeSet).Result).MapError(e => e.Message);
                if (upsertResult.IsBad) return upsertResult.Map(cs => false).MapError(s => Tuple.Create(1, s)); // ignore the changeset and return a false value in the failure case.
            }

            return Result<bool, Tuple<int,string>>.Succeed(true);
        }

        static bool SameButWarning(ChangeSet cs, ChangeSet dbcs, bool warn, Action<string> logFunc, Action<string> errFunc)
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
      
        static string MakeCommandLineConnectionString(string hostname, int port, string database)
        {
            var mongourl = new MongoUrlBuilder()
            {
                Server = new MongoServerAddress(hostname, port),
            }.ToMongoUrl();

            var hostnameAndPort = string.Format("{0}:{1}", hostname, port);
            var replSetName = new MongoClient(mongourl)?.Cluster?.Settings?.ReplicaSetName ?? string.Empty;
            var actualHost = string.IsNullOrEmpty(replSetName) ? hostnameAndPort : string.Format("{0}/{1}", replSetName, hostnameAndPort);
            return string.Format("{0} --host \"{1}\"", database, actualHost);
        }

        static Result<bool, int> ExecCommandChangeSet(string mongoPath, string hostName, int port, string database, ChangeSet changeSet, Action<string> logFunc, Action<string> errFunc)
        {
            var connstring = MakeCommandLineConnectionString(hostName, port, database);
            var ops = new ConsoleOps(
                string.Format("print ('Begining ChangeSet[{0}] from File[{1}]')", changeSet.ChangeId, changeSet.File),
                changeSet.Content,
                string.Format("print ('Finishing ChangeSet[{0}] from File[{1}]')", changeSet.ChangeId, changeSet.File)
                );
            var fqnPath = Path.Combine(mongoPath, "mongo.exe");
            return ExecProcess(fqnPath, connstring, ops, logFunc, errFunc);
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
