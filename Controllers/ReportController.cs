using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using CsvHelper;
using CsvHelper.Configuration;
using viafront3.Models;
using viafront3.Models.ReportViewModels;
using viafront3.Data;
using via_jsonrpc.sql;

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

    [Authorize(Roles = Utils.AdminOrFinanceRole)]
    [Route("[controller]/[action]")]
    public class ReportController : BaseSettingsController
    {
        static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public ReportController(
            ILogger<InternalController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<ExchangeSettings> settings) : base(logger, userManager, context, settings)
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

        public IActionResult Broker(int offset=0, int limit=20, DateTime? startDate=null, DateTime? endDate=null, string orderStatus=null, string notOrderStatus=null, bool csv=false)
        {
            var user = GetUser(required: true).Result;

            var orders = _context.BrokerOrders.AsEnumerable();
            if (startDate.HasValue)
            {
                startDate = StartOfDay(startDate.Value);
                var unixTimestamp = DateTimeToUnix(startDate.Value);
                orders = orders.Where(o => o.Date >= unixTimestamp);
            }
            if (endDate.HasValue)
            {
                endDate = EndOfDay(endDate.Value);
                var unixTimestamp = DateTimeToUnix(endDate.Value);
                orders = orders.Where(o => o.Date <= unixTimestamp);
            }
            if (orderStatus == "")
                orderStatus = null;
            if (orderStatus != null)
                orders = orders.Where(o => o.Status == orderStatus);
            if (notOrderStatus != null)
                orders = orders.Where(o => o.Status != notOrderStatus);
            orders = orders.OrderByDescending(o => o.Date);

            if (csv)
            {
                var stream = new MemoryStream();
                var streamWriter = new StreamWriter(stream);
                streamWriter.AutoFlush = true;
                using (var csvWriter = new CsvWriter(streamWriter, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csvWriter.Configuration.RegisterClassMap<BrokerOrderMap>();
                    csvWriter.WriteRecords(orders);
                    return File(stream.GetBuffer(), "application/octet-stream", "broker.csv");
                }
            }
            else
            {
                var model = new BrokerViewModel
                {
                    User = user,
                    Orders = orders.Skip(offset).Take(limit),
                    Offset = offset,
                    Limit = limit,
                    StartDate = startDate,
                    EndDate = endDate,
                    Count = orders.Count(),
                    OrderStatus = orderStatus,
                    NotOrderStatus = notOrderStatus,
                    AssetSettings = _settings.Assets,
                };
                return View(model);
            }
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
    }
}
