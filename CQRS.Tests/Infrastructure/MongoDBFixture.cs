using CQRS.Aggregates;
using CQRS.CommandHandlers;
using CQRS.Commands;
using CQRS.Events;
using CQRS.Events.InventoryItem;
using CQRS.Projections;
using CQRS.Projections.InventoryItem;
using CQRS.ReadModels;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using CQRS.MongoDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQRS.Tests.Infrastructure
{
    public class MongoDBFixture : IDisposable
    {
        public readonly Mediator Mediator;
        public IInventoryReadModel InventoryReadModel { get; set; }
        public IEventStorage EventStorage { get; set; }

        public IProjectionStore<InventoryItemListProjection> InventoryItemListProjectionStore { get; set; }
        public IProjectionStore<InventoryItemDetailsProjection> InventoryItemDetailsProjectionStore { get; set; }
        private readonly MongoRepositorySettings _eventMongoSettings;
        private readonly MongoRepositorySettings _listProjectionMongoSettings;
        private readonly MongoRepositorySettings _detailProjectionMongoSettings;
        private IMongoClient _mongoClient { get; set; }
        private readonly string _mongoConnectionString;
        public MongoDBFixture()
        {
            // Replace this connection string with whatever your mongo connection string is. 
            // Running a local mongo container in docker is easiest to test this out
            _mongoConnectionString = "mongodb://localhost:27017";
            _eventMongoSettings = new MongoRepositorySettings("CQRSEventsDB", "Events");
            _listProjectionMongoSettings = new MongoRepositorySettings("CQRSProjectionsDB", "InventoryItemList");
            _detailProjectionMongoSettings = new MongoRepositorySettings("CQRSProjectionsDB", "InventoryItemDetail");
            Mediator = new Mediator();
        }

        public void CreateMongoClient()
        {
            _mongoClient = new MongoClient(_mongoConnectionString);
            EventStorage = new MongoEventStorage(_mongoClient, _eventMongoSettings);

            // Ignore extra elements by convention
            var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            // Map immutable classes (still not allowing our events to have readonly fields, TODO:Work out why)
            ConventionRegistry.Register(nameof(ImmutableTypeClassMapConvention),
            new ConventionPack { new ImmutableTypeClassMapConvention() }, type => true);
        }

        public void EmptyEventStore()
        {
            _mongoClient.GetDatabase(_eventMongoSettings.DatabaseName).DropCollection(_eventMongoSettings.CollectionName);
        }

        public void EmptyProjectionStore()
        {
            // Simple in this situation just to drop the collections
            _mongoClient.GetDatabase(_listProjectionMongoSettings.DatabaseName).DropCollection(_listProjectionMongoSettings.CollectionName);
            _mongoClient.GetDatabase(_detailProjectionMongoSettings.DatabaseName).DropCollection(_detailProjectionMongoSettings.CollectionName);
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
            InventoryItemListProjectionStore = new MongoProjectionStore<InventoryItemListProjection>(_mongoClient, _listProjectionMongoSettings);
            InventoryItemDetailsProjectionStore = new MongoProjectionStore<InventoryItemDetailsProjection>(_mongoClient, _detailProjectionMongoSettings);

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
