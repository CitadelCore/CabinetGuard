using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using System.Threading.Tasks;
using ManagementController.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using ManagementController.Services;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace ManagementController
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private ControllerSettings controllerSettings;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            Task.Run(RunController);
        }

        private async Task RunController()
        {
            controllerSettings = await ControllerSettings.ReadAsync();

            // Build the service collection
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder
            .AddDebug()
            .SetMinimumLevel(LogLevel.Debug));

            ILoggerFactory logger = serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            serviceCollection.AddSingleton(new DiscoveryClient(logger.CreateLogger(typeof(DiscoveryClient)), Guid.Parse(controllerSettings.Guid)));
            serviceCollection.AddSingleton(typeof(ControllerSettings), controllerSettings);
            serviceCollection.AddSingleton(new ServerClient(serviceCollection.BuildServiceProvider()));
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            FmcController alarmController = new FmcController(serviceProvider);
            alarmController.FmcReadyEvent += AlarmController_FmcReadyEvent;
            await alarmController.Initialize();
        }

        private Task AlarmController_FmcReadyEvent(FmcController alarmController)
        {
            //await alarmController.Arm();
            //await alarmController.Alarm();

            return Task.CompletedTask;
        }
    }
}
