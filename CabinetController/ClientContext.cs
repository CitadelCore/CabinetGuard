using CorePlatform.Models.CabinetModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ManagementController
{
    class ClientContext : DbContext
    {
        public DbSet<ScheduledCommand> ScheduledCommands { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=clientStore.db");
        }
    }
}
