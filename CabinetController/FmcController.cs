using ManagementController.Models;
using CorePlatform.Models.CabinetModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagementController
{
    class FmcController
    {
        private SerialController serialController;
        private IDictionary<string, PendingCommand> pendingCommands = new ConcurrentDictionary<string, PendingCommand>();

        public event FmcReadyEventDelegate FmcReadyEvent;
        public delegate Task FmcReadyEventDelegate(FmcController alarmController);

        /// <summary>
        /// Whether the failsafe management controller (FMC) is ready for use.
        /// This is set to true on FMC reset.
        /// </summary>
        public bool FmcReady { get; private set; } = false;

        public bool FmcResponding { get; private set; } = false;

        /// <summary>
        /// Whether an alarm is currently in force.
        /// Does not reflect the exact state of the FMC.
        /// </summary>
        public bool Alarmed { get; private set; } = false;

        /// <summary>
        /// Whether the FMC is armed.
        /// Does not reflect the exact state of the FMC.
        /// </summary>
        public bool Armed { get; private set; } = false;

        /// <summary>
        /// Whether the FMC is in an override condition.
        /// This could be via keyswitch or remote.
        /// </summary>
        public bool Override { get; private set; } = false;

        private Timer syncTimer = new Timer(syncInterval);

        // DO NOT SYNC if a sync is already pending.
        private bool syncPending = false;

        /// <summary>
        /// Interval at which a synchronization request
        /// is sent to the failsafe management controller (FMC),
        /// and the master server.
        /// </summary>
        private static int syncInterval = 10000;

        private ControllerSettings controllerSettings;
        private ServerClient serverClient;
        private ILogger<FmcController> logger;

        private delegate Task<bool> RunnableCommand();

        /// <summary>
        /// Handles communication to and from
        /// the Alarm Failsafe Management Controller (FMC).
        /// </summary>
        public FmcController(IServiceProvider serviceProvider)
        {
            controllerSettings = serviceProvider.GetService<ControllerSettings>();
            serverClient = serviceProvider.GetService<ServerClient>();

            ILoggerFactory factory = serviceProvider.GetService<ILoggerFactory>();
            logger = factory.CreateLogger<FmcController>();

            serialController = new SerialController(serviceProvider);
            serialController.SerialInitialized += SerialController_SerialInitialized;
            serialController.SerialRecieved += SerialController_SerialRecieved;

            serverClient.ServerCommandRecieved += ServerClient_ServerCommandRecieved;

            syncTimer.Start();
            syncTimer.Elapsed += SyncTimer_Elapsed;
        }

        private async Task SyncCabinetWithServer()
        {
            Cabinet cabinet = new Cabinet
            {
                SecurityArmed = Armed,
                SecurityAlerted = Alarmed,
                FireAlerted = false,
                Override = Override,
            };

            await serverClient.UpdateCabinetStatus(cabinet);
        }

        private async Task ServerClient_ServerCommandRecieved(ScheduledCommand command)
        {
            switch (command.Name)
            {
                case "arm":
                    await RunRequestedCommand(async () => { return await Arm(); }, command);
                    break;
                case "disarm":
                    await RunRequestedCommand(async () => { return await Disarm(); }, command);
                    break;
                case "alarm":
                    await RunRequestedCommand(async () => { return await Alarm(); }, command);
                    break;
                case "silence":
                    await RunRequestedCommand(async () => { return await Silence(); }, command);
                    break;
            }
        }

        /// <summary>
        /// Runs a command recieved from the server,
        /// and reports back to the server with completion information.
        /// </summary>
        private async Task RunRequestedCommand(RunnableCommand del, ScheduledCommand command)
        {
            logger.LogDebug(String.Format("Recieved a request to run the command \"{0}\" from the server.", command.Name));

            try
            {
                bool result = await del();

                if (result)
                {
                    logger.LogDebug(String.Format("Command \"{0}\" completed successfully. Updating server status to Success.", command.Name));

                    command.State = CommandState.Complete;
                    command.Result = CommandResult.Success;
                    await serverClient.UpdateCommand(command);

                    await SyncCabinetWithServer();
                }
                else
                {
                    logger.LogDebug(String.Format("Command \"{0}\" did NOT complete successfully. Updating server status to Error.", command.Name));

                    command.State = CommandState.Complete;
                    command.Result = CommandResult.Error;
                    await serverClient.UpdateCommand(command);
                }
            }
            catch (Exception e)
            {
                logger.LogError("An exception occurred while running a command. The command state will be reported as errored.", e);

                command.State = CommandState.Complete;
                command.Result = CommandResult.Error;
                await serverClient.UpdateCommand(command);
            }
        }

        private async void SyncTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Sync the FMC
            if (serialController.GetSerialInitialized() && FmcReady && !syncPending)
                await ForceSync();

            // Sync the CCS
            await SyncCabinetWithServer();
        }

        /// <summary>
        /// Initialize communications with the controller.
        /// </summary>
        public async Task Initialize()
        {
            await Task.Run(async () => {
                try
                {
                    logger.LogInformation("Attempting first time serial initialization with the FMC.");
                    await serialController.InitializeSerialAsync();
                }
                catch (Exception e)
                {
                    logger.LogError("Could not initialize the serial interface on the first attempt. The operation will continue to be retried.", e);
                    await serialController.RecoverSerialAsync();
                }
            });
        }

        private async Task SerialController_SerialInitialized()
        {
            if (!FmcReady)
                logger.LogInformation("The FMC is not ready and will be reset for the initial start sequence.");
                await Reset();

            await Task.Run(serialController.StartListeningAsync);
        }

        /// <summary>
        /// Called when this CMC recieves a serial message.
        /// </summary>
        private Task SerialController_SerialRecieved(string text)
        {
            string[] split = text.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            IList<IDictionary<string, dynamic>> statusResults = new List<IDictionary<string, dynamic>>();
            IList<PendingCommand> commandResults = new List<PendingCommand>();

            foreach (string line in split)
            {
                // Handle malformed serial output by the Arduino
                string _line = line;

                if (line == "{,\"command\":\"syncStatus\"}")
                    continue;

                string[] firstSplit = _line.Split("{\"status\":");
                if (firstSplit.Count() == 2)
                {
                    _line = "{\"status\":" + firstSplit[1];
                }

                IDictionary<string, dynamic> result = null;

                try
                {
                    result = JsonConvert.DeserializeObject<IDictionary<string, dynamic>>(_line);
                }
                catch (Exception e)
                {
                    logger.LogError(String.Format("Could not parse JSON from line with content \"{0}\". Content was malformed from the Arduino. The line will be ignored.", _line), e);
                    continue;
                }
                
                if (result.ContainsKey("status") && result.ContainsKey("command"))
                {
                    // Command result
                    IEnumerable<PendingCommand> pending = pendingCommands.Select(i => i.Value).Where(p => p.Command == result["command"]).OrderBy(p => p.TimeSent);

                    if (pending.Count() > 0)
                    {
                        PendingCommand command = pending.First();
                        if (command != null)
                        {
                            commandResults.Add(command);

                            pendingCommands[command.Guid].Confirmed = true;
                            pendingCommands[command.Guid].Status = result["status"];
                        }
                    }
                }
                else if (result.ContainsKey("status"))
                {
                    statusResults.Add(result);

                    switch (result["status"])
                    {
                        case "system_ready":
                            FmcReady = true;
                            Task.Run(async () => { await FmcReadyEvent.Invoke(this); });
                            break;
                        case "tamper_detection_tripped":
                            break;
                        case "sensor_triggered":
                            break;
                        case "sensor_untriggered":
                            break;
                        case "alarm_activated_not_armed":
                            break;
                        case "alert":
                            Alarmed = true;
                            break;
                        case "syncStatus":
                            // Got a sync request-response from the FMC
                            if (result.ContainsKey("statusValues"))
                            {
                                IList<string> statusValues = ((JArray)result["statusValues"]).ToObject<IList<string>>();
                                foreach (string value in statusValues)
                                {
                                    switch (value)
                                    {
                                        case "alarmed":
                                            Alarmed = true;
                                            break;
                                        case "silenced":
                                            Alarmed = false;
                                            break;
                                        case "armed":
                                            Armed = true;
                                            break;
                                        case "disarmed":
                                            Armed = false;
                                            break;
                                        case "override":
                                            Override = true;
                                            break;
                                        case "no_override":
                                            Override = false;
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends a command to the controller over serial,
        /// and waits for execution.
        /// </summary>
        /// <param name="command">The command to execute, including parameters.</param>
        /// <returns>The result.</returns>
        public async Task<bool> SendCommand(string command, bool waitForConfirmation = true)
        {
            logger.LogDebug(String.Format("Sending a command to the FMC serial interface: \"{0}\"", command));

            string commandGuid = Guid.NewGuid().ToString();
            await serialController.TryWriteString(command);
            serialController.CommandInProgress = true;

            if (!waitForConfirmation)
                return true;

            pendingCommands.Add(commandGuid, new PendingCommand(commandGuid, command, DateTime.Now));

            while (true)
            {
                PendingCommand pendingCommand = pendingCommands[commandGuid];

                if (DateTime.Now >= pendingCommand.TimeSent.AddSeconds(30))
                {
                    // Command execution timed out
                    pendingCommands.Remove(commandGuid);

                    serialController.CommandInProgress = false;
                    logger.LogError("An command intended to run on the FMC timed out. This indicates an internal fault. The command was: " + pendingCommand.Command);
                    return false;
                }

                if (pendingCommand.Confirmed = true && pendingCommand.Status != null)
                {
                    pendingCommands.Remove(commandGuid);

                    serialController.CommandInProgress = false;
                    if (pendingCommand.Status == "success")
                    {
                        logger.LogInformation("An command sent to the FMC succeeded. The command was: " + pendingCommand.Command);
                        return true;
                    }
                    else if (pendingCommand.Status == "failure")
                    {
                        logger.LogError("The FMC reported command failure. The command was: " + pendingCommand.Command);
                        return false;
                    }
                    else
                    {
                        throw new Exception("Malformed command status.");
                    }
                }
            }
        }

        /// <summary>
        /// Triggers the security system alarm.
        /// This is only functional if the system is armed.
        /// </summary>
        public async Task<bool> Alarm()
        {
            if (!Armed)
            {
                logger.LogInformation("Alarm operation was attempted, but the alarm system was not armed.");
                return false;
            }

            bool result = await SendCommand("alarm"); Alarmed = true; return result;
        }

        /// <summary>
        /// Beeps the controller buzzer once.
        /// </summary>
        public async Task<bool> Beep() { return await SendCommand("beep"); }

        /// <summary>
        /// Silences the security alarm.
        /// </summary>
        public async Task<bool> Silence() { bool result = await SendCommand("silence"); Alarmed = !result; return result; }

        /// <summary>
        /// Arms the controller security system.
        /// </summary>
        public async Task<bool> Arm() { bool result = await SendCommand("arm"); Armed = result; return result; }

        /// <summary>
        /// Disarms the security alarm.
        /// This stops all current alarms.
        /// </summary>
        public async Task<bool> Disarm() { bool result = await SendCommand("disarm"); Armed = !result; return result; }

        /// <summary>
        /// Resets the failsafe management controller (FMC).
        /// This will disconnect Serial.
        /// </summary>
        public async Task<bool> Reset() { await SendCommand("reset"); FmcReady = false; return true; }

        /// <summary>
        /// Forces synchronization with the FMC.
        /// </summary>
        public async Task ForceSync() { syncPending = true; await SendCommand("syncStatus"); syncPending = false; }

        private class PendingCommand
        {
            public PendingCommand(string guid, string command, DateTime timeSent) {
                Guid = guid;
                Command = command;
                TimeSent = timeSent;
            }

            public string Guid;
            public string Command;
            public DateTime TimeSent;
            public string Status = null;
            public bool Confirmed = false;
        }
    }
}