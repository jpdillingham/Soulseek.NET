namespace WebAPI.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Soulseek;

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

        public FilesController(IConfiguration configuration, ISoulseekClient client)
        {
            OutputDirectory = configuration.GetValue<string>("OUTPUT_DIR");
            Client = client;
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
            var fileBytes = await Client.DownloadAsync(username, filename, token);

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

        [HttpPost("queue/{username}/{filename}")]
        public async Task<IActionResult> Enqueue([FromRoute, Required]string username, [FromRoute, Required]string filename, [FromQuery]int? token)
        {
            var fileBytes = await Client.DownloadAsync(username, filename, token);
            var localFilename = SaveLocalFile(filename, OutputDirectory, fileBytes);

            return Ok(localFilename);
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
