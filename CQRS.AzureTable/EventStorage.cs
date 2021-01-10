using CQRS.Events;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CQRS.AzureTable
{
    public class AzureTableEventStorage : IEventStorage
    {
        private readonly ILogger<AzureTableEventStorage> _log;
        private readonly string _tableName;
        private readonly CloudTableClient _tableClient;
        public AzureTableEventStorage(ILogger<AzureTableEventStorage> log, string connectionStringVariableName, string tableName)
        {
            _log = log;
            _tableName = tableName;
            string storageConnectionString = Environment.GetEnvironmentVariable(connectionStringVariableName);

            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create a table client for interacting with the table service
            _tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        }
        public async Task<List<Event>> GetEventsForAggregate(Guid aggregateId)
        {
            CloudTable table = _tableClient.GetTableReference(_tableName);
            TableContinuationToken token = null;
            var eventsSegment = await table
                .ExecuteQuerySegmentedAsync<TableEntityAdapter<Event>>(
                    new TableQuery<TableEntityAdapter<Event>>()
                    .Where(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, aggregateId.ToString())), 
                    token);
            //TODO: deal with more than 1000 results by using continuation token
            List<Event> events = new List<Event>();
            foreach(var evt in eventsSegment)
            {
                events.Add(evt.OriginalEntity);
            }
            return events;
        }

        public Task<List<Event>> GetEventsSince(string id)
        {
            throw new NotImplementedException();
        }

        public async Task SaveNewEventsForAggregate(Guid aggregateId, List<Event> newEvents)
        {
            // Create the batch operation. 
            CloudTable table = _tableClient.GetTableReference(_tableName);
            TableBatchOperation batchOperation = new TableBatchOperation();
            foreach(var evt in newEvents)
            {
                batchOperation.Insert(new TableEntityAdapter<Event>(evt, evt.AggregateId.ToString(), evt.Id));
            }
            await table.ExecuteBatchAsync(batchOperation);
        }
    }
}
