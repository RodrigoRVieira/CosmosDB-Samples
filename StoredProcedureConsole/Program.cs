using DocumentDB.Samples.Shared.Util;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoredProcedureConsole
{
    class Program
    {
        private static DocumentClient client;

        private static readonly string DatabaseName = "samples";
        private static readonly string CollectionName = "sp-sample";

        private static readonly string endpointUrl = ConfigurationManager.AppSettings["EndPointUrl"];
        private static readonly string authorizationKey = ConfigurationManager.AppSettings["AuthorizationKey"];

        static void Main(string[] args)
        {
            try
            {
                using (client = new DocumentClient(new Uri(endpointUrl), authorizationKey,
                    new ConnectionPolicy { ConnectionMode = ConnectionMode.Gateway, ConnectionProtocol = Protocol.Https }))
                {
                    RunDemoAsync(DatabaseName, CollectionName).Wait();
                }
            }
            catch (Exception e)
            {
                Log.LogException(e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        static async Task RunDemoAsync(string databaseId, string collectionId)
        {
            Database database = await client.CreateDatabaseIfNotExistsAsync(
                new Database { Id = databaseId });

            DocumentCollection collection = await GetOrCreateCollectionAsync(databaseId, collectionId);

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

            await CreateStoredProcedure();

            await CreateItem();

            // await client.DeleteDocumentCollectionAsync(collectionUri);
        }

        private static async Task CreateItem()
        {
            dynamic newItem = new
            {
                Category = "Personal",
                Name = "Groceries",
                Description = "Pick up strawberries",
                IsComplete = false
            };

            Uri uri = UriFactory.CreateStoredProcedureUri(DatabaseName, CollectionName, "spCreateToDoItem");
            RequestOptions options = new RequestOptions { PartitionKey = new PartitionKey("Personal") };
            var result = await client.ExecuteStoredProcedureAsync<string>(uri, options, newItem);
            var id = result.Response;
        }

        private static async Task CreateStoredProcedure()
        {
            string storedProcedureId = "spCreateToDoItem";

            StoredProcedure newStoredProcedure = new StoredProcedure
            {
                Id = storedProcedureId,
                Body = File.ReadAllText($@"JS\{storedProcedureId}.js")
            };

            Uri containerUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            var response = await client.CreateStoredProcedureAsync(containerUri, newStoredProcedure);

            StoredProcedure createdStoredProcedure = response.Resource;
        }

        private static async Task<DocumentCollection> GetOrCreateCollectionAsync(string databaseId, string collectionId)
        {
            DocumentCollection collectionDefinition = new DocumentCollection();
            collectionDefinition.Id = collectionId;
            collectionDefinition.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
            collectionDefinition.PartitionKey.Paths.Add("/Category");
            collectionDefinition.DefaultTimeToLive = -1;

            return await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(databaseId),
                collectionDefinition,
                new RequestOptions { OfferThroughput = 400 });
        }
    }
}
