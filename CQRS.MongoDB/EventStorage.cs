using CQRS.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.MongoDB
{
    //In order to use the objectId of mongo for ordering purposes, we have a mongo event class around our event
    [BsonIgnoreExtraElements]
    public class MongoEvent
    {
        [BsonId]
        public ObjectId ObjectId { get; set; }
        public Event Event { get; set; }
    }
    public class MongoEventStorage : IEventStorage
    {
        private readonly IMongoCollection<MongoEvent> _events;
        public async Task<List<Event>> GetEventsForAggregate(Guid aggregateId)
        {
            var filter = Builders<MongoEvent>.Filter.Eq(a => a.Event.AggregateId, aggregateId);
            var events = (await _events.FindAsync(filter)).ToList();
            return MongoEventsToEvents(events);
        }

        public async Task SaveNewEventsForAggregate(Guid aggregateId, List<Event> newEvents)
        {
            var mongoEvents = newEvents.Select(e => new MongoEvent { Event = e });
            await _events.InsertManyAsync(mongoEvents, new InsertManyOptions { IsOrdered = true });
        }

        public async Task<List<Event>> GetEventsSince(string id)
        {
            var filter = Builders<MongoEvent>.Filter.Gt(a => a.ObjectId, id == null ? ObjectId.Empty : ObjectId.Parse(id));
            var events = (await _events.FindAsync(filter)).ToList();
            return MongoEventsToEvents(events);
        }


        public MongoEventStorage(IMongoClient client, MongoRepositorySettings settings)
        {
            var database = client.GetDatabase(settings.DatabaseName);

            _events = database.GetCollection<MongoEvent>(settings.CollectionName);
        }

        private List<Event> MongoEventsToEvents(List<MongoEvent> events)
        {
            events.ForEach(e => e.Event.Id = e.ObjectId.ToString());
            return events.Select(e => e.Event).ToList();
        }
    }
}
