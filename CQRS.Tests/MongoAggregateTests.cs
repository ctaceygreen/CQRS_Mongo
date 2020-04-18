using CQRS.Commands;
using CQRS.Events.InventoryItem;
using CQRS.Tests.Infrastructure;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xbehave;
using Xunit;

namespace CQRS.Tests
{
    // Sequential purely due to the shared integration with the same mongo collections
    [Collection("Sequential")]
    public class MongoAggregateTests
    {

        [Scenario]
        public void InventoryItemAggregate_CreatedSuccessfully()
        {
            var fixture = new MongoDBFixture();
            string inventoryItemName = "TestItem";
            Guid inventoryItemId = Guid.NewGuid();
            int numberOfCheckIns = 2;
            $"Given I have no inventory item and have registered command handlers".x(async () =>
            {
                fixture.CreateMongoClient();
                fixture.EmptyEventStore();
                fixture.EmptyProjectionStore();
                fixture.ComposeHandlers();
                fixture.ComposeProjectionStore();
                fixture.ComposeReadModel();
            });
            $"When I send a command to create the inventory item".x(async () =>
            {
                await fixture.Mediator.Send(new CreateInventoryItem(inventoryItemId, inventoryItemName));
            });
            $"Then a created event is created".x(async () =>
            {
                var events = await fixture.EventStorage.GetEventsForAggregate(inventoryItemId);
                events.Count.ShouldBe(1);
                events.First().Type.ShouldBe(typeof(InventoryItemCreated).FullName);
            });
            $"And then when I check in items to the inventory item".x(async () =>
            {
                await fixture.Mediator.Send(new CheckInItemsToInventory(inventoryItemId, numberOfCheckIns));
            });
            $"Another inventory item is not created and a new check-in event is created".x(async () =>
            {
                var events = await fixture.EventStorage.GetEventsForAggregate(inventoryItemId);
                events.Count.ShouldBe(2);
                events.Where(e => e.Type == typeof(InventoryItemCreated).FullName).Count().ShouldBe(1);
            });
            $"And items are checked into our inventory item".x(async () =>
            {
                var detailsReadModel = await fixture.InventoryReadModel.GetInventoryItemDetails(inventoryItemId);
                detailsReadModel.Name.ShouldBe(inventoryItemName);
                detailsReadModel.CurrentCount.ShouldBe(numberOfCheckIns);
            });
            $"And the projection stores were not used".x(async () =>
            {
                var projectionDetailsStore = await fixture.InventoryItemDetailsProjectionStore.Get(inventoryItemId.ToString());
                projectionDetailsStore.ShouldBeNull();
                var projectionStore = await fixture.InventoryItemListProjectionStore.Get(""); // Gets all items
                projectionStore.ShouldBeNull();
            });

        }

        [Scenario]
        public void InventoryItemReadModel_UsesProjectionStore_WhenProjectionHandlersAreRunning()
        {
            var fixture = new MongoDBFixture();
            string inventoryItemName = "TestItem";
            Guid inventoryItemId = Guid.NewGuid();
            int numberOfCheckIns = 2;
            $"Given I have no inventory item and have registered command and projection handlers".x(async () =>
            {
                fixture.CreateMongoClient();
                fixture.EmptyEventStore();
                fixture.EmptyProjectionStore();
                fixture.ComposeHandlers();
                fixture.ComposeProjectionStore();
                fixture.ComposeReadModel();
                fixture.ComposeProjectionHandlers();
            });
            $"When I send a command to create the inventory item".x(async () =>
            {
                await fixture.Mediator.Send(new CreateInventoryItem(inventoryItemId, inventoryItemName));
            });
            $"Then a created event is created".x(async () =>
            {
                var events = await fixture.EventStorage.GetEventsForAggregate(inventoryItemId);
                events.Count.ShouldBe(1);
                events.First().Type.ShouldBe(typeof(InventoryItemCreated).FullName);
            });
            $"And then when I check in items to the inventory item".x(async () =>
            {
                await fixture.Mediator.Send(new CheckInItemsToInventory(inventoryItemId, numberOfCheckIns));
            });
            $"And we wait for our projection handlers to complete".x(async () =>
            {
                Thread.Sleep(3000);
            });
            $"Another inventory item is not created and a new check-in event is created".x(async () =>
            {
                var events = await fixture.EventStorage.GetEventsForAggregate(inventoryItemId);
                events.Count.ShouldBe(2);
                events.Where(e => e.Type == typeof(InventoryItemCreated).FullName).Count().ShouldBe(1);
            });
            $"And items are checked into our inventory item".x(async () =>
            {
                var detailsReadModel = await fixture.InventoryReadModel.GetInventoryItemDetails(inventoryItemId);
                detailsReadModel.Name.ShouldBe(inventoryItemName);
                detailsReadModel.CurrentCount.ShouldBe(numberOfCheckIns);
            });
            $"And the projection stores are populated".x(async () =>
            {
                var projectionDetailsStore = await fixture.InventoryItemDetailsProjectionStore.Get(inventoryItemId.ToString());
                projectionDetailsStore.ShouldNotBeNull();
                var projectionStore = await fixture.InventoryItemListProjectionStore.Get(""); // Gets all items
                projectionStore.ShouldNotBeNull();
            });
            $"Then if the events store goes down".x(async () =>
            {
                fixture.Mediator.ClearHandlers();
                fixture.EventStorage = null;
                fixture.ComposeHandlers();
                fixture.ComposeReadModel();
            });
            $"Our read models will still return from the projection store".x(async () =>
            {
                var detailsReadModel = await fixture.InventoryReadModel.GetInventoryItemDetails(inventoryItemId);
                detailsReadModel.Name.ShouldBe(inventoryItemName);
                detailsReadModel.CurrentCount.ShouldBe(numberOfCheckIns);
            });

        }

    }
}
