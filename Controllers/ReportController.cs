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
        public ReportController(
            ILogger<InternalController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<ExchangeSettings> settings) : base(logger, userManager, context, settings)
        {
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public IActionResult Broker(int offset=0, int limit=20, DateTime? startDate=null, DateTime? endDate=null, string orderStatus=null, string notOrderStatus=null, bool csv=false)
        {
            var user = GetUser(required: true).Result;

            var orders = _context.BrokerOrders.AsEnumerable();
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (startDate.HasValue)
            {
                var unixTimestamp = (startDate.Value.ToUniversalTime() - epoch).TotalSeconds;
                orders = orders.Where(o => o.Date >= unixTimestamp);
            }
            if (endDate.HasValue)
            {
                var unixTimestamp = (endDate.Value.ToUniversalTime() - epoch).TotalSeconds;
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
                var writeFile = new StreamWriter(stream);
                var csvWriter = new CsvWriter(writeFile, System.Globalization.CultureInfo.InvariantCulture);
                csvWriter.WriteRecords(orders);
                csvWriter.Flush();
                stream.Position = 0; //reset stream
                return File(stream, "application/octet-stream", "broker.csv");
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
