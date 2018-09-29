using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CabinetServer.Web.Data;
using CabinetServer.Web.Models;
using CabinetServer.Web.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Cryptography.X509Certificates;
using CabinetServer.Web.Command;
using Microsoft.AspNetCore.Authorization;
using CabinetServer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using Microsoft.AspNetCore.Authentication;

namespace CabinetServer.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            IConfigurationSection section = configuration.GetSection("ControlServer");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtBearerOptions =>
            {
                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateActor = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = "tower.local",
                    ValidAudience = "tower.local",
                    IssuerSigningKey = new X509SecurityKey(GetMachineCertificate()),
                };

                jwtBearerOptions.IncludeErrorDetails = true;
            }).AddWsFederation(wsFederationOptions =>
            {
                wsFederationOptions.MetadataAddress = "https://login.towerdevs.xyz/FederationMetadata/2007-06/FederationMetadata.xml";
                wsFederationOptions.Wtrealm = "https://login.towerdevs.xyz";
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ControllerValid", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ControllerRevocationRequirement());
                }
                );
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddSignalR();

            services.AddSingleton<IHostedService, DatabaseUpdateService>();
            services.AddSingleton<IHostedService, ServerDiscoveryService>();
            services.AddSingleton<CommandHub>();
            services.AddSingleton<IAuthorizationHandler, ControllerRevocationHandler>();

            services.ConfigureApplicationCookie(options => {
                options.Events.OnRedirectToAccessDenied = ReplaceRedirector(HttpStatusCode.Forbidden, options.Events.OnRedirectToAccessDenied);
                options.Events.OnRedirectToLogin = ReplaceRedirector(HttpStatusCode.Unauthorized, options.Events.OnRedirectToLogin);
            });

            // Always add Mvc LAST.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseSignalR(routes =>
            {
                routes.MapHub<CommandHub>("/signalr/CommandHub");
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public static X509Certificate2 GetMachineCertificate()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindByTemplateName, "Machine", true);
            X509Certificate2 cert;
            if (collection.Count <= 0)
                throw new Exception("No valid machine certificates found!");

            cert = collection[0];
            store.Close();

            return cert;
        }

        private static Func<RedirectContext<CookieAuthenticationOptions>, Task> ReplaceRedirector(HttpStatusCode statusCode, Func<RedirectContext<CookieAuthenticationOptions>, Task> existingRedirector) =>
        context => {
        if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/signalr"))
        {
            context.Response.StatusCode = (int)statusCode;
            return Task.CompletedTask;
        }
        return existingRedirector(context);
    };
    }
}
