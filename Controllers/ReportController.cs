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
using viafront3.Models;
using viafront3.Models.InternalViewModels;
using viafront3.Data;

namespace viafront3.Controllers
{
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

        static double DateTimeToUnix(DateTime date)
        {
            return (date.ToUniversalTime() - epoch).TotalSeconds;
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
    }
}
