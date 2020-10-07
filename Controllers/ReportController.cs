using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using viafront3.Models;
using viafront3.Models.ReportViewModels;
using viafront3.Models.InternalViewModels;
using viafront3.Data;
using viafront3.Services;
using via_jsonrpc.sql;
using xchwallet;

namespace viafront3.Controllers
{
    public sealed class BrokerOrderMap : ClassMap<BrokerOrder>
    {
        public BrokerOrderMap()
        {
            Map(m => m.Date);
            Map(m => m.User).ConvertUsing(row => row.User.Email);
            Map(m => m.Token);
            Map(m => m.InvoiceId);
            Map(m => m.Market);
            Map(m => m.Price);
            Map(m => m.AssetSend);
            Map(m => m.AmountSend);
            Map(m => m.AssetReceive);
            Map(m => m.AmountReceive);
            Map(m => m.Fee);
            Map(m => m.PaymentAddress);
            Map(m => m.PaymentUrl);
            Map(m => m.TxIdPayment);
            Map(m => m.TxIdRecipient);
            Map(m => m.Status);
        }
    }

    public sealed class DealMap : ClassMap<via_jsonrpc.sql.Deal>
    {
        public DealMap()
        {
            Map(m => m.time).Name("Date");
            Map(m => m.user_id).Name("Exchange User Id");
            Map(m => m.market).Name("Market");
            Map(m => m.deal_id).Name("Trade Id");
            Map(m => m.order_id).Name("Order Id");
            //Map(m => m.deal_order_id).Name("Counterparty Order Id");
            Map(m => m.side).Name("Side");
            Map(m => m.role).Name("Role");
            Map(m => m.price).Name("Price");
            Map(m => m.amount).Name("Amount (base currency)");
            Map(m => m.deal).Name("Amount (quote currency)");
            Map(m => m.fee).Name("Fee");
            //Map(m => m.deal_fee).Name("Counterparty fee");
        }
    }

    public sealed class UserInfoMap : ClassMap<UserInfo>
    {
        public UserInfoMap()
        {
            Map(m => m.User.Email).Name("Email");
            Map(m => m.User.UserName).Name("Name");
            Map(m => m.User.Exchange).Name("Exchange Id").ConvertUsing(ui => ui.User.Exchange == null ? "" : ui.User.Exchange.Id.ToString());
            Map(m => m.Roles).Name("Roles").ConvertUsing(ui => string.Join(",", ui.Roles));
            Map(m => m.User.Kyc).Name("Kyc Level").ConvertUsing(ui => ui.User.Kyc == null ? "" : ui.User.Kyc.Level.ToString());
        }
    }

    [Authorize(Roles = Utils.AdminOrFinanceRole)]
    [Route("[controller]/[action]")]
    public class ReportController : BaseWalletController
    {
        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public ReportController(
            ILogger<InternalController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWalletProvider walletProvider,
            IOptions<KycSettings> kycSettings,
            IOptions<ExchangeSettings> settings) : base(logger, userManager, context, settings, walletProvider, kycSettings)
        {
        }

        static DateTime StartOfDay(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0);
        }

        static DateTime EndOfDay(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999);
        }

        static long DateTimeToUnix(DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - epoch).TotalSeconds);
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public IActionResult Broker(int offset=0, int limit=20, DateTime? startDate=null, DateTime? endDate=null, string email=null, string orderStatus=null, string notOrderStatus=null, bool csv=false)
        {
            var user = GetUser(required: true).Result;

            long startUnixTimestamp = 0;
            if (startDate.HasValue)
            {
                startDate = StartOfDay(startDate.Value);
                startUnixTimestamp = DateTimeToUnix(startDate.Value);
            }
            long endUnixTimestamp = 0;
            if (endDate.HasValue)
            {
                endDate = EndOfDay(endDate.Value);
                endUnixTimestamp = DateTimeToUnix(endDate.Value);
            }
            var orders = _context.BrokerOrders.Include(o => o.User).
                                               Where(o => (!startDate.HasValue || o.Date >= startUnixTimestamp) && (!endDate.HasValue || o.Date <= endUnixTimestamp) &&
                                                          (string.IsNullOrEmpty(email) || o.User.Email == email) &&
                                                          (string.IsNullOrEmpty(orderStatus) || o.Status == orderStatus) &&
                                                          (string.IsNullOrEmpty(notOrderStatus) || o.Status != notOrderStatus)).
                                               OrderByDescending(o => o.Date);

            if (csv)
            {
                var stream = new MemoryStream();
                var streamWriter = new StreamWriter(stream);
                streamWriter.AutoFlush = true;
                using (var csvWriter = new CsvWriter(streamWriter, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csvWriter.Configuration.RegisterClassMap<BrokerOrderMap>();
                    csvWriter.WriteRecords(orders.AsEnumerable());
                    return File(stream.GetBuffer(), "application/octet-stream", "broker.csv");
                }
            }
            else
            {
                var count = orders.Count();
                var model = new BrokerViewModel
                {
                    User = user,
                    Orders = orders.Skip(offset).Take(limit).AsEnumerable(),
                    Offset = offset,
                    Limit = limit,
                    StartDate = startDate,
                    EndDate = endDate,
                    Email = email,
                    Count = count,
                    OrderStatus = orderStatus,
                    NotOrderStatus = notOrderStatus,
                    AssetSettings = _settings.Assets,
                };
                return View(model);
            }
        }


        public IActionResult BrokerOrder(string token)
        {
            var user = GetUser(required: true).Result;

            var brokerOrder = _context.BrokerOrders.Where(bo => bo.Token == token).FirstOrDefault();
            WalletPendingSpend pendingSpend = null;
            FiatWalletTx fiatTx = null;
            if (_walletProvider.IsChain(brokerOrder.AssetReceive))
            {
                var boc = _context.BrokerOrderChainWithdrawals.Where(boc => boc.BrokerOrderId == brokerOrder.Id).FirstOrDefault();
                if (boc != null)
                    pendingSpend = _walletProvider.GetChain(brokerOrder.AssetReceive).PendingSpendGet(boc.SpendCode);
            }
            else
            {
                var bof = _context.BrokerOrderFiatWithdrawals.Where(bof => bof.BrokerOrderId == brokerOrder.Id).FirstOrDefault();
                if (bof != null)
                    fiatTx = _walletProvider.GetFiat(brokerOrder.AssetReceive).GetTx(bof.DepositCode);
            }
            // get user kyc request
            string kycRequestUrl = null;
            var kycRequest = _context.KycRequests.Where(r => r.ApplicationUserId == brokerOrder.ApplicationUserId).OrderByDescending(r => r.Date).FirstOrDefault();
            if (kycRequest != null)
                kycRequestUrl = $"{_kycSettings.KycServerUrl}/request/{kycRequest.Token}";
            var model = new BrokerOrderViewModel
            {
                User = user,
                Order = brokerOrder,
                ChainWithdrawal = pendingSpend,
                FiatWithdrawal = fiatTx,
                KycRequestUrl = kycRequestUrl,
                AssetSettings = _settings.Assets
            };
            return View(model);
        }

        public IActionResult Exchange(int offset = 0, int limit = 20, DateTime? startDate = null, DateTime? endDate = null, string orderStatus = null, string notOrderStatus = null, bool csv = false)
        {
            var user = GetUser(required: true).Result;

            var startUnixTimestamp = 0L;
            var endUnixTimestamp = 0L;
            if (startDate.HasValue)
            {
                startDate = StartOfDay(startDate.Value);
                startUnixTimestamp = DateTimeToUnix(startDate.Value);
            }
            if (endDate.HasValue)
            {
                endDate = EndOfDay(endDate.Value);
                endUnixTimestamp = DateTimeToUnix(endDate.Value);
            }

            if (csv)
            {
                var deals = ViaSql.ExchangeDeals(_logger, _settings.MySql.Host, _settings.MySql.Database, _settings.MySql.User, _settings.MySql.Password, startUnixTimestamp, endUnixTimestamp);
                var stream = new MemoryStream();
                var streamWriter = new StreamWriter(stream);
                streamWriter.AutoFlush = true;
                using (var csvWriter = new CsvWriter(streamWriter, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csvWriter.Configuration.RegisterClassMap<DealMap>();
                    csvWriter.WriteRecords(deals);
                    return File(stream.GetBuffer(), "application/octet-stream", "exchange.csv");
                }
            }
            else
            {
                var deals = ViaSql.ExchangeDeals(_logger, _settings.MySql.Host, _settings.MySql.Database, _settings.MySql.User, _settings.MySql.Password, startUnixTimestamp, endUnixTimestamp, offset, limit);
                var count = ViaSql.ExchangeDealsCount(_logger, _settings.MySql.Host, _settings.MySql.Database, _settings.MySql.User, _settings.MySql.Password, startUnixTimestamp, endUnixTimestamp);
                var model = new ExchangeViewModel
                {
                    User = user,
                    Deals = deals,
                    Offset = offset,
                    Limit = limit,
                    StartDate = startDate,
                    EndDate = endDate,
                    Count = count,
                    AssetSettings = _settings.Assets,
                };
                return View(model);
            }
        }

        public IActionResult Users(int offset = 0, int limit = 20, string role = null, string emailSearch = null, string nameSearch = null, bool csv = false)
        {
            var user = GetUser(required: true).Result;

            if (csv)
            {
                var userInfos = UserInfo.Query(_context, role, emailSearch, nameSearch);
                var stream = new MemoryStream();
                var streamWriter = new StreamWriter(stream);
                streamWriter.AutoFlush = true;
                using (var csvWriter = new CsvWriter(streamWriter, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csvWriter.Configuration.RegisterClassMap<UserInfoMap>();
                    csvWriter.WriteRecords(userInfos.ToList());
                    return File(stream.GetBuffer(), "application/octet-stream", "users.csv");
                }
            }
            else
            {
                var userInfos = UserInfo.Query(_context, role, emailSearch, nameSearch);
                var model = new UsersViewModel
                {
                    User = user,
                    UserInfos = userInfos.Skip(offset).Take(limit).ToList(),
                    Offset = offset,
                    Limit = limit,
                    Count = userInfos.Count(),
                    Role = role,
                    EmailSearch = emailSearch,
                    NameSearch = nameSearch,
                };
                return View(model);
            }
        }
    }
}
