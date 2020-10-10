namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Soulseek;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using WebAPI.Entities;
    using WebAPI.Trackers;

    /// <summary>
    ///     Rooms.
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class RoomsController : ControllerBase
    {
        public RoomsController(ISoulseekClient client, IRoomTracker tracker)
        {
            Client = client;
            Tracker = tracker;
        }

        private ISoulseekClient Client { get; }
        private IRoomTracker Tracker { get; }

        /// <summary>
        ///     Gets all tracked rooms.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("joined")]
        [Authorize]
        [ProducesResponseType(typeof(Dictionary<string, List<RoomMessage>>), 200)]
        public IActionResult GetAll()
        {
            return Ok(Tracker.Rooms);
        }

        /// <summary>
        ///     Gets all messages for the specified roomName.
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        /// <response code="404">The specified roomName could not be found.</response>
        [HttpGet("joined/{roomName}")]
        [Authorize]
        [ProducesResponseType(typeof(List<RoomMessage>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetByRoomName([FromRoute]string roomName)
        {
            if (Tracker.TryGet(roomName, out var messages))
            {
                return Ok(messages.OrderBy(m => m.Timestamp));
            }

            return NotFound();
        }

        /// <summary>
        ///     Gets a list of rooms from the server.
        /// </summary>
        /// <returns></returns>
        [HttpGet("available")]
        [Authorize]
        [ProducesResponseType(typeof(List<Room>), 200)]
        public async Task<IActionResult> GetRooms()
        {
            return Ok(await Client.GetRoomListAsync());
        }

        /// <summary>
        ///     Joins a room.
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        /// <response code="201">The request completed successfully.</response>
        /// <response code="304">The room has already been joined.</response>
        [HttpPost("joined/{roomName}")]
        [Authorize]
        [ProducesResponseType(typeof(RoomData), 201)]
        [ProducesResponseType(304)]
        public async Task<IActionResult> JoinRoom([FromRoute]string roomName)
        {
            if (Tracker.Rooms.ContainsKey(roomName))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return StatusCode(StatusCodes.Status201Created, await Client.JoinRoomAsync(roomName));
        }

        /// <summary>
        ///     Leaves a room.
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        /// <response code="204">The request completed successfully.</response>
        /// <response code="404">The room has not been joined.</response>
        [HttpDelete("joined/{roomName}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LeaveRoom([FromRoute]string roomName)
        {
            if (!Tracker.Rooms.ContainsKey(roomName))
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }

            await Client.LeaveRoomAsync(roomName);
            Tracker.TryRemove(roomName);

            return StatusCode(StatusCodes.Status204NoContent);
        }
    }
}