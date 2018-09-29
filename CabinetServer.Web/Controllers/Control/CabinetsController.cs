using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CabinetServer.Web.Data;
using CabinetServer.Web.Models;
using CorePlatform.Models.CabinetModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CabinetServer.Web.Controllers.Control
{
    [Authorize]
    public class CabinetsController : BaseController
    {
        public CabinetsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context, userManager) { }

        // GET: Cabinets
        public IActionResult Index()
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            List<Cabinet> cabinets = _context.Cabinets.Where(o => o.UserId == userId).ToList();
            //cabinets.ForEach(command => command.PrettifyEnums());

            return View(cabinets);
        }

        public IActionResult ConnectOneview(Guid? id)
        {
            string userId = _userManager.GetUserId(HttpContext.User);
            Cabinet cabinet = _context.Cabinets.Where(o => o.UserId == userId).SingleOrDefault(m => m.Id == id);
            if (cabinet == null)
                return NotFound();

            return View(cabinet);
        }
    }
}