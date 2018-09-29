using CabinetServer.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CabinetServer.Web.Command
{
    public class ControllerRevocationHandler : AuthorizationHandler<ControllerRevocationRequirement>
    {
        private IServiceProvider _serviceProvider;
        public ControllerRevocationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ControllerRevocationRequirement requirement)
        {
            // Ensure the token has valid claims
            if (!context.User.HasClaim(c => c.Type == "RevocationGuid") || 
                !context.User.HasClaim(c => c.Type == "ControllerGuid") ||
                !context.User.HasClaim(c => c.Type == "AuthorizationType" && c.Value == "ControllerAuthOnly"))
                return Task.CompletedTask;

            // Get values from the JWT claims
            string revocation = context.User.FindFirst(c => c.Type == "RevocationGuid").Value;
            string guid = context.User.FindFirst(c => c.Type == "ControllerGuid").Value;

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ApplicationDbContext _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check the claims against the controller in the database
                if (_context.Controllers.Any(c => c.RevocationGuid.ToString() == revocation && c.Id.ToString() == guid))
                    context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
