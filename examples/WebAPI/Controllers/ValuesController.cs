using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    ///     Values
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class ValuesController : ControllerBase
    {
        /// <summary>
        ///     Gets a list of values
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        ///     Gets a specific value
        /// </summary>
        /// <param name="index">The index to get</param>
        /// <param name="foo">foo</param>
        /// <returns></returns>
        [HttpGet("{index}")]
        public ActionResult<string> GetOne(int index = 1, string foo = null)
        {
            var arr = new string[] { "value1", "value2" };

            return arr[index];
        }
    }

    /// <summary>
    ///     Values
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("2")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class ValuesController2 : ControllerBase
    {
        /// <summary>
        ///     Gets a list of values
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value3", "value4" };
        }

        /// <summary>
        ///     Gets a specific value
        /// </summary>
        /// <returns></returns>
        [HttpGet("{index}")]
        public ActionResult<string> GetOne(int index)
        {
            var arr = new string[] { "value3", "value4" };

            return arr[index];
        }
    }
}
