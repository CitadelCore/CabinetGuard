using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CorePlatform.Utilities;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Configuration;

namespace CabinetServer.Services
{
    public class ServerDiscoveryService : BackgroundService
    {
        private readonly ILogger<ServerDiscoveryService> _logger;
        private readonly IConfiguration _configuration;
        public ServerDiscoveryService(ILogger<ServerDiscoveryService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"The Server Discovery Service is starting.");
            stoppingToken.Register(() =>
            {
                _logger.LogDebug($"The Server Discovery Service is stopping.");
            });

            _logger.LogDebug($"Starting the broadcast reciever on Port {_configuration["Settings:Discovery:DiscoveryPort"]}.");
            UdpClient serverClient = new UdpClient(Convert.ToInt32(_configuration["Settings:Discovery:DiscoveryPort"]));
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for a request.
                UdpReceiveResult result = await serverClient.ReceiveAsync().WithCancellation(stoppingToken);
                string resultString = Encoding.ASCII.GetString(result.Buffer);

                // Respond with this server's FQDN and ports.
                IDictionary<string, dynamic> data = new Dictionary<string, dynamic>()
                {
                    { "hostname", Dns.GetHostName().ToLower() + "." + IPGlobalProperties.GetIPGlobalProperties().DomainName },
                    { "port", _configuration["Settings:WebServer:SslPort"] },
                    { "certPort", _configuration["Settings:WebServer:SslAuthPort"] },
                };

                byte[] response = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
                await serverClient.SendAsync(response, response.Length, result.RemoteEndPoint.Address.ToString(), result.RemoteEndPoint.Port).WithCancellation(stoppingToken);

                _logger.LogDebug(String.Format("Served request from client with IP {0} successfully.", result.RemoteEndPoint));
            }

            _logger.LogDebug($"The Server Discovery Service is stopping.");
        }
    }
}
