using CosmosDB.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDB.Repository.Interfaces
{
    public interface IRepository<T>
    {
        Task<BaseDocument> GetById<BaseDocument>(string entityId);

        Task<(List<BaseDocument> documents, string responseContinuation)> Get<BaseDocument>(string sortField, string order, string continuationToken);

        Task<BaseDocument> Add(BaseDocument entity);

        Task<BaseDocument> Update(BaseDocument entity);

        Task<bool> Delete(string entityId);
    }
}
