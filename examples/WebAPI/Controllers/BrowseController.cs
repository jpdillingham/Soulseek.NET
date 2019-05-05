namespace WebAPI.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
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
    public class BrowseController : ControllerBase
    {
        private ISoulseekClient Client { get; }

        public BrowseController(ISoulseekClient client)
        {
            Client = client;
        }

        /// <summary>
        ///     Retrieves the files shared by the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user to browse.</param>
        /// <returns></returns>
        [HttpGet("{username}")]
        public async Task<ActionResult<BrowseResponse>> Get([FromRoute, Required]string username)
        {
            var result = await Client.BrowseAsync(username);
            return result;
        }
    }
}
