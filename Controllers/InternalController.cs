using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using viafront3.Models;
using viafront3.Models.InternalViewModels;
using viafront3.Models.TradeViewModels;
using viafront3.Models.WalletViewModels;
using viafront3.Data;
using viafront3.Services;
using via_jsonrpc;
using xchwallet;

namespace viafront3.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("[controller]/[action]")]
    public class InternalController : BaseSettingsController
    {
        private readonly IWebsocketTokens _websocketTokens;
        private readonly ILogger _logger;
        private readonly WalletSettings _walletSettings;
        private readonly IWalletProvider _walletProvider;

        public InternalController(UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<ExchangeSettings> settings,
            IOptions<WalletSettings> walletSettings,
            IWalletProvider walletProvider,
            IWebsocketTokens websocketTokens,
            ILogger<InternalController> logger) : base(userManager, context, settings)
        {
            _walletSettings = walletSettings.Value;
            _walletProvider = walletProvider;
            _websocketTokens = websocketTokens;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public IActionResult Wallets()
        {
            var user = GetUser(required: true).Result;

            var balances = new Dictionary<string, WalletBalance>();
            foreach (var asset in _settings.Assets.Keys)
            {
                try
                {
                    var wallet = _walletProvider.Get(asset);
                    var tags = wallet.GetTags();
                    var balance = new WalletBalance{ Total = 0, Consolidated = 0};
                    foreach (var tag in tags)
                        balance.Total += wallet.GetBalance(tag.Tag);
                    wallet.GetTransactions(_walletSettings.ConsolidatedFundsTag); // this is currently required to update the wallet -> TODO: need to change xchwallet to be more explicit about what updates the wallet
                    balance.Consolidated = wallet.GetBalance(_walletSettings.ConsolidatedFundsTag);
                    balance.Wallet = wallet;
                    balances[asset] = balance;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Could not obtain wallet for asset '{0}'", asset);
                }
            }

            var model = new WalletsViewModel
            {
                User = user,
                AssetSettings = _settings.Assets,
                Balances = balances
            };
            return View(model);
        }

        public IActionResult Users()
        {
            var user = GetUser(required: true).Result;

            var userInfos = (from u in _context.Users
                        join e in _context.Exchange on u.Id equals e.ApplicationUserId
                        let query = (from ur in _context.Set<IdentityUserRole<string>>()
                            where ur.UserId.Equals(u.Id)
                            join r in _context.Roles on ur.RoleId equals r.Id select r.Name)
                        select new UserInfo() {User = u, ExchangeId = e.Id, Roles = query.ToList<string>()})
                        .ToList();
                        

            var model = new UsersViewModel
            {
                User = user,
                UserInfos = userInfos
            };
            return View(model);
        }

        public IActionResult UserInspect(string id)
        {
            var user = GetUser(required: true).Result;
            var userInspect = _userManager.FindByIdAsync(id).Result;
            userInspect.EnsureExchangePresent(_context);
            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balances = via.BalanceQuery(userInspect.Exchange.Id);

            var model = new UserViewModel
            {
                User = user,
                UserInspect = userInspect,
                Balances = new BalancesPartialViewModel{Balances=balances},
                AssetSettings = _settings.Assets,
            };
            return View(model);
        }

        public IActionResult UserInspectTrades(string id, string market)
        {
            var user = GetUser(required: true).Result;
            var userInspect = _userManager.FindByIdAsync(id).Result;
            userInspect.EnsureExchangePresent(_context);

            ViewData["userid"] = id;
            return View(TradeViewModel.Construct(user, userInspect, market, _settings));
        }

        public IActionResult UserInspectBlockchainTxs(string id, string asset)
        {
            var user = GetUser(required: true).Result;

            // get wallet transactions
            var wallet = _walletProvider.Get(asset);
            var txsIn = wallet.GetTransactions(id)
                .Where(t => t.Direction == WalletDirection.Incomming);
            var txsOutOnBehalf = wallet.GetTransactions(_walletProvider.ConsolidatedFundsTag())
                .Where(t => t.TagOnBehalfOf == id);

            ViewData["userid"] = id;
            var model = new UserTransactionsViewModel
            {
                User = user,
                Asset = asset,
                DepositAddress = null,
                AssetSettings = _settings.Assets,
                Wallet = wallet,
                TransactionsIncomming = txsIn,
                TransactionsOutgoing = txsOutOnBehalf
            };
            return View(model);
        }

        public IActionResult UserInspectExchangeBalanceHistory(string id, string asset, int offset=0)
        {
            var user = GetUser(required: true).Result;
            var userInspect = _userManager.FindByIdAsync(id).Result;
            userInspect.EnsureExchangePresent(_context);

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var history = via.BalanceHistoryQuery(userInspect.Exchange.Id, asset, "", 0, 0, offset, 50);

            ViewData["userid"] = id;
            var model = new UserBalanceHistoryViewModel
            {
                User = user,
                Asset = asset,
                AssetSettings = _settings.Assets,
                BalanceHistory = history
            };
            return View(model);
        }

        public async Task<IActionResult> OrdersPending(string userid, string market, int offset=0, int limit=10)
        {
            var user = await GetUser(required: true);
            var userInspect = _userManager.FindByIdAsync(userid).Result;
            userInspect.EnsureExchangePresent(_context);

            ViewData["userid"] = userid;
            return View(OrdersPendingViewModel.Construct(user, userInspect, market, _settings, offset, limit));
        }

        public async Task<IActionResult> BidOrdersCompleted(string userid, string market, int offset=0, int limit=10)
        {
            var user = await GetUser(required: true);
            var userInspect = _userManager.FindByIdAsync(userid).Result;
            userInspect.EnsureExchangePresent(_context);

            ViewData["userid"] = userid;
            return View(OrdersCompletedViewModel.Construct(user, userInspect, market, OrderSide.Bid, _settings, offset, limit));
        }

        public async Task<IActionResult> AskOrdersCompleted(string userid, string market, int offset=0, int limit=10)
        {
            var user = await GetUser(required: true);
            var userInspect = _userManager.FindByIdAsync(userid).Result;
            userInspect.EnsureExchangePresent(_context);

            ViewData["userid"] = userid;
            return View(OrdersCompletedViewModel.Construct(user, user, market, OrderSide.Ask, _settings, offset, limit));
        }

        [AllowAnonymous]
        [Produces("application/json")]
        public IActionResult WebsocketAuth()
        {
            var ip = GetRequestIP();
            if (ip != _settings.AccessWsIp)
                return Unauthorized();
            StringValues token;
            if (!Request.Headers.TryGetValue("Authorization", out token))
                return BadRequest();
            var wsToken = _websocketTokens.Remove(token);
            if (wsToken == null)
                return Unauthorized();
            return Ok(new { code = 0, message = (string)null, data = new { user_id = wsToken.ExchangeUserId}});
        }

        public IActionResult TestWebsocket()
        {
            var user = GetUser(required: true).Result;

            var model = new TestWebsocketViewModel
            {
                User = user,
                WebsocketToken = _websocketTokens.NewToken(user.Exchange.Id),
                WebsocketUrl = _settings.AccessWsUrl
            };

            return View(model);
        }
    }
}
