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
        public virtual Kyc Kyc { get; set; }
        public virtual List<Withdrawal> Withdrawals { get; set; }

        public bool EnsureExchangePresent(ApplicationDbContext context)
        {
            if (Exchange == null)
            {
                var exch = new Exchange{ ApplicationUserId=Id };
                context.Add(exch);
                return true;
            }
            return false;
        }

        public bool EnsureExchangeBackendTablesPresent(ILogger logger, MySqlSettings settings)
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

        public void UpdateKyc(ILogger logger, ApplicationDbContext context, KycSettings kyc, int level)
        {
            if (level >= kyc.Levels.Count())
            {
                logger.LogError($"Tried to set kyc level to a level that does not exist '{level}'");
                return;
            }

            if (this.Kyc == null)
                context.Kycs.Add(new Kyc {ApplicationUserId = this.Id, Level = level});
            else if (this.Kyc.Level < level)
            {
                this.Kyc.Level = level;
                context.Kycs.Update(this.Kyc);
            }
        }

        public decimal WithdrawalTotalThisPeriod(KycSettings settings)
        {
            DateTime startDate;
            switch (settings.WithdrawalPeriod)
            {
                case WithdrawalPeriod.Daily:
                    startDate = DateTime.Today;
                    break;
                case WithdrawalPeriod.Weekly:
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    break;
                case WithdrawalPeriod.Monthly:
                    var now = DateTime.Now;
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                default:
                    throw new Exception("invalid withdrawal period");
            }

            var timestamp = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
            var withdrawalsThisPeriod = this.Withdrawals.Where(w => w.ApplicationUserId == this.Id && w.Date >= timestamp);

            decimal total = 0;
            foreach (var withdrawal in withdrawalsThisPeriod)
                total += decimal.Parse(withdrawal.WithdrawalAssetEquivalent);
            return total;
        }

        public void AddWithdrawal(ApplicationDbContext context, string asset, decimal amount, decimal withdrawalAssetEquivalent)
        {
            var date = DateTimeOffset.Now.ToUnixTimeSeconds();
            var withdrawal = new Withdrawal
            {
                ApplicationUserId = this.Id,
                Date = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Asset = asset,
                Amount = amount.ToString(),
                WithdrawalAssetEquivalent = withdrawalAssetEquivalent.ToString(),
            };
            context.Withdrawals.Add(withdrawal);
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

    public class Kyc
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public int Level { get; set; }
    }

    public class Withdrawal
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public long Date { get; set; }
        public string Asset { get; set; }
        public string Amount { get; set; }
        public string WithdrawalAssetEquivalent { get; set; }
    }

    public class KycRequest
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public long Date { get; set; }
        public string Token { get; set; }
    }

    public enum BrokerOrderStatus
    {
        Created,
        Incomming,
        Confirmed,
        Sent
    }

    public class BrokerOrder
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public long Date { get; set; }
        public long Expiry { get; set; }
        public string Token { get; set; }
        public string Market { get; set; }
        public string AssetSend { get; set; }
        public decimal AmountSend { get; set; }
        public string AssetReceive { get; set; }
        public decimal AmountReceive { get; set; }
        public decimal Fee { get; set; }
        public string InvoiceId { get; set; }
        public string PaymentAddress { get; set; }
        public string PaymentUrl { get; set; }
        public string Recipient { get; set; }
        public string Status { get; set; }
    }
}
