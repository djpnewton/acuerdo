using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

using viafront3.Data;

namespace viafront3.Services
{
    public class EfTicketStore : ITicketStore
    {
        private readonly IServiceCollection _services;

        public EfTicketStore(IServiceCollection services)
        {
            _services = services;
        }

        public async Task RemoveAsync(string key)
        {
            using (var scope = _services.BuildServiceProvider().CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                if (Guid.TryParse(key, out var id))
                {
                    var ticket = await context.AuthenticationTickets.SingleOrDefaultAsync(x => x.Id == id);
                    if (ticket != null)
                    {
                        context.AuthenticationTickets.Remove(ticket);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            using (var scope = _services.BuildServiceProvider().CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                if (Guid.TryParse(key, out var id))
                {
                    var authenticationTicket = await context.AuthenticationTickets.FindAsync(id);
                    if (authenticationTicket != null)
                    {
                        authenticationTicket.Value = SerializeToBytes(ticket);
                        authenticationTicket.LastActivity = DateTimeOffset.UtcNow;
                        authenticationTicket.Expires = ticket.Properties.ExpiresUtc;
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            using (var scope = _services.BuildServiceProvider().CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                if (Guid.TryParse(key, out var id))
                {
                    var authenticationTicket = await context.AuthenticationTickets.FindAsync(id);
                    if (authenticationTicket != null)
                    {
                        authenticationTicket.LastActivity = DateTimeOffset.UtcNow;
                        await context.SaveChangesAsync();

                        return DeserializeFromBytes(authenticationTicket.Value);
                    }
                }
            }

            return null;
        }

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            using (var scope = _services.BuildServiceProvider().CreateScope())
            {
                var userId = string.Empty;
                var nameIdentifier = ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();

                if (ticket.AuthenticationScheme == "Identity.Application")
                {
                    userId = nameIdentifier;
                }
                else if (ticket.AuthenticationScheme == "Identity.External")
                {
                    userId = (await context.UserLogins.SingleAsync(x => x.ProviderKey == nameIdentifier)).UserId;
                }

                var authenticationTicket = new Models.AuthenticationTicket()
                {
                    UserId = userId,
                    LastActivity = DateTimeOffset.UtcNow,
                    Value = SerializeToBytes(ticket)
                };

                var expiresUtc = ticket.Properties.ExpiresUtc;
                if (expiresUtc.HasValue)
                {
                    authenticationTicket.Expires = expiresUtc.Value;
                }

                var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
                var httpContext = httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
                    if (remoteIpAddress != null)
                    {
                        authenticationTicket.RemoteIpAddress = remoteIpAddress.ToString();
                    }

                    var userAgent = httpContext.Request.Headers["User-Agent"];
                    if (!string.IsNullOrEmpty(userAgent))
                    {
                        var uaParser = UAParser.Parser.GetDefault();
                        var clientInfo = uaParser.Parse(userAgent);
                        authenticationTicket.OperatingSystem = clientInfo.OS.ToString();
                        authenticationTicket.UserAgentFamily = clientInfo.UserAgent.Family;
                        authenticationTicket.UserAgentVersion = $"{clientInfo.UserAgent.Major}.{clientInfo.UserAgent.Minor}.{clientInfo.UserAgent.Patch}";
                    }
                }

                context.AuthenticationTickets.Add(authenticationTicket);
                await context.SaveChangesAsync();

                return authenticationTicket.Id.ToString();
            }
        }

        private byte[] SerializeToBytes(AuthenticationTicket source)
            => TicketSerializer.Default.Serialize(source);

        private AuthenticationTicket DeserializeFromBytes(byte[] source)
            => source == null ? null : TicketSerializer.Default.Deserialize(source);
    }
}
