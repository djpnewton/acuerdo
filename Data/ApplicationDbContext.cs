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
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<AccountCreationRequest> AccountCreationRequests { get; set; }
        public DbSet<ApiKeyCreationRequest> ApiKeyCreationRequests { get; set; }
        public DbSet<Kyc> Kycs { get; set; }
        public DbSet<Withdrawal> Withdrawals { get; set; }
        public DbSet<KycRequest> KycRequests { get; set; }
        public DbSet<BrokerOrder> BrokerOrders { get; set; }
        public DbSet<AuthenticationTicket> AuthenticationTickets { get; set; }
        public DbSet<TripwireEvent> TripwireEvents { get; set; }

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

            builder.Entity<ApiKey>()
                .HasIndex(d => d.Key)
                .IsUnique();

            builder.Entity<AccountCreationRequest>()
                .HasIndex(r => r.Token)
                .IsUnique();

            builder.Entity<ApiKeyCreationRequest>()
                .HasIndex(r => r.Token)
                .IsUnique();

            builder.Entity<Kyc>()
                .HasIndex(k => k.ApplicationUserId)
                .IsUnique();

            builder.Entity<KycRequest>()
                .HasIndex(r => r.Token)
                .IsUnique();

            builder.Entity<BrokerOrder>()
                .HasIndex(r => r.Token)
                .IsUnique();

            builder.Entity<BrokerOrder>()
                .HasIndex(r => r.InvoiceId)
                .IsUnique();
        }
    }

    public enum TripwireEventType
    {
        LoginAttempt,
        Login,
        ResetPasswordAttempt,
        WithdrawalAttempt,
        Withdrawal,
    }

    public class TripwireEvent
    {
        public int Id { get; set; }
        public DateTimeOffset? Date { get; set; }
        public TripwireEventType Type { get; set; }
        public string RemoteIpAddress { get; set; }
    }
}
