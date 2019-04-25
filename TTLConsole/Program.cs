using DocumentDB.Samples.Shared.Util;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TTLConsole
{
    class Program
    {
        private static DocumentClient client;

        private static readonly string DatabaseName = "samples";
        private static readonly string CollectionName = "ttl-sample";

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
                LogException(e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        private static async Task RunDemoAsync(string databaseId, string collectionId)
        {
            Database database = await client.CreateDatabaseIfNotExistsAsync(
                new Database { Id = databaseId });

            DocumentCollection collection = await GetOrCreateCollectionAsync(databaseId, collectionId);

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

            await CreateDocuments(collectionUri);

            int resultCount = 0;

            do
            {
                resultCount = await RetrieveDocuments(collectionUri);

                Thread.Sleep(1000);
            }
            while (resultCount > 1);

            await client.DeleteDocumentCollectionAsync(collectionUri);
        }

        private static async Task CreateDocuments(Uri collectionUri)
        {
            Person person = new Person
            {
                Name = $"John Doe",
                Address = new Address
                {
                    City = "Miami",
                    State = "Florida"
                }
            };

            ResourceResponse<Document> result = await client.UpsertDocumentAsync(collectionUri, person);

            Log.LogAndWait("UpsertDocumentAsync without TTL: ", result.RequestCharge);

            person = new Person
            {
                Name = $"John Doe (TTL)",
                Address = new Address
                {
                    City = "Miami",
                    State = "Florida"
                },
                Ttl = 10
            };

            result = await client.UpsertDocumentAsync(collectionUri, person);

            Log.LogAndWait("UpsertDocumentAsync with 10 seconds TTL: ", result.RequestCharge);
        }

        private static async Task<int> RetrieveDocuments(Uri collectionUri)
        {
            IDocumentQuery<dynamic> q = client.CreateDocumentQuery(collectionUri,
                "SELECT * FROM c WHERE c.Address.State = 'Florida'",
                new FeedOptions
                {
                    EnableCrossPartitionQuery = false,
                    PopulateQueryMetrics = true,
                    MaxItemCount = 100
                }).AsDocumentQuery();

            // Asynchronous call to perform the query
            FeedResponse<dynamic> result = await q.ExecuteNextAsync();

            foreach (var item in result.ToList())
            {
                Console.WriteLine(item.Name);
            }

            Console.WriteLine($"RetrieveDocuments >> Item Count: {result.Count} - Request Charge: {result.RequestCharge}");

            return result.Count;
        }

        private static async Task<DocumentCollection> GetOrCreateCollectionAsync(string databaseId, string collectionId)
        {
            DocumentCollection collectionDefinition = new DocumentCollection();
            collectionDefinition.Id = collectionId;
            collectionDefinition.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
            collectionDefinition.PartitionKey.Paths.Add("/Address/State");
            collectionDefinition.DefaultTimeToLive = -1;

            return await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(databaseId),
                collectionDefinition,
                new RequestOptions { OfferThroughput = 400 });
        }

        /// <summary>
        /// Log exception error message to the console
        /// </summary>
        /// <param name="e">The caught exception.</param>
        private static void LogException(Exception e)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Exception baseException = e.GetBaseException();
            if (e is DocumentClientException)
            {
                DocumentClientException de = (DocumentClientException)e;
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            else
            {
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }

            Console.ForegroundColor = color;
        }
    }

    internal class Person
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Name { get; set; }

        public Address Address { get; set; }

        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? Ttl { get; set; }
    }

    internal class Address
    {
        public string State { get; set; }

        public string City { get; set; }
    }
}