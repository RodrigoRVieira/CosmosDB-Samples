using CosmosDB.Domain;
using CosmosDB.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDB.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase<User>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userRepository"></param>
        public UserController(IRepository<User> userRepository) : base(userRepository) { }

        /// <summary>
        /// Adds a new User.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/User
        ///     {
        ///        "Name": "John Doe",
        ///        "Email": "johndoe@geocities.com"
        ///     }
        ///
        /// </remarks>
        /// <param name="user">User payload</param>
        /// <returns>A newly created User.</returns>
        /// <response code="201">The User was successfully created.</response>
        /// <response code="400">The request payload is malformed.</response>
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [HttpPost]
        public async Task<ActionResult<User>> Post(User user)
        {
            User _user = (User)await base.Post(user);

            return CreatedAtAction(nameof(base.GetById), new { documentId = _user.Id }, _user);
        }

        /// <summary>
        /// Updates an existing User.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PATCH /api/User
        ///     {
        ///        "Id": "d7a0d62d-ed7e-4f7a-8a71-2b7761632321"
        ///        "Name": "John Doe",
        ///        "Email": "johndoe@geocities.com"
        ///     }
        ///
        /// </remarks>
        /// <param name="user">Updated User</param>
        /// <returns>The User that was updated.</returns>
        /// <response code="200">The User was successfully updated.</response>
        /// <response code="204">The User with the given Id doesn't exists.</response>
        /// <response code="400">The request payload is malformed.</response>
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [HttpPatch]
        public async Task<ActionResult<User>> Patch(User user)
        {
            User _user = (User)await base.Patch(user);

            return _user != null ?
                (ActionResult)Ok(_user) :
                NoContent();
        }
    }
}
