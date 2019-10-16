namespace WebAPI.Controllers
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Soulseek;
    using Soulseek.Options;

    /// <summary>
    ///     Search
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class FilesController : ControllerBase
    {
        private ISoulseekClient Client { get; }
        private string OutputDirectory { get; }
        private IDownloadTracker Tracker { get; }

        public FilesController(IConfiguration configuration, ISoulseekClient client, IDownloadTracker tracker)
        {
            OutputDirectory = configuration.GetValue<string>("OUTPUT_DIR");
            Client = client;
            Tracker = tracker;
        }

        [HttpGet("")]
        public IActionResult GetAll()
        {
            var x = Tracker.Downloads.Select(u => new
            {
                Username = u.Key,
                Directories = u.Value.Values
                    .GroupBy(f => Path.GetDirectoryName(f.Filename))
                    .Select(d => new { Directory = d.Key, Files = d })
            });

            return Ok(x);
        }

        /// <summary>
        ///     Retrieves the files shared by the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to download.</param>
        /// <param name="filename">The file to download.</param>
        /// <param name="token">The optional download token.</param>
        /// <param name="toDisk">A value indicating whether the downloaded data should be written to disk or returned in the request result.</param>
        /// <returns></returns>
        [HttpGet("{username}/{filename}")]
        public async Task<IActionResult> Download([FromRoute, Required]string username, [FromRoute, Required]string filename, [FromQuery]int? token, [FromQuery]bool toDisk = true)
        {
            var fileBytes = await Client.DownloadAsync(username, filename, token, 
                new TransferOptions(stateChanged: (e) => Tracker.AddOrUpdate(e), progressUpdated: (e) => Tracker.AddOrUpdate(e)));

            if (toDisk)
            {
                var localFilename = SaveLocalFile(filename, OutputDirectory, fileBytes);
                return Ok(localFilename);
            }
            else
            {
                return File(fileBytes, "application/octet-stream", Path.GetFileName(filename));
            }
        }

        [HttpGet("{username}/{filename}/placeInQueue")]
        public async Task<IActionResult> GetPlaceInQueue([FromRoute, Required]string username, [FromRoute, Required]string filename)
        {
            return Ok(await Client.GetDownloadPlaceInQueueAsync(username, filename));
        }

        [HttpPost("queue/{username}/{filename}")]
        public async Task<IActionResult> Enqueue([FromRoute, Required]string username, [FromRoute, Required]string filename, [FromQuery]int? token)
        {
            var waitUntilEnqueue = new TaskCompletionSource<bool>();

            var downloadTask = Client.DownloadAsync(username, filename, token, new TransferOptions(stateChanged: (e) =>
            {
                Tracker.AddOrUpdate(e);

                if (e.State == TransferStates.Queued)
                {
                    waitUntilEnqueue.SetResult(true);
                }

                if (e.State.HasFlag(TransferStates.Completed) && e.State.HasFlag(TransferStates.Succeeded))
                {
                    SaveLocalFile(filename, OutputDirectory, e.Data);
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

        private static string SaveLocalFile(string remoteFilename, string saveDirectory, byte[] data)
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

            System.IO.File.WriteAllBytes(localFilename, data);

            return localFilename;
        }
    }
}
