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
    using WebAPI.Trackers;

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

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchesController"/> class.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="tracker"></param>
        public SearchesController(ISoulseekClient client, ISearchTracker tracker)
        {
            Client = client;
            Tracker = tracker;
        }

        /// <summary>
        ///     Performs a search for the specified <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The search request.</param>
        /// <returns></returns>
        /// <response code="200">The search completed successfully.</response>
        /// <response code="400">The specified <paramref name="request"/> was malformed.</response>
        /// <response code="500">The search terminated abnormally.</response>
        [HttpPost("")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<SearchResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> Post([FromBody]SearchRequest request)
        {
            var options = request.ToSearchOptions(
                responseReceived: (e) => Tracker.AddOrUpdate(e),
                stateChanged: (e) => Tracker.AddOrUpdate(e));

            var results = new ConcurrentBag<SearchResponse>();

            var searchText = string.Join(' ', request.SearchText.Split(' ').Where(term => term.Length > 1));

            try
            {
                await Client.SearchAsync(SearchQuery.FromText(searchText), (r) => results.Add(r), SearchScope.Default, request.Token, options);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Search terminated abnormally: {ex.Message}");
            }
            finally
            {
                results = null;
                Tracker.TryRemove(request.SearchText);
            }
        }

        /// <summary>
        ///     Gets the state of all current searches.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<Search>), 200)]
        public IActionResult Get()
        {
            return Ok(Tracker.Searches);
        }

        /// <summary>
        ///     Gets the state of the search corresponding to the specified <paramref name="searchText"/>.
        /// </summary>
        /// <param name="searchText">The search phrase of the desired search.</param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        /// <response code="404">A matching search was not found.</response>
        [HttpGet("{searchText}")]
        [Authorize]
        [ProducesResponseType(typeof(Search), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetBySearchText([FromRoute]string searchText)
        {
            Tracker.Searches.TryGetValue(searchText, out var search);

            if (search == default)
            {
                return NotFound();
            }

            return Ok(search);
        }

        /// <summary>
        ///     Gets the state of the search corresponding to the specified <paramref name="token"/>.
        /// </summary>
        /// <param name="token">The token of the desired search.</param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        /// <response code="404">A matching search was not found.</response>
        [HttpGet("{token:int}")]
        [Authorize]
        [ProducesResponseType(typeof(Search), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetByToken([FromRoute]int token)
        {
            var search = Tracker.Searches.Values.SingleOrDefault(s => s.Token == token);

            if (search == default)
            {
                return NotFound();
            }

            return Ok(search);
        }
    }
}
