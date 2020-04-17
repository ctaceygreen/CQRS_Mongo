using CQRS.Projections;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace CQRS.MongoDB
{
    public class ProjectionHandlerRepository : IProjectionHandlerRepository
    {
        private readonly MongoClient _mongoClient;
        public ProjectionHandlerRepository(MongoClient mongoClient)
        {
            _mongoClient = mongoClient;
        }
        public async Task<string> GetLastStreamId<T>()
        {
            var collection = GetCollection();
            var filter = Builders<ProjectionHandler>.Filter.Eq(f => f.ProjectionType, typeof(T).Name);
            var projectionHandler = (await collection.FindAsync(filter)).FirstOrDefault();
            return projectionHandler?.LastStreamId;
        }

        public async Task Save<T>(string lastStreamId)
        {
            var collection = GetCollection();
            var filter = Builders<ProjectionHandler>.Filter.Eq(f => f.ProjectionType, typeof(T).Name);
            await collection.ReplaceOneAsync(filter, new ProjectionHandler { ProjectionType = typeof(T).Name, LastStreamId = lastStreamId });
        }

        private IMongoCollection<ProjectionHandler> GetCollection()
        {
            var database = _mongoClient.GetDatabase("Projections");

            return database.GetCollection<ProjectionHandler>("ProjectionHandlers");
        }
    }
}
