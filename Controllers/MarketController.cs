using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.MarketViewModels;
using via_jsonrpc;

namespace viafront3.Controllers
{
    [Route("[controller]/[action]")]
    public class MarketController : Controller
    {
        private readonly ExchangeSettings _settings;

        public MarketController(IOptions<ExchangeSettings> settings)
        {
            _settings = settings.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Orderbook(string id)
        {
            var via = new ViaJsonRpc(_settings.AccessHttpHost);
            var orderDepth = via.OrderDepthQuery(id, _settings.OrderBookLimit, _settings.OrderBookInterval);

            var model = new OrderbookViewModel
            {
                Market = id,
                MarketNice = string.Format("{0}/{1}", _settings.Markets[id].MarketAmountUnit, _settings.Markets[id].MarketPriceUnit),
                AmountUnit = _settings.Markets[id].MarketAmountUnit,
                PriceUnit = _settings.Markets[id].MarketPriceUnit,
                OrderDepth = orderDepth
            };

            return View(model);
        }
    }
}
