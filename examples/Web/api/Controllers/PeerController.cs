namespace WebAPI.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Soulseek;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Search
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class PeerController : ControllerBase
    {
        private ISoulseekClient Client { get; }

        public PeerController(ISoulseekClient client)
        {
            Client = client;
        }

        /// <summary>
        ///     Retrieves information about the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to download.</param>
        /// <returns></returns>
        [HttpGet("{username}")]
        public async Task<PeerInfoResponse> Get([FromRoute, Required]string username)
        {
            var response = await Client.GetUserInfoAsync(username);
            return response;
        }
    }
}
