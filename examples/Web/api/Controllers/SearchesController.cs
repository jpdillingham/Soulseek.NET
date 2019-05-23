namespace WebAPI.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Soulseek;

    /// <summary>
    ///     Search
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class SearchesController : ControllerBase
    {
        private ISoulseekClient Client { get; }
        private ISearchTracker Tracker { get; }

        public SearchesController(ISoulseekClient client, ISearchTracker tracker)
        {
            Client = client;
            Tracker = tracker;
        }

        /// <summary>
        ///     Performs a search for the specified <paramref name="searchText"/>.
        /// </summary>
        /// <param name="searchText">The search phrase.</param>
        /// <param name="token">The optional search token.</param>
        /// <returns></returns>
        [HttpPost("")]
        public async Task<IActionResult> Post([FromBody]string searchText, [FromQuery]int? token = null)
        {
            var results = await Client.SearchAsync(searchText, token, new SearchOptions(
                responseReceived: (e) => Tracker.AddOrUpdate(e), 
                stateChanged: (e) => Tracker.AddOrUpdate(e)));

            return Ok(results.ToList());
        }

        /// <summary>
        ///     Gets the status of the current search.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        public IActionResult Get()
        {
            var response = Tracker.Searches.Select(kvp => new
            {
                SearchText = kvp.Key,
                Token = kvp.Value.Token,
                State = kvp.Value.State,
                ResponseCount = kvp.Value.Responses.Count,
                TotalFileCount = kvp.Value.Responses.Sum(r => r.FileCount)
            });

            return Ok(response);
        }

        /// <summary>
        ///     Gets the status of the search corresponding to the specified <paramref name="searchText"/>.
        /// </summary>
        /// <param name="searchText">The search phrase of the desired search.</param>
        /// <returns></returns>
        [HttpGet("{searchText}")]
        public IActionResult Get([FromRoute]string searchText)
        {
            if (!Tracker.Searches.ContainsKey(searchText))
            {
                return NotFound();
            }

            return Ok(Tracker.Searches[searchText]);
        }
    }
}
