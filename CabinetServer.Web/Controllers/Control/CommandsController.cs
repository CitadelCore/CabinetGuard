using CabinetServer.Web.Data;
using CabinetServer.Web.Models;
using CorePlatform.Models.CabinetModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CabinetServer.Web.Controllers.Control
{
    [Authorize]
    public class CommandsController : BaseController
    {
        public CommandsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context, userManager) { }

        // GET: Commands
        public IActionResult Index()
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            List<ScheduledCommand> commands = _context.ScheduledCommands.Where(o => o.UserId == userId).ToList();
            commands.ForEach(command => command.PrettifyEnums());

            return View(commands);
        }
    }
}
