using CQRS.Events;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CQRS.EventGrid
{
    public class EventStorageWithEventGridPublisher : IEventStorage
    {
        private readonly IEventStorage _eventStorage;
        private readonly string _topicEndpoint;
        private readonly string _topicKey;

        public EventStorageWithEventGridPublisher(IEventStorage eventStorage, string topicEndpoint, string topicKey)
        {
            _eventStorage = eventStorage;
            _topicEndpoint = topicEndpoint;
            _topicKey = topicKey;
        }
        public async Task<List<Event>> GetEventsForAggregate(Guid aggregateId)
        {
            return await _eventStorage.GetEventsForAggregate(aggregateId);
        }

        public async Task<List<Event>> GetEventsSince(string id)
        {
            return await _eventStorage.GetEventsSince(id);
        }

        public async Task SaveNewEventsForAggregate(Guid aggregateId, List<Event> newEvents)
        {
            // Instead of saving event, publish to event grid
            string topicHostname = new Uri(_topicEndpoint).Host;
            TopicCredentials topicCredentials = new TopicCredentials(_topicKey);
            EventGridClient client = new EventGridClient(topicCredentials);
            await client.PublishEventsAsync(topicHostname, GetEventsList(newEvents));
        }

        private IList<EventGridEvent> GetEventsList(List<Event> events)
        {
            List<EventGridEvent> eventsList = new List<EventGridEvent>();

            foreach(var originalEvent in events)
            {
                eventsList.Add(new EventGridEvent()
                {
                    Id = originalEvent.Id,
                    Data = originalEvent,
                    EventTime = originalEvent.DateCreated,
                    EventType = originalEvent.Type,
                    Subject = "",
                    DataVersion = "1.0"
                });
            }
            return eventsList;
        }
    }
}
