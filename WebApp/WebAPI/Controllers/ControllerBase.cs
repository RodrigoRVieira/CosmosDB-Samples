using CosmosDB.Domain;
using CosmosDB.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDB.WebAPI.Controllers
{
    public abstract class ControllerBase<T> : ControllerBase
    {
        protected IRepository<T> _documentRepository;

        protected const string _continuationTokenHeaderName = "X-RequestContinuationToken";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentRepository"></param>
        public ControllerBase(IRepository<T> documentRepository)
        {
            _documentRepository = documentRepository;
        }

        /// <summary>
        /// Retrieves a Document list.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/[DocumentType]/CreatedAt/DESC
        ///
        /// </remarks>
        /// <param name="sortField">Sorting field. Ex. CreatedAt</param>
        /// <param name="order">Ex. ASC/DESC</param>
        /// <returns>Document list.</returns>
        /// <response code="204">No Documents with the given type were found.</response>
        /// <response code="200">Documents list.</response>
        [ProducesResponseType(204)]
        [ProducesResponseType(200)]
        [HttpGet("{sortField}/{order}")]
        public virtual async Task<ActionResult<List<T>>> Get(string sortField, string order)
        {
            var documentList = await (_documentRepository).Get<T>(sortField, order, GetContinuationToken());

            SetContinuationToken(documentList.responseContinuation);

            return documentList.documents != null && documentList.documents.Count > 0 ?
                (ActionResult)Ok(documentList.documents) :
                NoContent();
        }

        /// <summary>
        /// Retrieves a Document through Id.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/[DocumentType]/GetById/6196ea4a-9574-483c-b53c-8f9fa50a981c
        ///
        /// </remarks>
        /// <param name="documentId">Document Id</param>
        /// <returns>A Document.</returns>
        /// <response code="204">A Document with the given Id and Type wasn't found.</response>
        /// <response code="200">Document with the given Id.</response>
        [ProducesResponseType(204)]
        [ProducesResponseType(200)]
        [HttpGet("GetById/{documentId}")]
        public virtual async Task<ActionResult<T>> GetById(string documentId)
        {
            T document = await (_documentRepository).GetById<T>(documentId);

            return document != null ?
                (ActionResult)Ok(document) :
                NoContent();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        [NonAction]
        public async Task<BaseDocument> Post(BaseDocument document)
        {
            BaseDocument _document = await _documentRepository.Add(document);

            return _document;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        [NonAction]
        public async Task<BaseDocument> Patch(BaseDocument document)
        {
            BaseDocument _document = await _documentRepository.Update(document);

            return _document;
        }

        /// <summary>
        /// Deletes the Document with the given Id.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     DELETE /api/[DocumentType]/6196ea4a-9574-483c-b53c-8f9fa50a981c
        ///
        /// </remarks>
        /// <param name="documentId">Document Id</param>
        /// <returns>Deleted Yes/No.</returns>
        /// <response code="204">A Document with the given Id and Type wasn't found.</response>
        /// <response code="200">Document with the given Id was deleted.</response>
        [ProducesResponseType(204)]
        [ProducesResponseType(200)]
        [HttpDelete("{documentId}")]
        public virtual async Task<ActionResult> Delete(string documentId)
        {
            bool deleteResult = await _documentRepository.Delete(documentId);

            return deleteResult ?
                (ActionResult)Ok() :
                NoContent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal string GetContinuationToken()
        {
            Request.Headers.TryGetValue(_continuationTokenHeaderName, out StringValues continuationTokenHeader);

            return continuationTokenHeader.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="continuationToken"></param>
        internal void SetContinuationToken(string continuationToken)
        {
            if (!string.IsNullOrEmpty(continuationToken))
            {
                Response.Headers.Add("Access-Control-Expose-Headers", _continuationTokenHeaderName);
                Response.Headers.Add(_continuationTokenHeaderName, continuationToken);
            }
        }
    }
}
