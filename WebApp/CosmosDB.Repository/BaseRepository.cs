using CosmosDB.Domain;
using CosmosDB.Repository.Interfaces;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDB.Repository
{
    public abstract class BaseRepository<TRepository> : IRepository<TRepository> where TRepository : BaseDocument
    {
        string _databaseId;
        string _collectionId;

        int _maxItemCount;

        static DocumentClient _client;

        public string Type { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationSection"></param>
        public BaseRepository(IConfigurationSection configurationSection)
        {
            ConnectionPolicy connectionPolicy = new ConnectionPolicy
            {
                MaxConnectionLimit = 500,
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };

            connectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 2;
            connectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 5;

            _client = new DocumentClient(new Uri(configurationSection.GetSection("EndpointUri").Value),
                    configurationSection.GetSection("PrimaryKey").Value,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new DefaultContractResolver(),
                        NullValueHandling = NullValueHandling.Ignore
                    },
                    connectionPolicy: connectionPolicy);

            _databaseId = configurationSection.GetSection("DatabaseId").Value;
            _collectionId = configurationSection.GetSection("CollectionId").Value;

            _maxItemCount = int.Parse(configurationSection.GetSection("MaxItemCount").Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<BaseDocument> Add(BaseDocument entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.Type = this.Type;

            Document documentResponse = await _client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(
                    _databaseId,
                    _collectionId),
                entity);

            entity.Id = documentResponse.Id;

            return entity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="BaseDocument"></typeparam>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public async Task<BaseDocument> GetById<BaseDocument>(string entityId)
        {
            var documentQuery = _client.CreateDocumentQuery<BaseDocument>(
                    UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                    $"SELECT " +
                        $"* " +
                    $"FROM " +
                        $"c " +
                    $"WHERE " +
                        $"c.id = '{entityId}' " +
                    $"AND c.Type = '{Type}'").
                    AsDocumentQuery();

            var queryResult = await documentQuery.ExecuteNextAsync<BaseDocument>();

            return queryResult.Take(1).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="sortField"></param>
        /// <param name="order"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        public async Task<(List<T> documents, string responseContinuation)> Get<T>(string sortField, string order, string continuationToken)
        {
            string query =
                            $"SELECT " +
                                $"* " +
                            $"FROM " +
                                $"c " +
                            $"WHERE " +
                                $"c.Type = '{Type}' " +
                            $"ORDER BY " +
                                $"c.{sortField} {order}";

            return await Get<T>(query, continuationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="query"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        protected async Task<(List<T> documents, string responseContinuation)> Get<T>(string query, string continuationToken, short itemCount = 0)
        {
            FeedOptions feedOptions = new FeedOptions
            {
                MaxItemCount = itemCount > 0 && itemCount < _maxItemCount ? itemCount : _maxItemCount,
                RequestContinuation = continuationToken,
                PartitionKey = new PartitionKey(Type)
            };

            var documentQuery = _client.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                    query,
                    feedOptions).AsDocumentQuery();

            var queryResult = await documentQuery.ExecuteNextAsync<T>();

            List<T> resultList = queryResult.ToList();

            return (resultList, queryResult.ResponseContinuation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public async Task<bool> Delete(string entityId)
        {
            var document = _client.CreateDocumentQuery<Document>(
                    UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                    $"SELECT " +
                        $"c._self " +
                    $"FROM " +
                        $"c " +
                    $"WHERE " +
                        $"c.id = '{entityId}' " +
                    $"AND c.Type = '{Type}'").
                    ToList<Document>().
                    FirstOrDefault();

            if (document != null)
            {
                await _client.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = new PartitionKey(Type) });
            }

            return document != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<BaseDocument> Update(BaseDocument entity)
        {
            var document = _client.CreateDocumentQuery<Document>(
                    UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                    $"SELECT " +
                        $"* " +
                    $"FROM " +
                        $"c " +
                    $"WHERE " +
                        $"c.id = '{entity.Id}' " +
                    $"AND c.Type = '{Type}'").
                    ToList<Document>().
                    FirstOrDefault();

            if (document == null) return default(BaseDocument);

            entity.Type = this.Type;
            entity.CreatedBy = document.GetPropertyValue<Domain.User>("CreatedBy");
            entity.CreatedAt = document.GetPropertyValue<DateTime>("CreatedAt");
            entity.ModifiedAt = DateTime.UtcNow;

            var result = await _client.ReplaceDocumentAsync(document.SelfLink, entity,
                new RequestOptions
                {
                    AccessCondition = new AccessCondition
                    {
                        Condition = document.ETag,
                        Type = AccessConditionType.IfMatch
                    }
                });

            return entity;
        }
    }
}