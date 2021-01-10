using CQRS.Projections;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.AzureTable
{
    public class AzureTableProjectionStore<T> : IProjectionStore<T> where T : Projection
    {
        private readonly ILogger<AzureTableProjectionStore<T>> _log;
        private readonly string _tableName;
        private readonly CloudTableClient _tableClient;
        public AzureTableProjectionStore(ILogger<AzureTableProjectionStore<T>> log, string connectionStringVariableName, string tableName)
        {
            _log = log;
            _tableName = tableName;
            string storageConnectionString = Environment.GetEnvironmentVariable(connectionStringVariableName);

            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create a table client for interacting with the table service
            _tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        }
        public async Task<T> Get(string key)
        {
            CloudTable table = _tableClient.GetTableReference(_tableName);
            var profileAdapter = await table.ExecuteAsync(TableOperation.Retrieve<TableEntityAdapter<T>>("", key));
            return profileAdapter.Result as T;
        }

        public async Task Save(T projection, string key)
        {
            CloudTable table = _tableClient.GetTableReference(_tableName);
            await table.ExecuteAsync(TableOperation.InsertOrMerge(new TableEntityAdapter<T>(projection, "", key)));
        }
    }
}
