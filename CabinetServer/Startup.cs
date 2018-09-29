using CabinetServer.Command;
using CabinetServer.Data;
using CabinetServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography.X509Certificates;
using AutoMapper;

namespace CabinetServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
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

            // DEPRECATING!
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
            services.AddSignalR();
            services.AddAutoMapper();
            services.AddSingleton<IHostedService, DatabaseUpdateService>();
            services.AddSingleton<IHostedService, ServerDiscoveryService>();
            services.AddSingleton<IAuthorizationHandler, ControllerRevocationHandler>();
            services.AddScoped<IUserService, UserService>();
            services.AddSingleton<CommandHub>();

            // Always add Mvc LAST.
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseSpaStaticFiles();

            app.UseSignalR(routes =>
            {
                routes.MapHub<CommandHub>("/signalr/CommandHub");
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }

        /// <summary>
        /// Temporary method only!
        /// </summary>
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
    }
}
