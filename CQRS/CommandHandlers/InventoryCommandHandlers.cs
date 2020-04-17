using CQRS.Aggregates;
using CQRS.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.CommandHandlers
{
    public class InventoryCommandHandlers
    {
        private readonly IWriteRepository<InventoryItem> _repository;

        public InventoryCommandHandlers(IWriteRepository<InventoryItem> repository)
        {
            _repository = repository;
        }

        public async Task Handle(CreateInventoryItem message)
        {
            var item = new InventoryItem(message.InventoryItemId, message.Name);
            await _repository.Save(item);
        }

        public async Task Handle(DeactivateInventoryItem message)
        {
            var item = await _repository.GetById(message.InventoryItemId);
            item.Deactivate();
            await _repository.Save(item);
        }

        public async Task Handle(RemoveItemsFromInventory message)
        {
            var item = await _repository.GetById(message.InventoryItemId);
            item.Remove(message.Count);
            await _repository.Save(item);
        }

        public async Task Handle(CheckInItemsToInventory message)
        {
            var item = await _repository.GetById(message.InventoryItemId);
            item.CheckIn(message.Count);
            await _repository.Save(item);
        }

        public async Task Handle(RenameInventoryItem message)
        {
            var item = await _repository.GetById(message.InventoryItemId);
            item.ChangeName(message.NewName);
            await _repository.Save(item);
        }
    }
}
