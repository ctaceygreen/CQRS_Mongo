using CQRS.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Projections
{
    // Used to handle events and save snapshots of projections to our projection store. This should improve performance of our read model when we have many events, as 
    // we only need to process the events since the snapshot.
    public class ProjectionHandler<T> where T : Projection, new()
    {
        IProjectionRepository<T> _repository;
        public ProjectionHandler(IProjectionRepository<T> repository)
        {
            _repository = repository;
        }
        public async Task Handle(Event message)
        {
            var temp = new T();
            var key = temp.AsDynamic().GetKeyFromMessage(message);
            if (key == null)
            {
                throw new Exception($"{typeof(T).Name} has not implemented Handle for {message.GetType().Name}");
            }

            //Get projection
            var currentProjection = await _repository.Get(key);

            await _repository.Save(currentProjection);
        }
    }

    public interface IProjectionHandlerRepository
    {
        Task<string> GetLastStreamId<T>();
        Task Save<T>(string lastStreamId);
    }

    public class ProjectionHandler
    {
        public string ProjectionType { get; set; }
        public string LastStreamId { get; set; }
    }
}
