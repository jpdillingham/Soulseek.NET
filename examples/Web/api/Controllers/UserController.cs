namespace WebAPI.Controllers
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Soulseek;
    using Soulseek.Exceptions;

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
        private ISoulseekClient Client { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="client"></param>
        public UserController(ISoulseekClient client)
        {
            Client = client;
        }

        /// <summary>
        ///     Retrieves information about the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to download.</param>
        /// <returns></returns>
        [HttpGet("{username}/info")]
        public async Task<IActionResult> Info([FromRoute, Required]string username)
        {
            UserInfo response;

            try
            {
                response = await Client.GetUserInfoAsync(username);
            }
            catch (UserOfflineException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

            return Ok(response);
        }
    }
}
