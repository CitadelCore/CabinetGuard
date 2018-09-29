using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using CabinetServer.Web.Data;
using CorePlatform.Models.CabinetModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CabinetServer.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class EnrollmentController : Controller
    {
        private ApplicationDbContext DbContext;
        public EnrollmentController(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        /// <summary>
        /// Gets the controller enrollment status from a controller GUID.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("~/api/[controller]/Get/{id}")]
        public IActionResult Get(Guid? id)
        {
            CabinetController controller = DbContext.Controllers.SingleOrDefault(c => c.Id == id);

            if (controller == null)
                return new NotFoundResult();

            return new OkObjectResult(controller.StripUselessInfo());
        }

        /// <summary>
        /// Attempts enrollment of the controller with the specified hostname.
        /// This will fail if the controller has already been authorized.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("~/api/[controller]/Enroll")]
        public IActionResult Enroll()
        {
            string hostname = Dns.GetHostEntry(HttpContext.Connection.RemoteIpAddress.ToString()).HostName;

            // Get the controller from the database.
            CabinetController controller = DbContext.Controllers.SingleOrDefault(c => c.Hostname.ToLower() == hostname.ToLower());
            if (controller == null || controller.Authorized == true)
                return new UnauthorizedResult();

            Guid revocationGuid = Guid.NewGuid();

            // Give the client a JWT security token
            Claim[] claims = new Claim[]
            {
                new Claim("RevocationGuid", revocationGuid.ToString()),
                new Claim("ControllerGuid", controller.Id.ToString()),
                new Claim("AuthorizationType", "ControllerAuthOnly"),
                new Claim(ClaimTypes.Dns, hostname),
            };

            SigningCredentials credentials = new SigningCredentials(new X509SecurityKey(Startup.GetMachineCertificate()), SecurityAlgorithms.RsaSha256);
            JwtSecurityToken token = new JwtSecurityToken(
                issuer: "tower.local",
                audience: "tower.local",
                claims: claims,
                expires: DateTime.Now.AddMonths(2),
                signingCredentials: credentials);

            // Controller is enrolled!
            controller.Authorized = true;
            controller.RevocationGuid = revocationGuid;
            DbContext.Controllers.Update(controller);
            DbContext.SaveChanges();

            return new OkObjectResult(new { token = new JwtSecurityTokenHandler().WriteToken(token), guid = controller.Id.ToString() });
        }
    }
}