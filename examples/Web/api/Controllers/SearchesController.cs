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
            Tracker.Clear();

            var results = await Client.SearchAsync(searchText, token, new SearchOptions(
                responseReceived: (e) => Tracker.AddOrUpdate(e), 
                stateChanged: (e) => Tracker.AddOrUpdate(e)));

            return Ok(results.ToList());
        }

        /// <summary>
        ///     Gets the state of all current searches.
        /// </summary>
        /// <returns></returns>
        [HttpGet("")]
        public IActionResult Get()
        {
            var response = Tracker.Searches.Select(kvp => new
            {
                SearchText = kvp.Key,
                kvp.Value.Token,
                kvp.Value.State,
                kvp.Value.ResponseCount,
                kvp.Value.FileCount
            });

            return Ok(response);
        }

        /// <summary>
        ///     Gets the state of the search corresponding to the specified <paramref name="searchText"/>.
        /// </summary>
        /// <param name="searchText">The search phrase of the desired search.</param>
        /// <returns></returns>
        [HttpGet("{searchText}")]
        public IActionResult GetBySearchText([FromRoute]string searchText)
        {
            Tracker.Searches.TryGetValue(searchText, out var search);

            if (search == default)
            {
                return NotFound();
            }

            return Ok(new
            {
                search.SearchText,
                search.Token,
                search.State,
                search.ResponseCount,
                search.FileCount
            });
        }

        /// <summary>
        ///     Gets the state of the search corresponding to the specified <paramref name="token"/>.
        /// </summary>
        /// <param name="token">The token of the desired search.</param>
        /// <returns></returns>
        [HttpGet("{token:int}")]
        public IActionResult GetByToken([FromRoute]int token)
        {
            var searchText = Tracker.Searches.Values.SingleOrDefault(s => s.Token == token)?.SearchText;

            if (string.IsNullOrEmpty(searchText))
            {
                return NotFound();
            }

            return GetBySearchText(searchText);
        }
    }
}
