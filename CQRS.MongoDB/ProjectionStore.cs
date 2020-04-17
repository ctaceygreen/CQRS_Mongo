using CQRS.Projections;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.MongoDB
{
    public class MongoProjectionStore<T> : IProjectionStore<T> where T : Projection
    {
        private readonly IMongoCollection<T> _projection;
        public MongoProjectionStore(IMongoClient client, MongoRepositorySettings settings)
        {
            var database = client.GetDatabase(settings.DatabaseName);

            _projection = database.GetCollection<T>(settings.CollectionName);
        }
        public async Task<T> Get(string key)
        {
            var filter = Builders<T>.Filter.Eq(p => p.Key, key);
            return (await _projection.FindAsync(filter)).FirstOrDefault();
        }

        public async Task Save(T projection, string key)
        {
            var filter = Builders<T>.Filter.Eq(p => p.Key, key);
            await _projection.ReplaceOneAsync(filter, projection, new ReplaceOptions { IsUpsert = true });
        }
    }
}
