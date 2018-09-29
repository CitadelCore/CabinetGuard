using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using CabinetServer.Data;
using CorePlatform.Models.CabinetModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CabinetServer.Command
{
    /// <summary>
    /// This Hub handles command traffic to and from a controller.
    /// Actual processing is done in the ControllerProxy class.
    /// </summary>
    [Authorize(Policy = "ControllerValid", AuthenticationSchemes = "Bearer")]
    public class CommandHub : Hub
    {
        private IServiceProvider _serviceProvider;
        public event CommandRecievedDelegate CommandRecieved;
        public delegate Task CommandRecievedDelegate(string command, IDictionary<string, dynamic> data);

        public event CommandStatusUpdatedDelegate CommandStatusUpdated;
        public delegate Task CommandStatusUpdatedDelegate(ScheduledCommand command, ApplicationDbContext context);
        public CommandHub(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        #region Shared

        public Task UpdateCabinetStatus(Cabinet newCabinet)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ApplicationDbContext _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                CabinetController controller = GetSessionController(_context);
                Cabinet cabinet = _context.Cabinets.SingleOrDefault(c => c.ControllerId == controller.Id);

                cabinet.SecurityArmed = newCabinet.SecurityArmed;
                cabinet.SecurityAlerted = newCabinet.SecurityAlerted;
                cabinet.FireAlerted = newCabinet.FireAlerted;
                cabinet.Override = newCabinet.Override;

                _context.Update(cabinet);
                _context.SaveChanges();

                return Task.CompletedTask;
            }
        }
        public Task UpdateCommand(ScheduledCommand command)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ApplicationDbContext _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                CommandStatusUpdated?.Invoke(command, _context);
                return Task.CompletedTask;
            }
        }

        public override Task OnConnectedAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ApplicationDbContext _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Set the Connection ID of the controller in the database.
                CabinetController controller = GetSessionController(_context);
                controller.HubConnectionId = Context.ConnectionId;
                _context.Update(controller);
                _context.SaveChanges();
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ApplicationDbContext _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Unset the Connection ID of the controller in the database.
                CabinetController controller = GetSessionController(_context);
                controller.HubConnectionId = null;
                _context.Update(controller);
                _context.SaveChanges();
            }

            return base.OnDisconnectedAsync(exception);
        }
        #endregion
        #region Server

        private CabinetController GetSessionController(ApplicationDbContext context)
        {
            string guid = Context.User.FindFirst(c => c.Type == "ControllerGuid").Value;
            return context.Controllers.SingleOrDefault(c => c.Id.ToString() == guid);
        }
        #endregion
    }
}
