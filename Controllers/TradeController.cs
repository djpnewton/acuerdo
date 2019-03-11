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
        { }

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
            // check market exists
            if (model.Market == null || !_settings.Markets.ContainsKey(model.Market))
                return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Index", model.Market, "Market does not exist"));
            // check amount exists
            if (model.Amount == null)
                return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Trade", model.Market, "Amount not present"));
            // initialize amount vars for further validation
            var amount = decimal.Parse(model.Amount);
            var amountInterval = decimal.Parse(_settings.Markets[model.Market].AmountInterval);
            // check amount is greater then amountInterval
            if (amount < amountInterval)
                return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Trade", model.Market, $"Amount is less then {amountInterval}"));
            // check amonut is a multiple of the amount interval
            if ((amount / amountInterval) % 1 != 0)
                return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Trade", model.Market, $"Amount is not a multiple of {amountInterval}"));
            if (!marketOrder)
            {
                // check price exists
                if (model.Price == null)
                    return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Trade", model.Market, "Price not present"));
                // initialize price vars for further validation
                var price = decimal.Parse(model.Price);
                var priceInterval = decimal.Parse(_settings.Markets[model.Market].PriceInterval);
                // check price is greater then priceInterval
                if (price < priceInterval)
                    return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Trade", model.Market, $"Price is less then {priceInterval}"));
                // check price is a multiple of the price interval
                if ((price / priceInterval) % 1 != 0)
                    return new Tuple<bool, IActionResult>(false, FlashErrorAndRedirect("Trade", model.Market, $"Price is not a multiple of {priceInterval}"));
            }

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
            try
            {
                //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
                var via = new ViaJsonRpc(_settings.AccessHttpUrl);
                var order = via.OrderLimitQuery(user.Exchange.Id, model.Market, model.Side, model.Amount, model.Price, _settings.TakerFeeRate, _settings.MakerFeeRate, "viafront");
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
                    this.FlashSuccess($"Limit Order Failed (balance too small)");
                    return RedirectToAction("Trade", new { market = model.Market });
                }
                throw;
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> MarketOrder(TradeViewModel model)
        {
            var result = ValidateOrderParams(model, marketOrder: true);
            if (!result.Item1)
                return result.Item2;

            var user = await GetUser(required: true);
            try
            {
                //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
                var via = new ViaJsonRpc(_settings.AccessHttpUrl);
                Order order;
                if (_settings.MarketOrderBidAmountMoney)
                    order = via.OrderMarketQuery(user.Exchange.Id, model.Market, model.Side, model.Amount, _settings.TakerFeeRate, "viafront", _settings.MarketOrderBidAmountMoney);
                else
                    order = via.OrderMarketQuery(user.Exchange.Id, model.Market, model.Side, model.Amount, _settings.TakerFeeRate, "viafront");
                // send email: order created
                var amountUnit = _settings.Markets[model.Market].AmountUnit;
                this.FlashSuccess($"Market Order Created ({order.market} - {order.side}, Amount: {order.amount} {amountUnit})");
                return RedirectToAction("Trade", new { market = model.Market });
            }
            catch (ViaJsonException ex)
            {
                if (ex.Err == ViaError.PUT_MARKET__BALANCE_NOT_ENOUGH)
                {
                    this.FlashSuccess($"Market Order Failed (balance too small)");
                    return RedirectToAction("Trade", new { market = model.Market });
                }
                if (ex.Err == ViaError.PUT_MARKET__NO_ENOUGH_TRADER)
                {
                    this.FlashSuccess($"Market Order Failed (insufficient market depth)");
                    return RedirectToAction("Trade", new { market = model.Market });
                }
                throw;
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost] 
        public async Task<IActionResult> CancelOrder(OrdersPendingPartialViewModel model)
        {
            var user = await GetUser(required: true);
            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var order = via.OrderCancelQuery(user.Exchange.Id, model.Market, model.OrderId);
            this.FlashSuccess("Order Cancelled");
            return RedirectToAction("Trade", new { market = model.Market });
        }
    }
}
