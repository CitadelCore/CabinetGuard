using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CabinetServer.Web.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize(Policy = "ControllerValid", AuthenticationSchemes = "Bearer")]
    public class TestController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return new OkResult();
        }
    }
}