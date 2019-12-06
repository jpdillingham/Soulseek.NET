namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Soulseek;
    using System;
    using System.Collections.Concurrent;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
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
        private string OutputDirectory { get; }
        private ITransferTracker Tracker { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransfersController"/> class.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="client"></param>
        /// <param name="tracker"></param>
        public TransfersController(IConfiguration configuration, ISoulseekClient client, ITransferTracker tracker)
        {
            OutputDirectory = configuration.GetValue<string>("OUTPUT_DIR");
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

        [HttpDelete("{username}/{filename}")]
        public async Task Cancel([FromRoute, Required] string username, [FromRoute, Required]string filename)
        {

        }

        [HttpGet("{username}/{filename}/position")]
        public async Task<IActionResult> GetPlaceInQueue([FromRoute, Required]string username, [FromRoute, Required]string filename)
        {
            return Ok(await Client.GetDownloadPlaceInQueueAsync(username, filename));
        }

        [HttpPost("{username}/{filename}")]
        public async Task<IActionResult> Enqueue([FromRoute, Required]string username, [FromRoute, Required]string filename, [FromQuery]int? token)
        {
            var waitUntilEnqueue = new TaskCompletionSource<bool>();

            var stream = GetLocalFileStream(filename, OutputDirectory);

            var downloadTask = Client.DownloadAsync(username, filename, stream, token, new TransferOptions(disposeOutputStreamOnCompletion: true, stateChanged: (e) =>
            {
                Tracker.AddOrUpdate(e);

                if (e.Transfer.State == TransferStates.Queued)
                {
                    waitUntilEnqueue.SetResult(true);
                }
            }, progressUpdated: (e) => Tracker.AddOrUpdate(e)));

            try
            {
                // wait until either the waitUntilEnqueue task completes because the download was successfully
                // queued, or the downloadTask throws due to an error prior to successfully queueing.
                var task = await Task.WhenAny(waitUntilEnqueue.Task, downloadTask);

                if (task == downloadTask)
                {
                    return StatusCode(500, downloadTask.Exception.Message);
                }

                // if it didn't throw, just return ok.  the download will continue waiting in the background.
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        private static FileStream GetLocalFileStream(string remoteFilename, string saveDirectory)
        {
            // GetDirectoryName() and GetFileName() only work when the path separator is the same as the current OS' DirectorySeparatorChar.
            // normalize for both Windows and Linux by replacing / and \ with Path.DirectorySeparatorChar.
            var localFilename = remoteFilename.ToLocalOSPath();

            var path = $"{saveDirectory}{Path.DirectorySeparatorChar}{Path.GetDirectoryName(localFilename).Replace(Path.GetDirectoryName(Path.GetDirectoryName(localFilename)), "")}";

            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            localFilename = Path.Combine(path, Path.GetFileName(localFilename));

            return new FileStream(localFilename, FileMode.Create);
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
