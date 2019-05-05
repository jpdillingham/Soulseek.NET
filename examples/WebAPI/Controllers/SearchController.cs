namespace WebAPI.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Soulseek.NET;
    using Soulseek.NET.Messaging.Messages;

    /// <summary>
    ///     Search
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class SearchController : ControllerBase
    {
        private ISoulseekClient Client { get; }

        public SearchController(ISoulseekClient client)
        {
            Client = client;
        }

        /// <summary>
        ///     Performs a search for the specified <paramref name="searchText"/>.
        /// </summary>
        /// <param name="searchText">The search phrase.</param>
        /// <param name="token">The optional search token.</param>
        /// <returns></returns>
        [HttpGet("{searchText}")]
        public async Task<ActionResult<IEnumerable<SearchResponse>>> Get([FromRoute, Required]string searchText, [FromQuery]int? token = null)
        {
            var results = await Client.SearchAsync(searchText, token);
            return results.ToList();
        }
    }
}
