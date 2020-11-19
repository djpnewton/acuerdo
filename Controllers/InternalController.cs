﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    [Authorize(Roles = Utils.AdminRole)]
    [Route("[controller]/[action]")]
    public class InternalController : BaseWalletController
    {
        private readonly IWebsocketTokens _websocketTokens;
        private readonly WalletSettings _walletSettings;
        private readonly ITripwire _tripwire;
        private readonly TripwireSettings _tripwireSettings;

        public InternalController(
            ILogger<InternalController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<ExchangeSettings> settings,
            IOptions<WalletSettings> walletSettings,
            IWalletProvider walletProvider,
            IOptions<KycSettings> kycSettings,
            IWebsocketTokens websocketTokens,
            ITripwire tripwire,
            IOptions<TripwireSettings> tripwireSettings) : base(logger, userManager, context, settings, walletProvider, kycSettings)
        {
            _walletSettings = walletSettings.Value;
            _websocketTokens = websocketTokens;
            _tripwire = tripwire;
            _tripwireSettings = tripwireSettings.Value;
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public IActionResult Wallets(bool update=false)
        {
            var user = GetUser(required: true).Result;

            var chainBalances = new Dictionary<string, ChainWalletBalance>();
            var fiatBalances = new Dictionary<string, FiatWalletBalance>();
            foreach (var asset in _settings.Assets.Keys)
            {
                try
                {
                    if (_walletProvider.IsChain(asset))
                    {
                        if (update)
                            _walletProvider.UpdateBlockchainWallet(asset);

                        var wallet = _walletProvider.GetFastChain(asset);
                        var balance = new ChainWalletBalance{ Total = 0, Consolidated = 0};
                        balance.Total = wallet.GetBalance();
                        balance.Consolidated = wallet.GetBalance(_walletSettings.ConsolidatedFundsTag);
                        balance.Wallet = _walletProvider.GetChain(asset);
                        chainBalances[asset] = balance;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not obtain blockchain wallet for asset '{0}'", asset);
                }
                try
                {
                    if (_walletProvider.IsFiat(asset))
                    {
                        var wallet = _walletProvider.GetFastFiat(asset);
                        var balance = new FiatWalletBalance{ Total = 0 };
                        balance.Total = wallet.GetBalance();
                        balance.Wallet = _walletProvider.GetFiat(asset);
                        fiatBalances[asset] = balance;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not obtain fiat wallet for asset '{0}'", asset);
                }
            }

            var model = new WalletsViewModel
            {
                User = user,
                AssetSettings = _settings.Assets,
                ChainBalances = chainBalances,
                FiatBalances = fiatBalances
            };
            return View(model);
        }

        public IActionResult WalletPendingSpends(string asset, int offset=0, int limit=10, bool onlyIncomplete=true)
        {
            var user = GetUser(required: true).Result;

            var wallet = _walletProvider.GetChain(asset);
            var spends = wallet.PendingSpendsGet();
            if (onlyIncomplete)
                spends = spends.Where(s => s.State != PendingSpendState.Complete);
            spends = spends.OrderByDescending(t => t.Date);

            var model = new WalletPendingSpendsViewModel
            {
                User = user,
                Asset = asset,
                AssetSettings = _settings.Assets[asset],
                Offset = offset,
                Limit = limit,
                Count = spends.Count(),
                OnlyIncomplete = onlyIncomplete,
                PendingSpends = spends.Skip(offset).Take(limit),
                Wallet = wallet,
            };
            return View(model);
        }

        public IActionResult FiatWalletPendingTxs(string asset, int offset = 0, int limit = 10, bool onlyIncomplete = true, WalletDirection? direction = null)
        {
            var user = GetUser(required: true).Result;

            var wallet = _walletProvider.GetFiat(asset);
            IEnumerable<FiatWalletTx> pendingTxs = wallet.GetTransactions().ToList();
            if (onlyIncomplete)
                pendingTxs = pendingTxs.Where(t => t.BankTx == null);
            if (direction.HasValue)
                pendingTxs = pendingTxs.Where(t => t.Direction == direction.Value);
            pendingTxs = pendingTxs.OrderByDescending(t => t.Date);

            var model = new FiatWalletPendingTxsViewModel
            {
                User = user,
                Asset = asset,
                AssetSettings = _settings.Assets[asset],
                Offset = offset,
                Limit = limit,
                Count = pendingTxs.Count(),
                OnlyIncomplete = onlyIncomplete,
                Direction = direction,
                PendingTxs = pendingTxs.Skip(offset).Take(limit),
                Wallet = wallet,
            };
            return View(model);
        }

        public IActionResult Users(int offset=0, int limit=20, string role=null, string emailSearch=null, string nameSearch=null)
        {
            var user = GetUser(required: true).Result;

            var userInfos = UserInfo.Query(_context, role, emailSearch, nameSearch);
            var model = new UsersViewModel
            {
                User = user,
                UserInfos = userInfos.Skip(offset).Take(limit).ToList(),
                Offset = offset,
                Limit = limit,
                Count = userInfos.Count(),
                Role = role,
                EmailSearch = emailSearch,
                NameSearch = nameSearch,
            };
            return View(model);
        }

        public IActionResult UserExchangeCreate(UsersViewModel model)
        {
            var userInspect = _userManager.FindByIdAsync(model.UserId).Result;
            if (userInspect.EnsureExchangePresent(_context))
                _context.SaveChanges();
            this.FlashSuccess($"User exchange created for '{userInspect.Email}'");
            return RedirectToAction("Users");
        }

        public IActionResult UserInspect(string id)
        {
            var user = GetUser(required: true).Result;
            var userInspect = _userManager.FindByIdAsync(id).Result;
            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balances = Utils.GetUsedBalances(_settings, via, userInspect.Exchange);

            // get kyc level
            var level = userInspect.Kyc != null ? userInspect.Kyc.Level : 0;
            KycLevel kycLevel = null;
            if (level < _kycSettings.Levels.Count())
                kycLevel = _kycSettings.Levels[level];
            // get user kyc request
            string kycRequestUrl = null;
            var kycRequest = _context.KycRequests.Where(r => r.ApplicationUserId == userInspect.Id).OrderByDescending(r => r.Date).FirstOrDefault();
            if (kycRequest != null)
                kycRequestUrl = $"{_kycSettings.KycServerUrl}/request/{kycRequest.Token}";

            var model = new UserViewModel
            {
                User = user,
                UserInspect = userInspect,
                Balances = new BalancesPartialViewModel { Balances = balances },
                KycLevel = kycLevel,
                KycRequestUrl = kycRequestUrl,
                KycSettings = _kycSettings,
                AssetSettings = _settings.Assets,
            };
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult UserInspectKycSet(string id, int level)
        {
            var user = GetUser(required: true).Result;
            var userInspect = _userManager.FindByIdAsync(id).Result;
            if (level >= 0 && level < _kycSettings.Levels.Count())
            {
                if (userInspect.Kyc == null)
                {
                    userInspect.Kyc = new Kyc { ApplicationUserId = userInspect.Id, Level = level };
                    _context.Add(userInspect.Kyc);
                }
                else
                {
                    userInspect.Kyc.Level = level;
                    _context.Update(userInspect.Kyc);
                }
                _context.SaveChanges();
            }
            return RedirectToAction("UserInspect", new { id = userInspect.Id });
        }

        public IActionResult UserInspectTrades(string id, string market)
        {
            var user = GetUser(required: true).Result;
            var userInspect = _userManager.FindByIdAsync(id).Result;

            ViewData["userid"] = id;
            return View(TradeViewModel.Construct(user, userInspect, market, null, null, null, _settings));
        }

        public IActionResult UserInspectWalletTxs(string id, string asset, int inOffset=0, int inLimit=10, int outOffset=0, int outLimit=10)
        {
            var user = GetUser(required: true).Result;

            if (_walletProvider.IsFiat(asset))
                return RedirectToAction("UserInspectFiatWalletTxs", new {id=id, asset=asset});

            // get wallet transactions
            var wallet = _walletProvider.GetChain(asset);
            var txsIn = wallet.GetTransactions(id)
                .Where(t => t.Direction == WalletDirection.Incomming).OrderByDescending(t => t.ChainTx.Date);
            var txsOutForUser = wallet.GetTransactions(_walletProvider.ConsolidatedFundsTag(), id).OrderByDescending(t => t.ChainTx.Date);

            ViewData["userid"] = id;
            var model = new UserTransactionsViewModel
            {
                User = user,
                Asset = asset,
                DepositAddress = null,
                ChainAssetSettings = _walletProvider.ChainAssetSettings(asset),
                AssetSettings = _settings.Assets[asset],
                Wallet = wallet,
                TransactionsIncomming = txsIn.Skip(inOffset).Take(inLimit),
                TxsIncommingOffset = inOffset,
                TxsIncommingLimit = inLimit,
                TxsIncommingCount = txsIn.Count(),
                TransactionsOutgoing = txsOutForUser.Skip(outOffset).Take(outLimit),
                TxsOutgoingOffset = outOffset,
                TxsOutgoingLimit = outLimit,
                TxsOutgoingCount = txsOutForUser.Count(),
            };
            return View(model);
        }

        public IActionResult UserInspectFiatWalletTxs(string id, string asset, int offset=0, int limit=20)
        {
            var user = GetUser(required: true).Result;

            // get wallet transactions
            var wallet = _walletProvider.GetFiat(asset);
            var txs = wallet.GetTransactions(id).OrderByDescending(t => t.Date);

            ViewData["userid"] = id;
            var model = new FiatTransactionsViewModel
            {
                User = user,
                Asset = asset,
                AssetSettings = _settings.Assets,
                Wallet = wallet,
                Transactions = txs.Skip(offset).Take(limit),
                TxsOffset = offset,
                TxsLimit = limit,
                TxsCount = txs.Count(),
            };
            return View(model);
        }

        public IActionResult UserInspectExchangeBalanceHistory(string id, string asset, int offset=0, int limit=20)
        {
            var user = GetUser(required: true).Result;
            var userInspect = _userManager.FindByIdAsync(id).Result;

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var history = via.BalanceHistoryQuery(userInspect.Exchange.Id, asset, "", 0, 0, offset, limit);

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

            ViewData["userid"] = userid;
            return View(OrdersPendingViewModel.Construct(user, userInspect, market, _settings, offset, limit));
        }

        public async Task<IActionResult> BidOrdersCompleted(string userid, string market, int offset=0, int limit=10)
        {
            var user = await GetUser(required: true);
            var userInspect = _userManager.FindByIdAsync(userid).Result;

            ViewData["userid"] = userid;
            return View(OrdersCompletedViewModel.Construct(user, userInspect, market, OrderSide.Bid, _settings, offset, limit));
        }

        public async Task<IActionResult> AskOrdersCompleted(string userid, string market, int offset=0, int limit=10)
        {
            var user = await GetUser(required: true);
            var userInspect = _userManager.FindByIdAsync(userid).Result;
            
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
                WebsocketUrl = _settings.WebsocketUrl,
            };

            return View(model);
        }

        public IActionResult Tripwire()
        {
            var user = GetUser(required: true).Result;

            var stats = _tripwire.Stats(_tripwireSettings, _context);
            var model = new TripwireViewModel
            {
                User = user,
                Tripwire = _tripwire,
                Settings = _tripwireSettings,
                Stats = stats,
            };
            return View(model);
        }
    }
}
