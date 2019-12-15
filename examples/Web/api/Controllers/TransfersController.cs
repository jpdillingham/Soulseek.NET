namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Soulseek;
    using Soulseek.Exceptions;
    using System;
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

        /// <summary>
        ///     Cancels the specified download.
        /// </summary>
        /// <param name="username">The username of the download source.</param>
        /// <param name="filename">The download filename.</param>
        /// <param name="remove">A value indicating whether the tracked download should be removed after cancellation.</param>
        /// <returns></returns>
        /// <response code="204">The download was cancelled successfully.</response>
        /// <response code="404">The specified download was not found.</response>
        [HttpDelete("downloads/{username}/{filename}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult CancelDownload([FromRoute, Required] string username, [FromRoute, Required]string filename, [FromQuery]bool remove = false)
        {
            return CancelTransfer(TransferDirection.Download, username, filename, remove);
        }

        /// <summary>
        ///     Cancels the specified upload.
        /// </summary>
        /// <param name="username">The username of the upload destination.</param>
        /// <param name="filename">The upload filename.</param>
        /// <param name="remove">A value indicating whether the tracked upload should be removed after cancellation.</param>
        /// <returns></returns>
        /// <response code="204">The upload was cancelled successfully.</response>
        /// <response code="404">The specified upload was not found.</response>
        [HttpDelete("uploads/{username}/{filename}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult CancelUpload([FromRoute, Required] string username, [FromRoute, Required]string filename, [FromQuery]bool remove = false)
        {
            return CancelTransfer(TransferDirection.Upload, username, filename, remove);
        }

        /// <summary>
        ///     Enqueues the specified download.
        /// </summary>
        /// <param name="username">The username of the download source.</param>
        /// <param name="filename">The download filename.</param>
        /// <param name="token">The optional unique download token.</param>
        /// <returns></returns>
        /// <response code="201">The download was successfully enqueued.</response>
        /// <response code="403">The download was rejected.</response>
        /// <response code="500">An unexpected error was encountered.</response>
        [HttpPost("downloads/{username}/{filename}")]
        [ProducesResponseType(201)]
        [ProducesResponseType(typeof(string), 403)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> Enqueue([FromRoute, Required]string username, [FromRoute, Required]string filename, [FromQuery]int? token)
        {
            var waitUntilEnqueue = new TaskCompletionSource<bool>();
            var stream = GetLocalFileStream(filename, OutputDirectory);

            using (var cts = new CancellationTokenSource())
            {
                var downloadTask = Client.DownloadAsync(username, filename, stream, token, new TransferOptions(disposeOutputStreamOnCompletion: true, stateChanged: (e) =>
                {
                    Tracker.AddOrUpdate(e, cts);

                    if (e.Transfer.State == TransferStates.Queued)
                    {
                        waitUntilEnqueue.SetResult(true);
                    }
                }, progressUpdated: (e) => Tracker.AddOrUpdate(e, cts)), cts.Token);

                try
                {
                    // wait until either the waitUntilEnqueue task completes because the download was successfully queued, or the
                    // downloadTask throws due to an error prior to successfully queueing.
                    var task = await Task.WhenAny(waitUntilEnqueue.Task, downloadTask);

                    if (task == downloadTask)
                    {
                        var rejected = downloadTask.Exception.InnerExceptions.Where(e => e is TransferRejectedException);

                        if (rejected.Any())
                        {
                            return StatusCode(403, rejected.First().Message);
                        }

                        return StatusCode(500, downloadTask.Exception.Message);
                    }

                    // if it didn't throw, just return ok. the download will continue waiting in the background.
                    return StatusCode(201);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex);
                }
            }
        }

        /// <summary>
        ///     Gets all downloads.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("downloads")]
        [ProducesResponseType(200)]
        public IActionResult GetDownloads()
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Download)
                .ToMap());
        }

        /// <summary>
        ///     Gets all downloads for the specified username.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("downloads/{username}")]
        [ProducesResponseType(200)]
        public IActionResult GetDownloads([FromRoute, Required]string username)
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Download)
                .FromUser(username)
                .ToMap());
        }

        /// <summary>
        ///     Gets the download for the specified username matching the specified filename.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("downloads/{username}/{filename}")]
        [ProducesResponseType(typeof(Transfer), 200)]
        public IActionResult GetDownloads([FromRoute, Required]string username, [FromRoute, Required]string filename)
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Download)
                .FromUser(username)
                .WithFilename(filename).Transfer);
        }

        /// <summary>
        ///     Gets the current place in the remote queue of the specified download.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("downloads/{username}/{filename}/position")]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<IActionResult> GetPlaceInQueue([FromRoute, Required]string username, [FromRoute, Required]string filename)
        {
            return Ok(await Client.GetDownloadPlaceInQueueAsync(username, filename));
        }

        /// <summary>
        ///     Gets all uploads.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("uploads")]
        [ProducesResponseType(200)]
        public IActionResult GetUploads()
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Upload)
                .ToMap());
        }

        /// <summary>
        ///     Gets all uploads for the specified username.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("uploads/{username}")]
        [ProducesResponseType(200)]
        public IActionResult GetUploads([FromRoute, Required]string username)
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Upload)
                .FromUser(username)
                .ToMap());
        }

        /// <summary>
        ///     Gets the upload for the specified username matching the specified filename.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        /// <response code="200">The request completed successfully.</response>
        [HttpGet("uploads/{username}/{filename}")]
        [ProducesResponseType(200)]
        public IActionResult GetUploads([FromRoute, Required]string username, [FromRoute, Required]string filename)
        {
            return Ok(Tracker.Transfers
                .WithDirection(TransferDirection.Upload)
                .FromUser(username)
                .WithFilename(filename).Transfer);
        }

        private static FileStream GetLocalFileStream(string remoteFilename, string saveDirectory)
       {
            var localFilename = remoteFilename.ToLocalOSPath();
            var path = $"{saveDirectory}{Path.DirectorySeparatorChar}{Path.GetDirectoryName(localFilename).Replace(Path.GetDirectoryName(Path.GetDirectoryName(localFilename)), "")}";

            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            localFilename = Path.Combine(path, Path.GetFileName(localFilename));

            return new FileStream(localFilename, FileMode.Create);
        }

        private IActionResult CancelTransfer(TransferDirection direction, string username, string filename, bool remove = false)
        {
            if (Tracker.TryGet(direction, username, filename, out var transfer))
            {
                transfer.CancellationTokenSource.Cancel();

                if (remove)
                {
                    Tracker.TryRemove(direction, username, filename);
                }

                return NoContent();
            }

            return NotFound();
        }
    }
}