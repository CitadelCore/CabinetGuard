using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CabinetServer.Services;
using CabinetServer.Web.Command;
using CabinetServer.Web.Controllers.Control;
using CabinetServer.Web.Data;
using CabinetServer.Web.Models;
using CorePlatform.Models.CabinetModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CabinetServer.Web.Controllers.Api
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/Commands")]
    public class CommandsApiController : BaseController
    {
        public CommandsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context, userManager) { }

        [HttpPost("~/api/Commands/ScheduleFor/{id}")]
        public async Task<IActionResult> Post(Guid id, [FromBody] ScheduledCommand _scheduledCommand)
        {
            CabinetController cabinetController = await GetController(id);
            if (cabinetController == null)
                return GetCommandError(_scheduledCommand, ErrorStrings.ControllerNotFound);

            // Make sure the controller is online
            ControllerProxy proxy = GetControllerProxy(cabinetController);
            if (!proxy.ValidateController(cabinetController))
                return GetCommandError(_scheduledCommand, ErrorStrings.ControllerOffline);

            // Send the command to the proxy
            ScheduledCommand command = await proxy.SendCommandToController(_scheduledCommand.Name, _scheduledCommand.Payload);

            return new JsonResult(command);
        }

        [HttpGet("~/api/Commands/{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            ScheduledCommand command = await GetCommand(id);
            return new JsonResult(command);
        }
    }
}