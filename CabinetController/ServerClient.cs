using ManagementController.Models;
using CorePlatform.Models.CabinetModels;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Net;
using ManagementController.Services;
using Microsoft.Extensions.Logging;

namespace ManagementController
{
    /// <summary>
    /// Class which handles basic command communication
    /// between the controller and server.
    /// </summary>
    class ServerClient
    {
        private static HttpClient client = new HttpClient();

        private IHubConnectionBuilder hubConnectionBuilder;
        private HubConnection hubConnection;
        private ControllerSettings controllerSettings;

        private DiscoveryClient discoveryClient;

        private static int enrollmentInterval = 60000;
        private Timer enrollmentTimer = new Timer(enrollmentInterval);

        private static int autoReconnectInterval = 10000;
        private Timer autoReconnectTimer = new Timer(autoReconnectInterval);

        public event ServerCommandRecievedDelegate ServerCommandRecieved;
        public delegate Task ServerCommandRecievedDelegate(ScheduledCommand command);

        private string serverUrl;

        public bool Connected { get; private set; }

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        public ServerClient(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<ServerClient>();
            
            controllerSettings = serviceProvider.GetService<ControllerSettings>();
            discoveryClient = serviceProvider.GetService<DiscoveryClient>();

            enrollmentTimer.Elapsed += EnrollmentTimer_Elapsed;
            autoReconnectTimer.Elapsed += AutoReconnectTimer_Elapsed;

            if (String.IsNullOrEmpty(controllerSettings.Guid))
            {
                _logger.LogInformation("Client is not enrolled. Starting enrollment timer.");
                enrollmentTimer.Start();
            }
            else
            {
                _logger.LogInformation("Client is enrolled. Attempting immediate connection.");

                serverUrl = String.Format("https://{0}:{1}", controllerSettings.CCSHostname, controllerSettings.CCSPort);
                CreateConnectionBuilder();
                Task.Run(Connect);
            }
        }

        private async void AutoReconnectTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Connect();
        }

        private async void EnrollmentTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Enroll();
        }

        /// <summary>
        /// Attempts automatic enrollment with the server,
        /// to obtain a JWT security token.
        /// </summary>
        public async Task<bool> Enroll()
        {
            _logger.LogInformation("Starting enrollment sequence.");

            HttpResponseMessage message;
            IDictionary<string, dynamic> serverResult;

            try
            {
                // Broadcast discovery packets to obtain server information.
                _logger.LogInformation("Invoking the server discovery provider.");
                serverResult = discoveryClient.AwaitDiscovery();
                if (serverResult == null)
                    // No result from the server in the allotted time.
                    return false;

                _logger.LogInformation("Attempting secure enrollment and key exchange via the CCS REST API.");
                message = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, String.Format("https://{0}:{1}/api/Enrollment/Enroll", serverResult["hostname"], serverResult["port"])));

                if (message.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("Attempted to enroll, but the controller is not authorized for enrollment yet. Enrollment will be retried.");
                    return false;
                }

                message.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                _logger.LogError("Caught an exception while attempting to enroll.", e);
                return false;
            }

            // Extract the JWT token from the result and save it
            IDictionary<string, dynamic> values = JsonConvert.DeserializeObject<IDictionary<string, dynamic>>(await message.Content.ReadAsStringAsync());
            controllerSettings.JwtToken = values["token"];
            controllerSettings.Guid = values["guid"];

            // Save the discovery information
            controllerSettings.CCSHostname = serverResult["hostname"];
            controllerSettings.CCSPort = serverResult["port"];
            controllerSettings.CCSCertPort = serverResult["certPort"];

            // Save all settings
            await controllerSettings.SaveAsync();
            _logger.LogInformation("New controller settings have been configured.");

            // Recreate settings with the new URL
            serverUrl = String.Format("https://{0}:{1}", serverResult["hostname"], serverResult["port"]);
            CreateConnectionBuilder();

            // Stop the timer
            enrollmentTimer.Stop();

            // Attempt connection
            _logger.LogInformation("Enrollment successful. Initiating connection now.");
            await Task.Run(Connect);

            return true;
        }

        /// <summary>
        /// Attempts to connect to the hub.
        /// </summary>
        public async Task Connect()
        {
            _logger.LogInformation("Attempting to connect to the message bus.");

            if (String.IsNullOrEmpty(controllerSettings.JwtToken))
            {
                _logger.LogInformation("JWT token is missing or not found. Cannot connect.");
                return;
            }

            try
            {
                // First check our controller's state via the API endpoint.
                _logger.LogInformation("Checking client enrollment state via CCS API.");
                HttpResponseMessage message = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, serverUrl + "/api/Enrollment/Get/" + controllerSettings.Guid.ToString()));

                // If the controller object isn't found, the controller has been unenrolled
                // without being notified. Controller should force disconnect itself.
                if (message.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Controller does not exist on server side. Unenrolling client.");
                    await Unenroll();
                    return;
                }

                message.EnsureSuccessStatusCode();
                CabinetController controller = JsonConvert.DeserializeObject<CabinetController>(await message.Content.ReadAsStringAsync());

                _logger.LogInformation("Client enrollment state is valid.");
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to verify the controller enrollment status. The connection will be retried.", e);
                autoReconnectTimer.Start();
            }

            // Connect via SignalR.
            try
            {
                _logger.LogInformation("Connecting to SignalR.");

                hubConnection = hubConnectionBuilder.Build();

                await hubConnection.StartAsync();
                autoReconnectTimer.Stop();

                hubConnection.On<ScheduledCommand>("RecieveCommand", command =>
                {
                    Task.Run(() => HandleCommand(command));
                });

                hubConnection.Closed += HubConnection_Closed;
                Connected = true;

                _logger.LogInformation("Connected to SignalR successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError("Error connecting to SignalR. The connection will be retried.", e);
                autoReconnectTimer.Start();
            }
        }

        private Task HubConnection_Closed(Exception obj)
        {
            _logger.LogInformation("Recieved a connection closed event from the SignalR hub.");
            Connected = false;

            if (!String.IsNullOrEmpty(controllerSettings.JwtToken) && obj != null)
            {
                _logger.LogInformation("Starting timer to attempt reconnection.");

                // Start the reconnection timer
                autoReconnectTimer.Start();
                return Task.CompletedTask;
            }

            // Connection was closed manually, don't restart it.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles a command from the server.
        /// </summary>
        private async Task HandleCommand(ScheduledCommand command)
        {
            // Update the server state of the command
            command.State = CommandState.InProgress;
            await UpdateCommand(command);

            switch(command.Name)
            {
                case "arm":
                    await ServerCommandRecieved.Invoke(command);
                    break;
                case "disarm":
                    await ServerCommandRecieved.Invoke(command);
                    break;
                case "alarm":
                    await ServerCommandRecieved.Invoke(command);
                    break;
                case "silence":
                    await ServerCommandRecieved.Invoke(command);
                    break;
                case "clientDelete":
                    command.State = CommandState.Complete;
                    command.Result = CommandResult.Success;
                    await UpdateCommand(command);
                    await Unenroll();
                    
                    break;
                default:
                    // Command is invalid.
                    command.State = CommandState.Complete;
                    command.Result = CommandResult.Invalid;
                    await UpdateCommand(command);
                    break;
            }
        }

        /// <summary>
        /// Un-enrolls the client from the server.
        /// This closes the hub connection, removes the security token,
        /// recreates the connection builder and restarts the enrollment discovery timer.
        /// </summary>
        private async Task Unenroll()
        {
            if (hubConnection != null)
                await hubConnection.StopAsync();

            // Reset settings, including discovery information
            controllerSettings.JwtToken = null;
            controllerSettings.Guid = null;
            controllerSettings.ClearCCS();
            await controllerSettings.SaveAsync();

            CreateConnectionBuilder();
            enrollmentTimer.Start();

            _logger.LogInformation("Client unenrolled successfully.");
        }

        private void CreateConnectionBuilder()
        {
            hubConnectionBuilder = new HubConnectionBuilder()
                .WithUrl(serverUrl + "/signalr/CommandHub", options => { options.Headers = new Dictionary<string, string> { { "Authorization", String.Format("Bearer {0}", controllerSettings.JwtToken) } }; });
        }

        /// <summary>
        /// Updates the status of the cabinet on the server.
        /// </summary>
        public async Task UpdateCabinetStatus(Cabinet cabinet)
        {
            if (Connected)
                await hubConnection.SendAsync("UpdateCabinetStatus", new[] { cabinet });
        }

        /// <summary>
        /// Updates the state of a command on the server side.
        /// </summary>
        public async Task UpdateCommand(ScheduledCommand command)
        {
            if (Connected)
                await hubConnection.SendAsync("UpdateCommand", new[] { command });
        }
    }
}
