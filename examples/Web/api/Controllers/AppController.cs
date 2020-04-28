namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using WebAPI.DTO;

    /// <summary>
    ///     Application
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class AppController : ControllerBase
    {
        private IConfiguration Configuration { get; }

        public AppController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody]Login login)
        {
            if (login == default)
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest("Username and/or Password missing or invalid");
            }

            var un = Configuration.GetValue<string>("USERNAME");
            var pw = Configuration.GetValue<string>("PASSWORD");

            if (login.Username == un && login.Password == pw)
            {
                return Ok("jwt goes here");
            }

            return Unauthorized();
        }
    }
}
