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

        private ISoulseekClient Client { get; }
        private string OutputDirectory { get; }
        private ITransferTracker Tracker { get; }

        [HttpDelete("downloads/{username}/{filename}")]
        public IActionResult CancelDownload([FromRoute, Required] string username, [FromRoute, Required]string filename)
        {
            CancelTransfer(TransferDirection.Download, username, filename);
            return NoContent();
        }

        [HttpDelete("uploads/{username}/{filename}")]
        public IActionResult CancelUpload([FromRoute, Required] string username, [FromRoute, Required]string filename)
        {
            CancelTransfer(TransferDirection.Upload, username, filename);
            return NoContent();
        }

        private async void CancelTransfer(TransferDirection direction, string username, string filename)
        {
            Tracker.Transfers.TryGetValue(direction, out var transfers);
            transfers.TryGetValue(username, out var user);
            user.TryGetValue(filename, out var transfer);

            transfer.CancellationTokenSource.Cancel();
        }

        [HttpPost("downloads/{username}/{filename}")]
        public async Task<IActionResult> Enqueue([FromRoute, Required]string username, [FromRoute, Required]string filename, [FromQuery]int? token)
        {
            var waitUntilEnqueue = new TaskCompletionSource<bool>();
            var stream = GetLocalFileStream(filename, OutputDirectory);

            var cts = new CancellationTokenSource();

            var downloadTask = Client.DownloadAsync(username, filename, stream, token, new TransferOptions(disposeOutputStreamOnCompletion: true, stateChanged: (e) =>
            {
                Tracker.AddOrUpdate(e, cts);

                if (e.Transfer.State == TransferStates.Queued)
                {
                    waitUntilEnqueue.SetResult(true);
                }
            }, progressUpdated: (e) => Tracker.AddOrUpdate(e, cts)));

            try
            {
                // wait until either the waitUntilEnqueue task completes because the download was successfully queued, or the
                // downloadTask throws due to an error prior to successfully queueing.
                var task = await Task.WhenAny(waitUntilEnqueue.Task, downloadTask);

                if (task == downloadTask)
                {
                    return StatusCode(500, downloadTask.Exception.Message);
                }

                // if it didn't throw, just return ok. the download will continue waiting in the background.
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpGet("downloads")]
        public IActionResult GetDownloads()
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Download)
                .ToMap());
        }

        [HttpGet("downloads/{username}")]
        public IActionResult GetDownloads([FromRoute, Required]string username)
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Download)
                .FromUser(username)
                .ToMap());
        }

        [HttpGet("downloads/{username}/{filename}")]
        public IActionResult GetDownloads([FromRoute, Required]string username, [FromRoute, Required]string filename)
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Download)
                .FromUser(username)
                .WithFilename(filename).Transfer);
        }

        [HttpGet("downloads/{username}/{filename}/position")]
        public async Task<IActionResult> GetPlaceInQueue([FromRoute, Required]string username, [FromRoute, Required]string filename)
        {
            return Ok(await Client.GetDownloadPlaceInQueueAsync(username, filename));
        }

        private (Transfer, CancellationTokenSource) GetTransfer(TransferDirection direction, string username, string filename)
        {
            Tracker.Transfers.TryGetValue(direction, out var transfers);

            if (transfers == default) return default;

            transfers.TryGetValue(username, out var user);

            if (user == default) return default;

            user.TryGetValue(filename, out var transfer);
            return transfer;
        }

        private static FileStream GetLocalFileStream(string remoteFilename, string saveDirectory)
        {
            // GetDirectoryName() and GetFileName() only work when the path separator is the same as the current OS'
            // DirectorySeparatorChar. normalize for both Windows and Linux by replacing / and \ with Path.DirectorySeparatorChar.
            var localFilename = remoteFilename.ToLocalOSPath();

            var path = $"{saveDirectory}{Path.DirectorySeparatorChar}{Path.GetDirectoryName(localFilename).Replace(Path.GetDirectoryName(Path.GetDirectoryName(localFilename)), "")}";

            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            localFilename = Path.Combine(path, Path.GetFileName(localFilename));

            return new FileStream(localFilename, FileMode.Create);
        }

        private object GetTransfers(TransferDirection direction)
        {
            Tracker.Transfers.TryGetValue(direction, out var transfers);

            return transfers?.Select(u => new
            {
                Username = u.Key,
                Directories = u.Value.Values
                     .GroupBy(f => Path.GetDirectoryName(f.Transfer.Filename))
                     .Select(d => new { Directory = d.Key, Files = d })
            });
        }
    }
}