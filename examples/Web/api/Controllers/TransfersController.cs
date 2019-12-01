namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Soulseek;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;

    /// <summary>
    ///     Transfers
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class TransfersController : ControllerBase
    {
        private ISoulseekClient Client { get; }
        private ITransferTracker Tracker { get; }

        public TransfersController(ISoulseekClient client, ITransferTracker tracker)
        {
            Client = client;
            Tracker = tracker;
        }

        [HttpGet("download")]
        public IActionResult GetDownloads()
        {
            var x = Tracker.Downloads.MapOutput();

            return Ok(x);
        }

        [HttpGet("upload")]
        public IActionResult GetUploads()
        {
            var x = Tracker.Uploads.MapOutput();

            return Ok(x);
        }
    }

    public static class TransfersExtensions 
    {
        public static object MapOutput(this ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>> dict)
        {
            return dict.Select(u => new
            {
                Username = u.Key,
                Directories = u.Value.Values
                    .GroupBy(f => Path.GetDirectoryName(f.Filename))
                    .Select(d => new { Directory = d.Key, Files = d })
            });
        }
    }
}
