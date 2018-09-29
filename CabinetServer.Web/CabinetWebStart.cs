using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CabinetServer.Web
{
    public class CabinetWebStart
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Any, 44347), listenOptions =>
                    {
                        HttpsConnectionAdapterOptions adapterOptions = new HttpsConnectionAdapterOptions()
                        {
                            ClientCertificateMode = ClientCertificateMode.AllowCertificate,
                            SslProtocols = SslProtocols.Tls,
                            ServerCertificate = Startup.GetMachineCertificate(),
                        };

                        listenOptions.UseHttps(adapterOptions);
                    });

                    options.Listen(new IPEndPoint(IPAddress.Any, 44348), listenOptions =>
                    {
                        HttpsConnectionAdapterOptions adapterOptions = new HttpsConnectionAdapterOptions()
                        {
                            ClientCertificateMode = ClientCertificateMode.NoCertificate,
                            SslProtocols = SslProtocols.Tls,
                            ServerCertificate = Startup.GetMachineCertificate(),
                        };

                        listenOptions.UseHttps(adapterOptions);
                    });
                })
            .UseIISIntegration()
            .UseStartup<Startup>()
            .Build();
    }
}
