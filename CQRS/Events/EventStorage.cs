using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Events
{
    public interface IEventStorage
    {
        Task<List<Event>> GetEventsForAggregate(Guid aggregateId);
        Task<List<Event>> GetEventsSince(string id);
        Task SaveNewEventsForAggregate(Guid aggregateId, List<Event> newEvents);
    }

    public class BullshitEventStorage : IEventStorage
    {
        private readonly List<Event> _events = new List<Event>();
        private int _eventCounter = 0;
        public async Task<List<Event>> GetEventsForAggregate(Guid aggregateId)
        {
            return _events.Where(e => e.AggregateId == aggregateId).ToList();
        }

        public async Task<List<Event>> GetEventsSince(string id)
        {
            // This is just mimicking a comparison that would happen on your store of ids
            return _events.Where(e => int.Parse(e.Id) > (id == null ? -1 : int.Parse(id))).ToList();
        }

        public async Task SaveNewEventsForAggregate(Guid aggregateId, List<Event> newEvents)
        {
            foreach(var ev in newEvents)
            {
                // Just mimicking an id of the store being incremented
                ev.Id = _eventCounter.ToString();
                _eventCounter++;
                _events.Add(ev);
            }
        }
    }
}
