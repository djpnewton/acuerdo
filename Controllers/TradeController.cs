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
    public class TradeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        private readonly ExchangeSettings _settings;

        public TradeController(
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context,
          IOptions<ExchangeSettings> settings)
        {
            _userManager = userManager;
            _context = context;
            _settings = settings.Value;
        }

        async Task<ApplicationUser> GetUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (user.EnsureExchangePresent(_context))
                _context.SaveChanges();

            return user;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Trade(string id)
        {
            var market = id;
            var user = await GetUser();

            var via = new ViaJsonRpc(_settings.AccessHttpHost);
            var balances = via.BalanceQuery(user.Exchange.Id);
            var ordersPending = via.OrdersPendingQuery(user.Exchange.Id, market, 0, 10);
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            var bidOrdersCompleted = via.OrdersCompletedQuery(user.Exchange.Id, market, 1, now, 0, 10, OrderSide.Bid);
            var askOrdersCompleted = via.OrdersCompletedQuery(user.Exchange.Id, market, 1, now, 0, 10, OrderSide.Ask);

            var model = new TradeViewModel
            {
                Market = id,
                MarketNice = string.Format("{0}/{1}", _settings.Markets[market].AmountUnit, _settings.Markets[market].PriceUnit),
                AssetSettings = _settings.Assets,
                Settings = _settings.Markets[market],
                Balances = balances,
                OrdersPending = ordersPending,
                BidOrdersCompleted = bidOrdersCompleted,
                AskOrdersCompleted = askOrdersCompleted
            };

            return View(model);
        }

        IActionResult FlashErrorAndRedirect(string action, string market, string error)
        {
            this.FlashError(error);
            return RedirectToAction(action, new { id = market});
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

            var user = await GetUser();
            var via = new ViaJsonRpc(_settings.AccessHttpHost);
            var order = via.OrderLimitQuery(user.Exchange.Id, model.Market, model.Side, model.Amount, model.Price, _settings.TakerFeeRate, _settings.MakerFeeRate, "viafront");
            this.FlashSuccess(string.Format("Limit Order Created ({0} - {1}, Amount: {2}, Price: {3})", order.market, order.side, order.amount, order.price));
            return RedirectToAction("Trade", new { id = model.Market });
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> MarketOrder(TradeViewModel model)
        {
            var result = ValidateOrderParams(model, marketOrder: true);
            if (!result.Item1)
                return result.Item2;

            var user = await GetUser();
            var via = new ViaJsonRpc(_settings.AccessHttpHost);
            var order = via.OrderMarketQuery(user.Exchange.Id, model.Market, model.Side, model.Amount, _settings.TakerFeeRate, "viafront");
            this.FlashSuccess(string.Format("Market Order Created ({0} - {1}, Amount: {2})", order.market, order.side, order.amount));
            return RedirectToAction("Trade", new { id = model.Market });
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> CancelOrder(TradeViewModel model)
        {
            var user = await GetUser();
            var via = new ViaJsonRpc(_settings.AccessHttpHost);
            var order = via.OrderCancelQuery(user.Exchange.Id, model.Market, model.OrderId);
            this.FlashSuccess("Order Cancelled");
            return RedirectToAction("Trade", new { id = model.Market });
        }
    }
}
