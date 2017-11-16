using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.MarketViewModels;
using viafront3.Data;
using via_jsonrpc;

namespace viafront3.Controllers
{
    [Route("[controller]/[action]")]
    public class MarketController : BaseController
    {
        private readonly ExchangeSettings _settings;

        public MarketController(UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<ExchangeSettings> settings) : base(userManager, context)
        {
            _settings = settings.Value;
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public IActionResult Orderbook(string id)
        {
            var via = new ViaJsonRpc(_settings.AccessHttpHost);
            var orderDepth = via.OrderDepthQuery(id, _settings.OrderBookLimit, _settings.OrderBookInterval);

            var model = new OrderbookViewModel
            {
                User = GetUser().Result,
                Market = id,
                MarketNice = string.Format("{0}/{1}", _settings.Markets[id].AmountUnit, _settings.Markets[id].PriceUnit),
                AmountUnit = _settings.Markets[id].AmountUnit,
                PriceUnit = _settings.Markets[id].PriceUnit,
                OrderDepth = orderDepth
            };

            return View(model);
        }

        public IActionResult KLines(string id, long start, long end, long interval)
        {
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (start == 0)
                start = now - 24 * 3600;
            if (end == 0)
                end = now;
            if (interval == 0)
                interval = 3600;
            var via = new ViaJsonRpc(_settings.AccessHttpHost);
            var klines = via.KlineQuery(id, start, end, interval);
            return Json(klines);
        }
    }
}
