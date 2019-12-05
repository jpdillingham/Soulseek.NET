namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Soulseek;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WebAPI.Trackers;

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
            Tracker.Transfers.TryGetValue(TransferDirection.Download, out var downloads);
            var x = downloads.MapOutput();

            return Ok(x);
        }

        [HttpGet("upload")]
        public IActionResult GetUploads()
        {
            Tracker.Transfers.TryGetValue(TransferDirection.Upload, out var uploads);
            var x = uploads.MapOutput();

            return Ok(x);
        }
    }

    public static class TransfersExtensions
    {
        public static object MapOutput(this ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationToken CancellationToken)>> dict)
        {
            return dict.Select(u => new
            {
                Username = u.Key,
                Directories = u.Value.Values
                    .GroupBy(f => Path.GetDirectoryName(f.Transfer.Filename))
                    .Select(d => new { Directory = d.Key, Files = d })
            });
        }
    }
}
