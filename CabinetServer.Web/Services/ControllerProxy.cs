using CabinetServer.Web.Command;
using CabinetServer.Web.Data;
using CorePlatform.Models.CabinetModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CabinetServer.Services
{
    /// <summary>
    /// Class which proxies requests to
    /// the cabinet management controller (CMC).
    /// </summary>
    public class ControllerProxy
    {
        private static TimeSpan timeout = TimeSpan.FromMinutes(1);
        public CabinetController controller { get; private set; }
        public Cabinet cabinet { get; private set; }

        private ApplicationDbContext _context;
        private IServiceProvider _provider;
        private CommandHub _hub;

        public string CurrentUserId;

        public ControllerProxy(CabinetController controller, IServiceProvider provider)
        {
            this.controller = controller;
            _provider = provider;
            _context = (ApplicationDbContext) provider.GetService(typeof(ApplicationDbContext));
            _hub = (CommandHub) provider.GetService(typeof(CommandHub));

            cabinet = _context.Cabinets.Select(s => s).SingleOrDefault(p => (p.ControllerId == controller.Id));

            _hub.CommandStatusUpdated += Hub_CommandStatusUpdated;
        }

        private async Task Hub_CommandStatusUpdated(ScheduledCommand command, ApplicationDbContext context)
        {
            await SetScheduledCommands(command);
        }

        /// <summary>
        /// Queues a new command with the cabinet management controller (CMC).
        /// </summary>
        /// <param name="command">The command to send to the controller.</param>
        /// <param name="data">Additional data to send with the command.</param>
        public async Task<ScheduledCommand> SendCommandToController(string command, IDictionary<string, dynamic> data = null)
        {
            ScheduledCommand scheduledCommand = new ScheduledCommand()
            {
                ControllerId = controller.Id,
                Name = command,
                UserId = CurrentUserId,
                Payload = data,
                State = CommandState.Scheduled,
                Result = CommandResult.None,
                TimeCreated = DateTime.Now,
            };

            await _context.ScheduledCommands.AddAsync(scheduledCommand);
            await _context.SaveChangesAsync();

            await PushCommandToController(controller, scheduledCommand);

            return scheduledCommand;
        }

        /// <summary>
        /// Pushes a single command to the controller from the server side.
        /// Always called when a command is being executed on a live connection.
        /// </summary>
        private async Task PushCommandToController(CabinetController controller, ScheduledCommand command)
        {
            // Don't send if the controller is offline!
            if (!ValidateController(controller))
                throw new Exception("The controller is not connected to the hub!");

            if (command.State != CommandState.Scheduled)
                throw new Exception("Command is in an incorrect state. It may be invalid or have completed already.");

            await _hub.Clients.Client(controller.HubConnectionId)?.SendCoreAsync("RecieveCommand", new[] { command });
        }

        /// <summary>
        /// Validates the controller.
        /// </summary>
        public bool ValidateController(CabinetController controller)
        {
            // Ensure we have a valid Controller
            if (controller == null || String.IsNullOrEmpty(controller.HubConnectionId) || _hub.Clients == null || _hub.Clients.Client(controller.HubConnectionId) == null)
                return false;

            return true;
        }

        /// <summary>
        /// Retrieves scheduled commands for the current controller from the database.
        /// </summary>
        /// <returns>IEnumerable of the scheduled commands.</returns>
        public IEnumerable<ScheduledCommand> GetScheduledCommands()
        {
            return _context.ScheduledCommands.Select(s => s).Where(p => (p.ControllerId == controller.Id)).AsEnumerable();
        }

        /// <summary>
        /// Updates a command in the database with new
        /// State and Result enum values.
        /// </summary>
        /// <param name="commands">ScheduledCommand to update. GUIDs must match.</param>
        public async Task SetScheduledCommands(ScheduledCommand command)
        {
            using (IServiceScope scope = _provider.CreateScope())
            {
                ApplicationDbContext _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                ScheduledCommand dbCommand = await _context.ScheduledCommands.Select(s => s).Where(p => (p.Id == command.Id)).FirstAsync();

                dbCommand.State = command.State;
                dbCommand.Result = command.Result;
                _context.ScheduledCommands.Update(dbCommand);
                await _context.SaveChangesAsync();
            } 
        }
    }
}
