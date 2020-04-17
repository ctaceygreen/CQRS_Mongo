using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Aggregates
{
    public interface IWriteRepository<T> where T : AggregateRoot, new()
    {
        Task Save(AggregateRoot aggregate);
        Task<T> GetById(Guid id);
    }



    public class WriteRepository<T> : IWriteRepository<T> where T : AggregateRoot, new()
    {
        private readonly IEventStore _storage;

        public WriteRepository(IEventStore storage)
        {
            _storage = storage;
        }

        public async Task Save(AggregateRoot aggregate)
        {
            await _storage.SaveEvents(aggregate.Id, aggregate.GetUncommittedChanges(), aggregate.Version);
        }

        public async Task<T> GetById(Guid id)
        {
            var obj = new T();
            var e = await _storage.GetEventsForAggregate(id);
            obj.LoadsFromHistory(e);
            return obj;
        }
    }
}
