using CabinetServer.Services;
using CabinetServer.Web.Command;
using CabinetServer.Web.Data;
using CabinetServer.Web.Models;
using CorePlatform.Models.CabinetModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CabinetServer.Web.Controllers.Control
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;
        public BaseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public void RenderAlert(AlertType type, string text = null)
        {
            string _type = type.ToString().ToLower();

            ViewBag.HasAlert = true;
            ViewBag.AlertType = _type;
            ViewBag.AlertMessage = text;
        }

        public enum AlertType
        {
            Success,
            Info,
            Warning,
            Danger
        }

        /// <summary>
        /// Gets a controller proxy for the specified controller.
        /// </summary>
        protected ControllerProxy GetControllerProxy(CabinetController controller)
        {
            return new ControllerProxy(controller, HttpContext.RequestServices)
            {
                CurrentUserId = _userManager.GetUserId(HttpContext.User),
            };
        }

        /// <summary>
        /// Gets a cabinet controller from the specified ID.
        /// </summary>
        protected async Task<CabinetController> GetController(Guid id)
        {
            return await _context.Controllers.Where(o => o.UserId == _userManager.GetUserId(HttpContext.User)).SingleOrDefaultAsync(m => m.Id == id);
        }

        protected async Task<Cabinet> GetCabinet(CabinetController controller)
        {
            return await _context.Cabinets.Where(o => o.UserId == _userManager.GetUserId(HttpContext.User)).SingleOrDefaultAsync(m => m.ControllerId == controller.Id);
        }

        protected async Task<Cabinet> GetCabinet(Guid id)
        {
            return await _context.Cabinets.Where(o => o.UserId == _userManager.GetUserId(HttpContext.User)).SingleOrDefaultAsync(m => m.Id == id);
        }

        protected async Task<ScheduledCommand> GetCommand(Guid id)
        {
            return await _context.ScheduledCommands.Where(o => o.UserId == _userManager.GetUserId(HttpContext.User)).SingleOrDefaultAsync(m => m.Id == id);
        }

        protected IActionResult GetCommandError(ScheduledCommand command, string error)
        {
            command.Error = error;
            return Json(command);
        }
    }
}
