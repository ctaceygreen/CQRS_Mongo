using CQRS.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Projections
{
    public interface IProjectionStore<T> where T : Projection
    {
        Task<T> Get(string key);
        Task Save(T projection, string key);
    }

    public class FakeProjectionStore<T> : IProjectionStore<T> where T : Projection
    {
        private Dictionary<string, object> Current = null; // Just for mocking the DB, we don't make this type T, but object
        public FakeProjectionStore(BullShitDatabase db)
        { 
            // All just for fake database stuff
            if (!db.projections.ContainsKey(typeof(T)))
            {
                db.projections.Add(typeof(T), new Dictionary<string, object>());
            }
            Current = db.projections[typeof(T)];
        }
        public async Task<T> Get(string key)
        {
            //This would normally be a query on the database
            if (Current.ContainsKey(key))
            {
                return (T)Current[key];
            }
            return null;
        }

        public async Task Save(T projection, string key)
        {
            if (Current.ContainsKey(key))
            {
                Current[key] = projection;
            }
            else
            {
                Current.Add(key, projection);
            }
        }
    }

    public class BullShitDatabase
    {
        public Dictionary<Guid, InventoryItemDetailsDto> details = new Dictionary<Guid, InventoryItemDetailsDto>();
        public List<InventoryItemListDto> list = new List<InventoryItemListDto>();
        public Dictionary<Type, Dictionary<string, object>> projections = new Dictionary<Type, Dictionary<string, object>>();
    }
}
