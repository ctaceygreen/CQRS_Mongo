using CQRS.Projections;
using CQRS.Projections.InventoryItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.ReadModels
{
    public interface IInventoryReadModel
    {
        Task<IEnumerable<InventoryItemListDto>> GetInventoryItems();
        Task<InventoryItemDetailsDto> GetInventoryItemDetails(Guid id);
    }

    public class InventoryItemDetailsDto
    {
        public string Id;
        public string Name;
        public int CurrentCount;
        public int Version;

        public InventoryItemDetailsDto(string id, string name, int currentCount, int version)
        {
            Id = id;
            Name = name;
            CurrentCount = currentCount;
            Version = version;
        }
    }

    public class InventoryItemListDto
    {
        public string Id;
        public string Name;

        public InventoryItemListDto(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class InventoryReadModel : IInventoryReadModel
    {
        private readonly IProjectionRepository<InventoryItemListProjection> _readRepository;
        private readonly IProjectionRepository<InventoryItemDetailsProjection> _readDetailsRepository;
        public InventoryReadModel(IProjectionRepository<InventoryItemListProjection> readRepository, IProjectionRepository<InventoryItemDetailsProjection> readDetailRepository)
        {
            _readRepository = readRepository;
            _readDetailsRepository = readDetailRepository;
        }
        public async Task<IEnumerable<InventoryItemListDto>> GetInventoryItems()
        {
            var items = (await _readRepository.Get("")).Items;
            return items.Select(i => new InventoryItemListDto(i.Key, i.Name));
        }

        public async Task<InventoryItemDetailsDto> GetInventoryItemDetails(Guid id)
        {
            var projection = await _readDetailsRepository.Get(id.ToString());

            return new InventoryItemDetailsDto(projection.Key, projection.Name, projection.CurrentCount, projection.Version);
        }
    }
}
