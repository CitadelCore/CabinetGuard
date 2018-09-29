using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CabinetServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CorePlatform.Models.CabinetModels;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CabinetServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CabinetsController : BaseController
    {
        public CabinetsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context, userManager) {}

        // GET: api/values
        [HttpGet]
        public IEnumerable<Cabinet> Get() {
            string userId = _userManager.GetUserId(HttpContext.User);
            return _context.Cabinets.Where(o => o.UserId == userId).ToList();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public Cabinet Get(Guid id)
        {
            string userId = _userManager.GetUserId(HttpContext.User);
            return _context.Cabinets.Where(o => o.UserId == userId).FirstOrDefault(c => c.Id == id);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] Cabinet newCabinet)
        {

        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(Guid id, [FromBody] Cabinet cabinet)
        {

        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(Guid id)
        {

        }
    }
}
