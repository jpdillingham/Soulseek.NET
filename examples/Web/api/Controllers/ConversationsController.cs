namespace WebAPI.Controllers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Soulseek;
    using WebAPI.DTO;
    using WebAPI.Entities;
    using WebAPI.Trackers;

    /// <summary>
    ///     Search
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class ConversationsController : ControllerBase
    {
        private ISoulseekClient Client { get; }
        private IConversationTracker Tracker { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConversationsController"/> class.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tracker"></param>
        public ConversationsController(ISoulseekClient client, IConversationTracker tracker)
        {
            Client = client;
            Tracker = tracker;
        }

        /// <summary>
        ///     Gets all tracked conversations.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("")]
        [Authorize]
        [ProducesResponseType(typeof(List<PrivateMessage>), 200)]
        public IActionResult GetAll()
        {
            return Ok(Tracker.Conversations);
        }

        /// <summary>
        ///     Gets the conversation associated with the specified username.
        /// </summary>
        /// <param name="username">The username associated with the desired conversation.</param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        /// <response code="404">A matching search was not found.</response>
        [HttpGet("{username}")]
        [Authorize]
        [ProducesResponseType(typeof(List<PrivateMessage>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetById([FromRoute]string username)
        {
            Tracker.Conversations.TryGetValue(username, out var conversation);

            if (conversation == default)
            {
                return NotFound();
            }

            return Ok(conversation.OrderBy(m => m.Timestamp));
        }
    }
}
