namespace WebAPI.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Soulseek;
    using Soulseek.Exceptions;
    using WebAPI.DTO;

    /// <summary>
    ///     Users
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class UserController : ControllerBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="client"></param>
        public UserController(ISoulseekClient client)
        {
            Client = client;
        }

        private ISoulseekClient Client { get; }

        /// <summary>
        ///     Retrieves the address of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("{username}/address")]
        [ProducesResponseType(typeof(UserAddress), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Address([FromRoute, Required]string username)
        {
            try
            {
                var (ipAddress, port) = await Client.GetUserAddressAsync(username);
                return Ok(new UserAddress() { IPAddress = ipAddress.ToString(), Port = port });
            }
            catch (UserOfflineException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        ///     Retrieves the files shared by the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns></returns>
        [HttpGet("{username}/browse")]
        [ProducesResponseType(typeof(IEnumerable<Directory>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Browse([FromRoute, Required]string username)
        {
            try
            {
                var result = await Client.BrowseAsync(username);
                return Ok(result);
            }
            catch (UserOfflineException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        ///     Retrieves information about the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns></returns>
        [HttpGet("{username}/info")]
        [ProducesResponseType(typeof(UserInfo), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Info([FromRoute, Required]string username)
        {
            try
            {
                var response = await Client.GetUserInfoAsync(username);
                return Ok(response);
            }
            catch (UserOfflineException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        ///     Retrieves status for the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns></returns>
        [HttpGet("{username}/status")]
        [ProducesResponseType(typeof(DTO.UserStatus), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Status([FromRoute, Required]string username)
        {
            try
            {
                var response = await Client.GetUserStatusAsync(username);
                return Ok(new DTO.UserStatus() { Status = response.Status, IsPrivileged = response.IsPrivileged });
            }
            catch (UserOfflineException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}