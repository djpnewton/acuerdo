using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.TradeViewModels;
using viafront3.Data;
using via_jsonrpc;

namespace viafront3.Controllers
{
    [Route("[controller]/[action]")]
    public class TradeController : BaseSettingsController
    {
        public TradeController(
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context,
          IOptions<ExchangeSettings> settings) : base(userManager, context, settings)
        {
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public async Task<IActionResult> Trade(string market)
        {
            var user = await GetUser(required: true);
            return View(TradeViewModel.Construct(user, user, market, _settings));
        }

        public async Task<IActionResult> OrdersPending(string market, int offset=0, int limit=10)
        {
            var user = await GetUser(required: true);
            return View(OrdersPendingViewModel.Construct(user, user, market, _settings, offset, limit));
        }

        public async Task<IActionResult> BidOrdersCompleted(string market, int offset=0, int limit=10)
        {
            var user = await GetUser(required: true);
            return View(OrdersCompletedViewModel.Construct(user, user, market, OrderSide.Bid, _settings, offset, limit));
        }

        public async Task<IActionResult> AskOrdersCompleted(string market, int offset=0, int limit=10)
        {
            var user = await GetUser(required: true);
            return View(OrdersCompletedViewModel.Construct(user, user, market, OrderSide.Ask, _settings, offset, limit));
        }

        IActionResult FlashErrorAndRedirect(string action, string market, string error)
        {
            this.FlashError(error);
            return RedirectToAction(action, new { market = market});
        }

        Tuple<bool, IActionResult> ValidateOrderParams(TradeViewModel model, bool marketOrder=false)
        {
            if (model.Market == null || !_settings.Markets.ContainsKey(model.Market))
                return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Index", model.Market, "Market does not exist"));
            if (model.Amount == null || float.Parse(model.Amount) <= 0)
                return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Trade", model.Market, "Amount <= 0"));
            if (!marketOrder && (model.Price == null || float.Parse(model.Price) <= 0))
                return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Trade", model.Market, "Price <= 0"));
            return new Tuple<bool, IActionResult>(true, null);
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> LimitOrder(TradeViewModel model)
        {
            var result = ValidateOrderParams(model);
            if (!result.Item1)
                return result.Item2;

            var user = await GetUser(required: true);
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var order = via.OrderLimitQuery(user.Exchange.Id, model.Market, model.Side, model.Amount, model.Price, _settings.TakerFeeRate, _settings.MakerFeeRate, "viafront");
            this.FlashSuccess(string.Format("Limit Order Created ({0} - {1}, Amount: {2}, Price: {3})", order.market, order.side, order.amount, order.price));
            return RedirectToAction("Trade", new { market = model.Market });
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> MarketOrder(TradeViewModel model)
        {
            var result = ValidateOrderParams(model, marketOrder: true);
            if (!result.Item1)
                return result.Item2;

            var user = await GetUser(required: true);
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var order = via.OrderMarketQuery(user.Exchange.Id, model.Market, model.Side, model.Amount, _settings.TakerFeeRate, "viafront", bid_amount_money: false);
            this.FlashSuccess(string.Format("Market Order Created ({0} - {1}, Amount: {2})", order.market, order.side, order.amount));
            return RedirectToAction("Trade", new { market = model.Market });
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> CancelOrder(OrdersPendingPartialViewModel model)
        {
            var user = await GetUser(required: true);
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var order = via.OrderCancelQuery(user.Exchange.Id, model.Market, model.OrderId);
            this.FlashSuccess("Order Cancelled");
            return RedirectToAction("Trade", new { market = model.Market });
        }
    }
}
