using System;
using System.Collections.Generic;
using System.Text;

namespace CQRS.MongoDB
{
    public class MongoRepositorySettings
    {
        public MongoRepositorySettings(string databaseName, string collectionName)
        {
            DatabaseName = databaseName;
            CollectionName = collectionName;
        }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
    }
}
