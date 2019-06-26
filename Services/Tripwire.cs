using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

using viafront3.Data;

namespace viafront3.Services
{
    public class TripwireStats
    {
        public Dictionary<TripwireEventType, int> Events;
    }

    public interface ITripwire
    {
        TripwireStats Stats(TripwireSettings settings, ApplicationDbContext context);
        Task RegisterEvent(TripwireEventType type);
        bool TradingEnabled();
        bool WithdrawalsEnabled();
    }

    public class Tripwire : ITripwire
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;

        private bool tradingEnabled = true;
        private bool withdrawalsEnabled = true;

        public Tripwire(IServiceProvider services, ILogger<Tripwire> logger)
        {
            _services = services;
            _logger = logger;
        }

        public TripwireStats Stats(TripwireSettings settings, ApplicationDbContext context)
        {
            var stats = new TripwireStats { Events = new Dictionary<TripwireEventType, int>() };
            var start = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(settings.TimePeriodInMinutes));
            foreach (var eventType in Enum.GetValues(typeof(TripwireEventType)).Cast<TripwireEventType>())
            {
                var count = context.TripwireEvents.Count(e => e.Date >= start && e.Type == eventType);
                stats.Events.Add(eventType, count);
            }
            return stats;
        }

        void UpdateTripwire(IServiceScope scope, ApplicationDbContext context)
        {
            var emailSender = scope.ServiceProvider.GetService<IEmailSender>();
            var settings = scope.ServiceProvider.GetRequiredService<IOptions<TripwireSettings>>().Value;

            var start = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(settings.TimePeriodInMinutes));
            foreach (var eventType in Enum.GetValues(typeof(TripwireEventType)).Cast<TripwireEventType>())
            {
                var max = settings.Maximum[eventType];
                var count = context.TripwireEvents.Count(e => e.Date >= start && e.Type == eventType);
                if (count > max)
                {
                    tradingEnabled = false;
                    withdrawalsEnabled = false;
                    var msg = $"Switching off trading and withdrawals.. {count} {eventType} events in the last {settings.TimePeriodInMinutes} minutes";
                    _logger.LogCritical(msg);
                    emailSender.SendEmailAsync(settings.AlertEmail, "Tripwire!", msg).GetAwaiter().GetResult();
                }
            }
        }

        public async Task RegisterEvent(TripwireEventType type)
        {
            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var evt = new TripwireEvent { Date = DateTimeOffset.Now, Type = type };
                var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
                var httpContext = httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
                    if (remoteIpAddress != null)
                        evt.RemoteIpAddress = remoteIpAddress.ToString();
                }
                context.TripwireEvents.Add(evt);
                await context.SaveChangesAsync();
                UpdateTripwire(scope, context);
            }
        }

        public bool TradingEnabled()
        {
            return tradingEnabled;
        }

        public bool WithdrawalsEnabled()
        {
            return withdrawalsEnabled;
        }
    }
}
