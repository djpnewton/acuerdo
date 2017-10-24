using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using viafront3.Models;
using via_jsonrpc;

namespace viafront3.Controllers
{
    [Route("[controller]/[action]")]
    public class MarketController : Controller
    {
        public IActionResult Orderbook()
        {
            var via = new ViaJsonRpc("http://10.50.1.2:8080");
            var orderDepth = via.OrderDepthQuery("BTCCNY", 10, "1");

            ViewData["Market"] = "BTC/CNY";
            ViewData["AmountUnit"] = "BTC";
            ViewData["PriceUnit"] = "CNY";
            ViewData["OrderDepth"] = orderDepth;

            return View();
        }
    }
}
