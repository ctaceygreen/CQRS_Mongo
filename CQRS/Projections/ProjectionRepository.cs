using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Projections
{
    public interface IProjectionRepository<T> where T : Projection, new()
    {
        Task<T> Get(string key);
        Task Save(T proj);
    }

    public class ProjectionRepository<T> : IProjectionRepository<T> where T : Projection, new()
    {
        private readonly IReadEventStore _storage;
        private readonly IProjectionStore<T> _projectionStorage;

        public ProjectionRepository(IReadEventStore storage, IProjectionStore<T> projectionStorage)
        {
            _storage = storage;
            _projectionStorage = projectionStorage;
        }
        
        public async Task<T> Get(string key)
        {
            // If we can't find a projection in our store, then let's build one from the events
            var currentProjection = await _projectionStorage.Get(key);
            if (currentProjection == null)
            {
                currentProjection = new T();
                currentProjection.Key = key;
            }
            var e = await _storage.GetEventsSince(currentProjection.LastEventId);
            currentProjection.LoadsFromHistory(e);
            return currentProjection;
        }

        public async Task Save(T proj)
        {
            await _projectionStorage.Save(proj, proj.Key);
        }
    }
}
