using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using viafront3.Data;
using MySql.Data.MySqlClient;

namespace viafront3.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public virtual Exchange Exchange { get; set; }
        public virtual List<Device> Devices { get; set; }

        public bool EnsureBackendTablesPresent(ILogger logger, MySqlSettings settings)
        {
            var conn = new MySqlConnection($"host={settings.Host};database={settings.Database};uid={settings.User};password={settings.Password};");
            try
            {
                var sqlCmds = new string[] {
                    $"create table if not exists balance_history_{this.Exchange.Id} like balance_history_example;",
                    $"create table if not exists deal_history_{this.Exchange.Id} like deal_history_example;",
                    $"create table if not exists order_history_{this.Exchange.Id} like order_history_example;",
                    $"create table if not exists order_detail_{this.Exchange.Id} like order_detail_example;",
                    $"create table if not exists user_deal_history_{this.Exchange.Id} like user_deal_history_example;"};
                conn.Open();
                var trans = conn.BeginTransaction();
                foreach (var sqlCmd in sqlCmds)
                {
                    var cmd = new MySqlCommand(sqlCmd, conn, trans);
                    cmd.ExecuteNonQuery();
                }
                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                conn.Close();                
            }
            return false;
        }

        public static ApplicationUser GetUserFromExchangeId(ApplicationDbContext context, UserManager<ApplicationUser> userManager, int exchangeId)
        {
            var exch = context.Exchange.SingleOrDefault(e => e.Id == exchangeId);
            if (exch != null)
                return userManager.Users.SingleOrDefault(u => u.Id == exch.ApplicationUserId);
            return null;
        }
    }

    public class Exchange
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
    }

    public class Device
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public int CreationRequestId { get; set; }
        public string Name { get; set; }
        public string DeviceKey { get; set; }
        public string DeviceSecret { get; set; }
        public long Nonce { get; set; }
    }

    public class AccountCreationRequest
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public long Date { get; set; }
        public string Token { get; set; }
        public string Secret { get; set; }
        public bool Completed { get; set; }
        public string RequestedEmail { get; set; }
        public string RequestedDeviceName { get; set; }
    }

    public class DeviceCreationRequest
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public long Date { get; set; }
        public string Token { get; set; }
        public string Secret { get; set; }
        public bool Completed { get; set; }
        public string RequestedDeviceName { get; set; }
    }
}
