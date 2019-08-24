using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.DevApiViewModels;
using viafront3.Data;
using viafront3.Services;
using Newtonsoft.Json;
using via_jsonrpc;

namespace viafront3.Controllers
{
    [Produces("application/json")]
    [Route("api/dev/[action]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class DevApiController : BaseWalletController
    {
        protected readonly IHostingEnvironment _hostingEnvironment;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApiSettings _apiSettings;
        private readonly ITripwire _tripwire;

        public DevApiController(
            IHostingEnvironment hostingEnvironment,
            ILogger<ApiController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            IOptions<ExchangeSettings> settings,
            IOptions<ApiSettings> apiSettings,
            RoleManager<IdentityRole> roleManager,
            IOptions<KycSettings> kycSettings,
            IWalletProvider walletProvider,
            ITripwire tripwire) : base(logger, userManager, context, settings, walletProvider, kycSettings)
        {
            _hostingEnvironment = hostingEnvironment;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _apiSettings = apiSettings.Value;
            _tripwire = tripwire;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_hostingEnvironment.IsDevelopment())
                context.Result = NotFound();
        }

        [HttpGet]
        public ActionResult Check()
        {
            return Ok("check");
        }

        [HttpPost]
        public async Task<ActionResult<DevApiUserCreate>> UserCreate([FromBody] DevApiUserCreate req) 
        {
            var existingUser = await _userManager.FindByEmailAsync(req.Email);
            if (existingUser == null)
            {
                (var result, var user) = await CreateUser(_signInManager, _emailSender, req.Email, req.Email, req.Password, req.SendEmail, false);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create broker user");
                    return BadRequest();
                }
                existingUser = user;
            }
            if (!existingUser.EmailConfirmed && req.EmailConfirmed)
            {
                existingUser.EmailConfirmed = true;
                await PostUserEmailConfirmed(_roleManager, _signInManager, _kycSettings, existingUser);
            }
            return req;
        }

        [HttpPost]
        public async Task<ActionResult<DevApiUserApiKeyCreate>> UserApiKeyCreate([FromBody] DevApiUserApiKeyCreate req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return BadRequest();

            var apikey = _context.ApiKeys.SingleOrDefault(k => k.Key == req.Key && k.ApplicationUserId == user.Id);
            if (apikey == null)
            {
                apikey = new ApiKey { ApplicationUserId = user.Id, Key = req.Key, Name = "DEV", Nonce = 0, Secret = req.Secret };
                _context.ApiKeys.Add(apikey);
            }
            else
            {
                apikey.Secret = req.Secret;
                apikey.Nonce = 0;
                _context.ApiKeys.Update(apikey);
            }
            _context.SaveChanges();
            return req;
        }

        [HttpPost]
        public async Task<ActionResult<DevApiUserFundGive>> UserFundGive([FromBody] DevApiUserFundGive req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return BadRequest();

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            via.BalanceUpdateQuery(user.Exchange.Id, req.Asset, "DEVAPI", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), req.Amount.ToString());
            return req;
        }

        [HttpPost]
        public async Task<ActionResult<DevApiUserFundSet>> UserFundSet([FromBody] DevApiUserFundSet req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return BadRequest();

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(user.Exchange.Id, req.Asset);
            var available = decimal.Parse(balance.Available);
            var change = req.Amount - available;
            if (change != 0)
                via.BalanceUpdateQuery(user.Exchange.Id, req.Asset, "DEVAPI", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), change.ToString());
            return req;
        }

        [HttpPost]
        public async Task<ActionResult<DevApiUserFundGetResult>> UserFundGet([FromBody] DevApiUserFundGet req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return BadRequest();

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(user.Exchange.Id, req.Asset);
            return new DevApiUserFundGetResult { Amount = decimal.Parse(balance.Available) };
        }

        [HttpPost]
        public async Task<ActionResult<DevApiUserFundCheck>> UserFundCheck([FromBody] DevApiUserFundCheck req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return BadRequest();

            //TODO: move this to a ViaRpcProvider in Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(user.Exchange.Id, req.Asset);
            var available = decimal.Parse(balance.Available);
            if (available != req.Amount)
                return BadRequest();
            return req;
        }

        [HttpPost]
        public async Task<ActionResult<Order>> UserLimitOrder([FromBody] DevApiUserLimitOrder req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return BadRequest();

            var side = OrderSide.Any;
            if (req.Side == "buy")
                side = OrderSide.Bid;
            else if (req.Side == "sell")
                side = OrderSide.Ask;
            else
                return BadRequest();

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var order = via.OrderLimitQuery(user.Exchange.Id, req.Market, side, req.Amount.ToString(), req.Price.ToString(), _settings.TakerFeeRate, _settings.MakerFeeRate, "DEVAPI");
            return order;
        }

        [HttpPost]
        public async Task<ActionResult<Order>> UserMarketOrder([FromBody] DevApiUserLimitOrder req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return BadRequest();

            var side = OrderSide.Any;
            if (req.Side == "buy")
                side = OrderSide.Bid;
            else if (req.Side == "sell")
                side = OrderSide.Ask;
            else
                return BadRequest();

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            if (!_settings.MarketOrderBidAmountMoney)
                return via.OrderMarketQuery(user.Exchange.Id, req.Market, side, req.Amount.ToString(), _settings.TakerFeeRate, "DEVAPI", _settings.MarketOrderBidAmountMoney);
            else
                return via.OrderMarketQuery(user.Exchange.Id, req.Market, side, req.Amount.ToString(), _settings.TakerFeeRate, "DEVAPI");
        }

        [HttpPost]
        public ActionResult<DevApiClearAllOrders> ClearAllOrders([FromBody] DevApiClearAllOrders req)
        {
            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            while (true)
            {
                var count = 0;
                var orders = via.OrderBookQuery(req.Market, OrderSide.Ask, 0, 100);
                if (orders.orders.Count() == 0)
                    count++;
                foreach (var order in orders.orders)
                    via.OrderCancelQuery(order.user, order.market, order.id);
                orders = via.OrderBookQuery(req.Market, OrderSide.Bid, 0, 100);
                if (orders.orders.Count() == 0)
                    count++;
                foreach (var order in orders.orders)
                    via.OrderCancelQuery(order.user, order.market, order.id);
                if (count >= 2)
                    break;
            }
            return req;
        }

        [HttpPost]
        public ActionResult<DevApiFeeRates> FeeRatesSet([FromBody] DevApiFeeRates req)
        {
            _settings.MakerFeeRate = req.Maker;
            _settings.TakerFeeRate = req.Taker;
            return req;
        }

        [HttpPost]
        public async Task<IActionResult> ResetTripwire()
        {
            await _tripwire.Reset();
            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<DevApiResetWithdrawalLimit>> ResetWithdrawalLimit(DevApiResetWithdrawalLimit req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return BadRequest();

            _context.Withdrawals.RemoveRange(_context.Withdrawals.Where(w => w.ApplicationUserId == user.Id));
            _context.SaveChanges();
            return req;
        }
    }
}