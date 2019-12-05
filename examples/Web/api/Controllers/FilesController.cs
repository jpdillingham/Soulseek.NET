namespace WebAPI.Controllers
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Soulseek;
    using WebAPI.Trackers;

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
        private ITransferTracker Tracker { get; }

        public FilesController(IConfiguration configuration, ISoulseekClient client, ITransferTracker tracker)
        {
            OutputDirectory = configuration.GetValue<string>("OUTPUT_DIR");
            Client = client;
            Tracker = tracker;
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
}
