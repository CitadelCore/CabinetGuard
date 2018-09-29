using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CabinetServer.Web.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CabinetServer.Web.Models;
using Microsoft.AspNetCore.Identity;
using CorePlatform.Models.CabinetModels;
using CabinetServer.Services;
using Newtonsoft.Json;
using CabinetServer.Web.Command;

namespace CabinetServer.Web.Controllers.Control
{
    [Authorize]
    public class ControllersController : BaseController
    {
        public ControllersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context, userManager) { }

        // GET: CabinetControllers
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(HttpContext.User);
            IList<Tuple<CabinetController, Cabinet>> finalPairs = new List<Tuple<CabinetController, Cabinet>>();

            foreach (CabinetController controller in _context.Controllers.Where(o => o.UserId == userId))
            {
                Cabinet cabinet = await _context.Cabinets.Where(o => o.UserId == userId).SingleOrDefaultAsync(m => m.ControllerId == controller.Id);
                controller.PrettifyStyles();
                finalPairs.Add(new Tuple<CabinetController, Cabinet>(controller, cabinet));
            }

            return View(finalPairs);
        }

        // GET: CabinetControllers/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            if (id == null)
            {
                return NotFound();
            }

            CabinetController cabinetController = await _context.Controllers.Where(o => o.UserId == userId)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (cabinetController == null)
                return NotFound();

            Cabinet cabinet = await _context.Cabinets.Where(o => o.UserId == userId).SingleOrDefaultAsync(m => m.ControllerId == cabinetController.Id);
            if (cabinet == null)
                return NotFound();

            return View(new Tuple<CabinetController, Cabinet>(cabinetController, cabinet));
        }

        // GET: CabinetControllers/Create
        public IActionResult Create()
        {
            return View();
        }

        public IActionResult RunCommand(Guid? id)
        {
            if (id == null)
                return NotFound();

            return View();
        }

        // POST: CabinetControllers/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nickname", "Hostname")] CabinetController cabinetController)
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            if (ModelState.IsValid)
            {
                cabinetController.Id = Guid.NewGuid();
                cabinetController.UserId = userId;

                Cabinet cabinet = new Cabinet()
                {
                    Id = Guid.NewGuid(),
                    ControllerId = cabinetController.Id,
                    UserId = userId,
                };

                _context.Add(cabinetController);
                _context.Add(cabinet);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cabinetController);
        }

        // GET: CabinetControllers/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            if (id == null)
            {
                return NotFound();
            }

            var cabinetController = await _context.Controllers.Where(o => o.UserId == userId).SingleOrDefaultAsync(c => c.Id == id);
            if (cabinetController == null)
            {
                return NotFound();
            }
            return View(cabinetController);
        }

        // POST: CabinetControllers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Nickname", "Hostname")] CabinetController cabinetController)
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            if (ModelState.IsValid)
            {
                try
                {
                    cabinetController.Id = id;
                    cabinetController.UserId = userId;
                    _context.Update(cabinetController);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CabinetControllerExists(cabinetController.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(cabinetController);
        }

        // GET: CabinetControllers/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            if (id == null)
            {
                return NotFound();
            }

            var cabinetController = await _context.Controllers.Where(o => o.UserId == userId)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (cabinetController == null)
            {
                return NotFound();
            }

            return View(cabinetController);
        }

        // POST: CabinetControllers/Delete/5
        // TODO: Fix this so it'll delete the controller regardless of whether it's connected or not
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            CabinetController cabinetController = await GetController(id);
            Cabinet cabinet = await GetCabinet(cabinetController);
            if (cabinet != null)
                _context.Cabinets.Remove(cabinet);

            ControllerProxy proxy = GetControllerProxy(cabinetController);

            // If the controller isn't connected, force un-enroll it
            if (proxy.ValidateController(cabinetController))
                // Notify the controller that it should delete its JWT token and reset
                await proxy.SendCommandToController("clientDelete");

            // Wait a second or two for completion
            await Task.Delay(TimeSpan.FromSeconds(2));

            _context.Controllers.Remove(cabinetController);
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /**
        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> VerifyController(Guid? id)
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            CabinetController cabinetController = await _context.Controllers.Where(o => o.UserId == userId)
                .SingleOrDefaultAsync(m => m.Id == id);
            if (cabinetController == null)
                return Json($"The controller was not found or you do not have permission to access it.");

            ControllerProxy proxy = new ControllerProxy(cabinetController, HttpContext.RequestServices, _hub);
            if (proxy.ValidateController(cabinetController))
            {
                return Json(true);
            }

            return Json($"The controller is offline or unresponsive.");
        }*/

        private bool CabinetControllerExists(Guid id)
        {
            string userId = _userManager.GetUserId(HttpContext.User);

            return _context.Controllers.Where(o => o.UserId == userId).Any(e => e.Id == id);
        }
    }
}
