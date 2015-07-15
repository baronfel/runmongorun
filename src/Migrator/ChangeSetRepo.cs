using System.Threading.Tasks;
using MongoDB.Driver;
using System;

namespace Migrator
{
    public class ChangeSetRepo
    {
        readonly IMongoCollection<ChangeSet> _collection;
        static readonly FilterDefinitionBuilder<ChangeSet> _filterer = new FilterDefinitionBuilder<ChangeSet>();
        static readonly UpdateDefinitionBuilder<ChangeSet> _updater = new UpdateDefinitionBuilder<ChangeSet>();
        static readonly UpdateOptions _updateOpts = new UpdateOptions { IsUpsert = true };

        static string MakeMongoUrl(string server, int port) => string.Format("mongodb://{server}:{port}", server, port);


        public ChangeSetRepo(string server, int port, string database, string changeSetCollectionName)
        {
            _collection = new MongoClient(MakeMongoUrl(server, port)).GetDatabase(database).GetCollection<ChangeSet>(changeSetCollectionName);
        }

        public async Task<ChangeSet> GetById(string changeId)
        {
            var filter = _filterer.Eq(cs => cs.ChangeId, changeId);
            return await _collection.Find(filter).SingleOrDefaultAsync();
        }

        public async Task<ChangeSet> Upsert(ChangeSet changeSet)
        {
            var filter = _filterer.Eq(cs => cs.ChangeId, changeSet.ChangeId);
            var update = _updater.Set(e => e.Author, changeSet.Author)
                .Set(e => e.Date, DateTime.Now.ToUniversalTime())
                .Set(e => e.File, changeSet.File)
                .Set(e => e.ChangeId, changeSet.ChangeId)
                .Set(e => e.Hash, changeSet.Hash);

            var result = await _collection.UpdateOneAsync(filter, update, _updateOpts);
            return await GetById(result.UpsertedId.ToString());
        }
    }
}
