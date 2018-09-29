using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CorePlatform.Models.CabinetModels;

namespace CabinetServer.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CabinetController> Controllers { get; set; }
        public DbSet<Cabinet> Cabinets { get; set; }
        public DbSet<CabinetDoor> MonitoredDoors { get; set; }
        public DbSet<FanModule> FanModules { get; set; }
        public DbSet<Fan> Fans { get; set; }
        public DbSet<ScheduledCommand> ScheduledCommands { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<ScheduledCommand>()
                .Property(s => s._Payload).HasColumnName("Payload");

            builder.Entity<ScheduledCommand>()
                .Property(s => s._Result).HasColumnName("Result");

            builder.Entity<ScheduledCommand>()
                .Property(s => s._State).HasColumnName("State");

            builder.Entity<ScheduledCommand>()
                .Property(s => s._TimeCreated).HasColumnName("TimeCreated");

            builder.Entity<CabinetController>()
                .HasIndex(p => new { p.Nickname, p.Hostname })
                .IsUnique(true);
        }
    }
}
