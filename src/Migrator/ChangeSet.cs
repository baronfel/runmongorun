using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Migrator
{
    public class ChangeSet
    {

        public object Id { get; set; }
        [BsonElement(elementName: "author")]
        public string Author { get; set; }
        [BsonElement(elementName: "changeId")]
        public string ChangeId { get; set; }
        [BsonIgnore]
        public bool AlwaysRuns { get; set; }
        [BsonElement(elementName: "file")]
        public string File { get; set; }
        [BsonIgnore]
        public List<string> Content { get; set; }
        [BsonElement(elementName: "hash")]
        public Guid? Hash => MakeHash();

        private Guid MakeHash()
        {
            using (MD5 md5Hash = MD5.Create())
            using (var ms = new MemoryStream())
            using (var sr = new BinaryWriter(ms))
            {
                Content.ForEach(x => sr.Write(x));
                sr.Flush();
                byte[] data = md5Hash.ComputeHash(ms.ToArray());
                return new Guid(data);
            }
        }

        [BsonElement(elementName: "date")]
        public DateTime? Date { get; set; }

        public string Mongeezetype => "changeSetExecution";
    }
}
