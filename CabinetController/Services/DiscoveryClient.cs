using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CorePlatform.Utilities;
using Microsoft.Extensions.Logging;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Networking;

namespace ManagementController.Services
{
    /// <summary>
    /// Class responsible for broadcast discovery
    /// of Cabinet Management servers on the local subnet.
    /// </summary>
    class DiscoveryClient
    {
        /// <summary>
        /// This event is invoked when a server replies to a
        /// Cabinet Controller discovery request.
        /// </summary>
        public event ServerDiscoveredDelegate ServerDiscovered;
        public delegate Task ServerDiscoveredDelegate(IDictionary<string, dynamic> resultDict);

        private ILogger Logger;
        //private UdpClient Client;
        private DatagramSocket Socket;
        private Guid ClientId;

        private System.Timers.Timer DiscoveryTimer;
        private CancellationTokenSource tokenSource;

        private static int DiscoveryPort = 44350;

        public DiscoveryClient(ILogger logger, Guid clientId)
        {
            Logger = logger;
            ClientId = clientId;
        }

        /// <summary>
        /// Attempts discovery and waits until the specified timeout.
        /// </summary>
        /// <param name="timeout">Timeout for discovery.</param>
        public IDictionary<string, dynamic> AwaitDiscovery(int timeout = 30000)
        {
            Logger.LogInformation("Starting discovery and waiting for discovery tasks to complete.");
            IDictionary<string, dynamic> resultDict = null;

            BeginDiscovery();
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
            ServerDiscovered += (result) => {
                resultDict = result;
                source.TrySetResult(true);
                return Task.CompletedTask;
            };

            Task.WaitAny(Task.Delay(timeout), source.Task);
            StopDiscovery();

            return resultDict;
        }

        public void BeginDiscovery(int interval = 4000)
        {
            Logger.LogInformation("Dynamic server discovery is starting.");
            tokenSource = new CancellationTokenSource();

            /**
            Client = new UdpClient
            {
                EnableBroadcast = true
            };*/

            Socket = new DatagramSocket();
            Socket.Control.MulticastOnly = false;
            Socket.MessageReceived += Socket_MessageReceived;

            DiscoveryTimer = new System.Timers.Timer(interval);
            DiscoveryTimer.Elapsed += DiscoveryTimer_Elapsed;
            DiscoveryTimer.Start();
        }

        private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                uint stringLength = args.GetDataReader().UnconsumedBufferLength;
                string receivedMessage = args.GetDataReader().ReadString(stringLength);

                Logger.LogDebug(String.Format("Got a control message from the CCS: {0}", receivedMessage));
                IDictionary<string, dynamic> control = JsonConvert.DeserializeObject<IDictionary<string, dynamic>>(receivedMessage);

                // Three things are discovered here: the hostname of the CCS, the REST API port and the dedicated certificate authentication port.
                Logger.LogInformation(String.Format("Discovered a CCS server at {0}, with SSL port {1} and CertAuth port {2}.", (string)control["hostname"], Convert.ToString((long)control["port"]), Convert.ToString((long)control["certPort"])));
                ServerDiscovered?.Invoke(control);
            }
            catch (Exception e)
            {
                Logger.LogError("Unable to retrieve the UDP message because an exception was thrown.", e);
            }
        }

        public void StopDiscovery()
        {
            Logger.LogInformation("Dynamic server discovery is stopping.");
            DiscoveryTimer.Stop();
            tokenSource.Cancel();
            //Client.Close();

            // Dispose the socket
            if (Socket != null)
            {
                Socket.Dispose();
                Socket = null;
            }
        }

        private void DiscoveryTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Task.Run(BroadcastTask).Wait();
            DiscoveryTimer.Start();
        }

        public async Task BroadcastTask()
        {
            Logger.LogInformation(String.Format("Attempting dynamic discovery on UDP broadcast port {0}.", DiscoveryPort));

            // Don't do anything if we've already disposed the socket
            if (Socket == null)
                return;

            try
            {
                // HACK: UDP broadcast only works on directed broadcast address, FIX THIS MICROSOFT!
                // does it only work in the subnet we're currently in?
                using (IOutputStream output = await Socket.GetOutputStreamAsync(new HostName("192.168.1.255"), Convert.ToString(DiscoveryPort)))
                {
                    using (DataWriter writer = new DataWriter(output))
                    {
                        writer.WriteString(Convert.ToString(ClientId));
                        await writer.StoreAsync();
                    }
                }

                //byte[] data = Encoding.ASCII.GetBytes(ClientId.ToString());
                //await Client.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort)).WithCancellation(tokenSource.Token);
                //string resultString = Encoding.ASCII.GetString((await Client.ReceiveAsync().WithCancellation(tokenSource.Token)).Buffer);
            }
            catch (Exception e)
            {
                Logger.LogError("Caught an exception while attempting to send the broadcast message. The attempt will be retried.", e);
            }
        }
    }
}
