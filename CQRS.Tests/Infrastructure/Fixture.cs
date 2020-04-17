using CQRS.Aggregates;
using CQRS.CommandHandlers;
using CQRS.Commands;
using CQRS.Events;
using CQRS.Events.InventoryItem;
using CQRS.Projections;
using CQRS.Projections.InventoryItem;
using CQRS.ReadModels;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace CQRS.Tests.Infrastructure
{
    public class Fixture : IDisposable
    {
        public readonly Mediator Mediator;
        public IInventoryReadModel InventoryReadModel { get; set; }
        public IEventStorage EventStorage { get; set; }

        public IProjectionStore<InventoryItemListProjection> InventoryItemListProjectionStore { get; set; }
        public IProjectionStore<InventoryItemDetailsProjection> InventoryItemDetailsProjectionStore { get; set; }
        public Fixture()
        {
            Mediator = new Mediator();
        }

        public void EmptyEventStore()
        {
            EventStorage = new BullshitEventStorage();
        }

        public void ComposeHandlers()
        {
            //Create our event store
            var storage = new EventStore(Mediator, EventStorage);

            // Create write repository and command handlers
            var rep = new WriteRepository<InventoryItem>(storage);
            var commands = new InventoryCommandHandlers(rep);
            Mediator.RegisterHandler<CheckInItemsToInventory>(commands.Handle);
            Mediator.RegisterHandler<CreateInventoryItem>(commands.Handle);
            Mediator.RegisterHandler<DeactivateInventoryItem>(commands.Handle);
            Mediator.RegisterHandler<RemoveItemsFromInventory>(commands.Handle);
            Mediator.RegisterHandler<RenameInventoryItem>(commands.Handle);

            // Create our projection stores
            BullShitDatabase db = new BullShitDatabase();
            InventoryItemListProjectionStore = new FakeProjectionStore<InventoryItemListProjection>(db);
            InventoryItemDetailsProjectionStore = new FakeProjectionStore<InventoryItemDetailsProjection>(db);

            // Create read model, passing projection repositories which use an event store and a projection store
            InventoryReadModel =
                new InventoryReadModel(
                    new ProjectionRepository<InventoryItemListProjection>(
                        new ReadEventStore(EventStorage),
                        InventoryItemListProjectionStore
                    ),
                    new ProjectionRepository<InventoryItemDetailsProjection>(
                        new ReadEventStore(EventStorage),
                        InventoryItemDetailsProjectionStore
                    )
                );
        }

        public void ComposeProjectionHandlers()
        {
            // Compose projection handlers, which save snapshots of the projections to our store, improving performance when we have many events
            var invProjectionHandler = new ProjectionHandler<InventoryItemListProjection>(new ProjectionRepository<InventoryItemListProjection>(new ReadEventStore(EventStorage), InventoryItemListProjectionStore));
            Mediator.RegisterHandler<InventoryItemCreated>(invProjectionHandler.Handle);
            var invDetailProjectionHandler = new ProjectionHandler<InventoryItemDetailsProjection>(new ProjectionRepository<InventoryItemDetailsProjection>(new ReadEventStore(EventStorage), InventoryItemDetailsProjectionStore));
            Mediator.RegisterHandler<InventoryItemCreated>(invDetailProjectionHandler.Handle);
            Mediator.RegisterHandler<ItemsCheckedInToInventory>(invDetailProjectionHandler.Handle);
        }
        public void Dispose()
        {
            
        }
    }
}
