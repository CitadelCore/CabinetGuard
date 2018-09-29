using CabinetServer.Data;
using CorePlatform.Models.CabinetModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CabinetServer.Services
{
    public class DatabaseUpdateService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<DatabaseUpdateService> _logger;

        private static TimeSpan Timeout = TimeSpan.FromMinutes(10);
        private static TimeSpan Deletion = TimeSpan.FromHours(2);
        public DatabaseUpdateService(IServiceProvider serviceProvider, ILogger<DatabaseUpdateService> logger)
        {
            _provider = serviceProvider;
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"The Database Updater is starting.");
            stoppingToken.Register(() =>
            {
                _logger.LogDebug($"The Database Updater is stopping.");
            });

            using (IServiceScope scope = _provider.CreateScope())
            {
                ApplicationDbContext _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Ensure all controllers are disconnected
                List<CabinetController> controllers = _context.Controllers.Where(c => c.HubConnectionId != null).ToList();
                controllers.ForEach(controller =>
                {
                    _logger.LogDebug("Controller " + controller.Id.ToString() + " was still marked as connected! Fixing this.");
                    controller.HubConnectionId = null;
                });

                _context.UpdateRange(controllers);
                await _context.SaveChangesAsync();
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                using (IServiceScope scope = _provider.CreateScope())
                {
                    ApplicationDbContext _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Expire commands that have timed out
                    _logger.LogDebug($"Checking for database entries that should be expired.");

                    List<ScheduledCommand> expiredCommands = _context.ScheduledCommands.Where(c => c.Result != CommandResult.Expired && (c.State == CommandState.InProgress || c.State == CommandState.Scheduled)).Where(c => DateTime.Now > (c.TimeCreated + Timeout)).ToList();
                    expiredCommands.ForEach(command =>
                    {
                        _logger.LogDebug(String.Format("Command {0} scheduled by user {1} on controller {2} has passed its expiry time. It will be expired.", command.Id, command.UserId, command.ControllerId));
                        command.State = CommandState.Complete;
                        command.Result = CommandResult.Expired;
                    });

                    _context.UpdateRange(expiredCommands);

                    // Remove commands expired for a length of time
                    _logger.LogDebug($"Checking for database entries that should be deleted.");

                    List<ScheduledCommand> deletedCommands = _context.ScheduledCommands.Where(c => c.State == CommandState.Complete).Where(c => DateTime.Now > (c.TimeCreated + Deletion)).ToList();
                    expiredCommands.ForEach(command =>
                    {
                        _logger.LogDebug(String.Format("Command {0} scheduled by user {1} on controller {2} has passed its deletion time. It will be removed.", command.Id, command.UserId, command.ControllerId));
                    });

                    _context.RemoveRange(deletedCommands);

                    await _context.SaveChangesAsync();

                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        // Cancellation requested
                    }
                    
                }
            }

            _logger.LogDebug($"The Database Updater is stopping.");
        }

        /**
        protected override async Task StopAsync(CancellationToken stoppingToken)
        {

        }*/
    }
}
