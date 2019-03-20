using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using viafront3.Models;

namespace viafront3.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Exchange> Exchange { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<AccountCreationRequest> AccountCreationRequests { get; set; }
        public DbSet<DeviceCreationRequest> DeviceCreationRequests { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<Device>()
                .HasIndex(d => d.DeviceKey)
                .IsUnique();

            builder.Entity<AccountCreationRequest>()
                .HasIndex(r => r.Token)
                .IsUnique();

            builder.Entity<DeviceCreationRequest>()
                .HasIndex(r => r.Token)
                .IsUnique();
        }
    }
}
