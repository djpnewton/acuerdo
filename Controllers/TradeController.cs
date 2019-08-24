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
using viafront3.Models.TradeViewModels;
using viafront3.Data;
using viafront3.Services;
using via_jsonrpc;

namespace viafront3.Controllers
{
    [Route("[controller]/[action]")]
    public class TradeController : BaseSettingsController
    {
        readonly ITripwire _tripwire;
        readonly IUserLocks _userLocks;

        public TradeController(
          ILogger<TradeController> logger,
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context,
          IOptions<ExchangeSettings> settings,
          ITripwire tripwire,
          IUserLocks userLocks) : base(logger, userManager, context, settings)
        {
            _tripwire = tripwire;
            _userLocks = userLocks;
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public async Task<IActionResult> Trade(string market, string side=null, string amount=null, string price=null)
        {
            var user = await GetUser(required: true);
            return View(TradeViewModel.Construct(user, user, market, side, amount, price, _settings));
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

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> LimitOrder(TradeViewModel model)
        {
            // if tripwire tripped cancel
            if (!_tripwire.TradingEnabled())
            {
                _logger.LogError("Tripwire tripped, exiting LimitOrder()");
                this.FlashError($"Trading not enabled");
                return RedirectToAction("Trade", new { market = model.Market });
            }
            (var success, var error) = Utils.ValidateOrderParams(_settings, model.Order, model.Order.Price);
            if (!success)
                return FlashErrorAndRedirect("Trade", model.Market, error);

            var user = await GetUser(required: true);

            // lock process of performing trade
            lock (_userLocks.GetLock(user.Id))
            {
                try
                {
                    //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
                    var via = new ViaJsonRpc(_settings.AccessHttpUrl);
                    (var side, var error2) = Utils.GetOrderSide(model.Order.Side);
                    if (error2 != null)
                        return BadRequest(error2);
                    var order = via.OrderLimitQuery(user.Exchange.Id, model.Market, side, model.Order.Amount, model.Order.Price, _settings.TakerFeeRate, _settings.MakerFeeRate, "viafront");
                    // send email: order created
                    var amountUnit = _settings.Markets[model.Market].AmountUnit;
                    var priceUnit = _settings.Markets[model.Market].PriceUnit;
                    this.FlashSuccess($"Limit Order Created ({order.market} - {order.side}, Amount: {order.amount} {amountUnit}, Price: {order.price} {priceUnit})");
                    return RedirectToAction("Trade", new { market = model.Market });
                }
                catch (ViaJsonException ex)
                {
                    if (ex.Err == ViaError.PUT_LIMIT__BALANCE_NOT_ENOUGH)
                    {
                        this.FlashError($"Limit Order Failed (balance too small)");
                        return RedirectToAction("Trade", new { market = model.Market, side = model.Order.Side, amount = model.Order.Amount, price = model.Order.Price });
                    }
                    throw;
                }
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> MarketOrder(TradeViewModel model)
        {
            // if tripwire tripped cancel
            if (!_tripwire.TradingEnabled())
            {
                _logger.LogError("Tripwire tripped, exiting MarketOrder()");
                this.FlashError($"Trading not enabled");
                return RedirectToAction("Trade", new { market = model.Market });
            }
            (var success, var error) = Utils.ValidateOrderParams(_settings, model.Order, null, marketOrder: true);
            if (!success)
                return FlashErrorAndRedirect("Trade", model.Market, error);

            var user = await GetUser(required: true);

            // lock process of performing trade
            lock (_userLocks.GetLock(user.Id))
            {
                try
                {
                    //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
                    var via = new ViaJsonRpc(_settings.AccessHttpUrl);
                    (var side, var error2) = Utils.GetOrderSide(model.Order.Side);
                    if (error2 != null)
                        return BadRequest(error2);
                    Order order;
                    if (!_settings.MarketOrderBidAmountMoney)
                        order = via.OrderMarketQuery(user.Exchange.Id, model.Market, side, model.Order.Amount, _settings.TakerFeeRate, "viafront", _settings.MarketOrderBidAmountMoney);
                    else
                        order = via.OrderMarketQuery(user.Exchange.Id, model.Market, side, model.Order.Amount, _settings.TakerFeeRate, "viafront");
                    // send email: order created
                    var amountUnit = _settings.Markets[model.Market].AmountUnit;
                    this.FlashSuccess($"Market Order Created ({order.market} - {order.side}, Amount: {order.amount} {amountUnit})");
                    return RedirectToAction("Trade", new { market = model.Market });
                }
                catch (ViaJsonException ex)
                {
                    if (ex.Err == ViaError.PUT_MARKET__BALANCE_NOT_ENOUGH)
                    {
                        this.FlashError($"Market Order Failed (balance too small)");
                        return RedirectToAction("Trade", new { market = model.Market, side = model.Order.Side, amount = model.Order.Amount });
                    }
                    if (ex.Err == ViaError.PUT_MARKET__NO_ENOUGH_TRADER)
                    {
                        this.FlashError($"Market Order Failed (insufficient market depth)");
                        return RedirectToAction("Trade", new { market = model.Market, side = model.Order.Side, amount = model.Order.Amount });
                    }
                    throw;
                }
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> CancelOrder(OrdersPendingPartialViewModel model)
        {
            var user = await GetUser(required: true);

            // lock process of cancelling trade
            lock (_userLocks.GetLock(user.Id))
            {
                //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
                var via = new ViaJsonRpc(_settings.AccessHttpUrl);
                var order = via.OrderCancelQuery(user.Exchange.Id, model.Market, model.OrderId);
            }

            this.FlashSuccess("Order Cancelled");
            return RedirectToAction("Trade", new { market = model.Market });
        }
    }
}
