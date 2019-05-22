using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    public class MarketController : BaseSettingsController
    {
        public MarketController(
            ILogger<MarketController> logger, 
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<ExchangeSettings> settings) : base(logger, userManager, context, settings)
        {
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public IActionResult Orderbook(string market)
        {
            Debug.Assert(market != null);
            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var orderDepth = via.OrderDepthQuery(market, _settings.OrderBookLimit, _settings.Markets[market].PriceInterval);

            var ob = new OrderbookPartialViewModel
            {
                AmountUnit = _settings.Markets[market].AmountUnit,
                PriceUnit = _settings.Markets[market].PriceUnit,
                OrderDepth = orderDepth
            };
            var model = new OrderbookViewModel
            {
                User = GetUser().Result,
                Market = market,
                MarketNice = string.Format("{0}/{1}", _settings.Markets[market].AmountUnit, _settings.Markets[market].PriceUnit),
                OrderBook = ob
            };

            return View(model);
        }

        public IActionResult KLines(string market, long start, long end, long interval)
        {
            Debug.Assert(market != null);
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (start == 0)
                start = now - 24 * 3600;
            if (end == 0)
                end = now;
            if (interval == 0)
                interval = 3600;
            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            try
            {
                var klines = via.KlineQuery(market, start, end, interval);
                return Json(klines);
            }
            catch (ViaJsonException ex)
            {
                _logger.LogError(ex, "exception getting klines");
                return BadRequest();
            }
        }
    }
}
