using Bogus;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartitioningConsole
{
    class Program
    {
        private static DocumentClient client;

        private static readonly string DatabaseName = "samples";
        private static readonly string CollectionName = "partitioning-samples";
        private static readonly string CollectionNameSuffix = "partitioning-samples-suffix";

        private static readonly string endpointUrl = ConfigurationManager.AppSettings["EndPointUrl"];
        private static readonly string authorizationKey = ConfigurationManager.AppSettings["AuthorizationKey"];

        private static readonly Faker faker = new Faker();

        private static readonly Bogus.DataSets.Name nameGenerator = new Bogus.DataSets.Name("pt_BR");
        private static readonly Bogus.DataSets.Address addressGenerator = new Bogus.DataSets.Address("pt_BR");

        static void Main(string[] args)
        {
            try
            {
                using (client = new DocumentClient(new Uri(endpointUrl), authorizationKey,
                    new ConnectionPolicy { ConnectionMode = ConnectionMode.Gateway, ConnectionProtocol = Protocol.Https }))
                {
                    Uri collectionUri = RunDemoAsync(DatabaseName, CollectionName, false).Result;

                    Uri collectionUriSuffix = RunDemoAsync(DatabaseName, CollectionNameSuffix, true).Result;

                    RetrieveDocumentsWithPartitionKey(collectionUri).Wait();

                    RetrieveDocumentsWithoutPartitionKey(collectionUriSuffix).Wait();
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

        private static async Task<Uri> RunDemoAsync(string databaseId, string collectionId, bool addSuffix)
        {
            Microsoft.Azure.Documents.Database database = await client.CreateDatabaseIfNotExistsAsync(
                new Microsoft.Azure.Documents.Database { Id = databaseId });

            DocumentCollection collection = await GetOrCreateCollectionAsync(databaseId, collectionId);

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);

            // await CreateDocuments(collectionUri, addSuffix);

            return collectionUri;

            // await client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
        }

        private static async Task<DocumentCollection> GetOrCreateCollectionAsync(string databaseId, string collectionId)
        {
            DocumentCollection collectionDefinition = new DocumentCollection();
            collectionDefinition.Id = collectionId;
            collectionDefinition.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
            collectionDefinition.PartitionKey.Paths.Add("/Address/State");

            return await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(databaseId),
                collectionDefinition,
                new RequestOptions { OfferThroughput = 400 });
        }

        private static async Task CreateDocuments(Uri collectionUri, bool addSuffix)
        {
            var taskCollection = new List<Task>();

            for (var i = 0; i < 100; i++)
            {
                taskCollection.Add(AddPerson(collectionUri, addSuffix));
            }

            await Task.WhenAll(taskCollection);
        }

        private static async Task AddPerson(Uri collectionUri, bool addSuffix)
        {
            for (short i = 0; i < 500; i++)
            {
                Person person = new Person
                {
                    Name = $"{nameGenerator.FirstName()} {nameGenerator.LastName()}",
                    Address = new Address
                    {
                        City = addressGenerator.City(),
                        State = $"{addressGenerator.State()}{(addSuffix ? "-" + faker.Random.Number(1, 999) : "")}"
                    }
                };

                await client.UpsertDocumentAsync(collectionUri, person);

                await Task.Delay(50);
            }
        }

        private static async Task RetrieveDocumentsWithPartitionKey(Uri collectionUri)
        {
            IDocumentQuery<dynamic> q = client.CreateDocumentQuery(collectionUri,
                "SELECT * FROM c WHERE c.Address.State = 'Minas Gerais'",
                new FeedOptions
                {
                    EnableCrossPartitionQuery = false,
                    PopulateQueryMetrics = true,
                    MaxItemCount = 100
                }).AsDocumentQuery();

            // Asynchronous call to perform the query
            FeedResponse<dynamic> result = await q.ExecuteNextAsync();

            Console.WriteLine($"RetrieveDocumentsWithPartitionKey >> Item Count: {result.Count} - Request Charge: {result.RequestCharge}");
        }

        private static async Task RetrieveDocumentsWithoutPartitionKey(Uri collectionUri)
        {
            IDocumentQuery<dynamic> q = client.CreateDocumentQuery(collectionUri,
                "SELECT * FROM c WHERE STARTSWITH(c.Address.State, 'Minas Gerais')",
                new FeedOptions
                {
                    EnableCrossPartitionQuery = true,
                    PopulateQueryMetrics = true,
                    MaxItemCount = 100
                }).AsDocumentQuery();

            // Asynchronous call to perform the query
            FeedResponse<dynamic> result = await q.ExecuteNextAsync();

            Console.WriteLine($"RetrieveDocumentsWithoutPartitionKey >> Item Count: {result.Count} - Request Charge: {result.RequestCharge}");
        }

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

        internal class Person
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            public string Name { get; set; }

            public Address Address { get; set; }
        }

        internal class Address
        {
            public string State { get; set; }

            public string City { get; set; }
        }
    }
}
