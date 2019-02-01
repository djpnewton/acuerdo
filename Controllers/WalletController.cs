using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Numerics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.WalletViewModels;
using viafront3.Services;
using viafront3.Data;
using via_jsonrpc;
using xchwallet;

namespace viafront3.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class WalletController : BaseSettingsController
    {
        private readonly ILogger _logger;
        private readonly IWalletProvider _walletProvider;

        public WalletController(
          UserManager<ApplicationUser> userManager,
          ILogger<ManageController> logger,
          ApplicationDbContext context,
          IOptions<ExchangeSettings> settings,
          IWalletProvider walletProvider) : base(userManager, context, settings)
        {
            _logger = logger;
            _walletProvider = walletProvider;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetUser(required: true);

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balances = via.BalanceQuery(user.Exchange.Id);

            var model = new BalanceViewModel
            {
                User = user,
                AssetSettings = _settings.Assets,
                Balances = balances
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Deposits()
        {
            var user = await GetUser(required: true);
            var model = new BaseViewModel
            {
                User = user
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Deposit(string asset)
        {
            var user = await GetUser(required: true);

            var wallet = _walletProvider.Get(asset);
            var addrs = wallet.GetAddresses(user.Id);
            WalletAddr addr = null;
            if (addrs.Any())
                addr = addrs.First();
            else
            {
                addr = wallet.NewAddress(user.Id);
                wallet.Save();
            }

            var model = new DepositViewModel
            {
                User = user,
                Asset = asset,
                DepositAddress = addr.Address,
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> TransactionCheck(string asset, string address)
        {
            var user = await GetUser(required: true);

            // get wallet address
            var wallet = _walletProvider.Get(asset);
            var addrs = wallet.GetAddresses(user.Id);
            WalletAddr addr = null;
            if (addrs.Any())
                addr = addrs.First();
            else
                addr = wallet.NewAddress(user.Id);

            // update wallet from blockchain
            wallet.UpdateFromBlockchain();

            // get wallet transactions
            var txs = wallet.GetAddrTransactions(addr.Address)
                .Where(t => t.Direction == WalletDirection.Incomming);;
            var unackedTxs = wallet.GetAddrUnacknowledgedTransactions(addr.Address)
                .Where(t => t.Direction == WalletDirection.Incomming);;
            BigInteger newDeposits = 0;
            foreach (var tx in unackedTxs)
                newDeposits += tx.ChainTx.Amount;
            var newDepositsHuman = wallet.AmountToString(newDeposits);

            // ack txs and save wallet
            wallet.AcknowledgeTransactions(user.Id, unackedTxs);
            wallet.Save();

            // register new deposits with the exchange backend
            var via = new ViaJsonRpc(_settings.AccessHttpUrl); //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            foreach (var tx in unackedTxs)
            {
                var amount = wallet.AmountToString(tx.ChainTx.Amount);
                var source = new Dictionary<string, object>();
                source["txid"] = tx.ChainTx.TxId;
                var businessId = wallet.GetNextTxWalletId(user.Id);
                wallet.SetTxWalletId(user.Id, tx.ChainTx.TxId, businessId);
                wallet.Save();
                via.BalanceUpdateQuery(user.Exchange.Id, asset, "deposit", businessId, amount, source);
            } 

            var model = new TransactionCheckViewModel
            {
                User = user,
                Asset = asset,
                AssetSettings = _settings.Assets,
                DepositAddress = addr.Address,
                Wallet = wallet,
                TransactionsIncomming = txs,
                NewTransactionsIncomming = unackedTxs,
                NewDeposits = newDeposits,
                NewDepositsHuman = newDepositsHuman,
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Withdrawals()
        {
            var user = await GetUser(required: true);
            var model = new BaseViewModel
            {
                User = user
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Withdraw(string asset)
        {
            var user = await GetUser(required: true);

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(user.Exchange.Id, asset);

            var model = new WithdrawViewModel
            {
                User = user,
                Asset = asset,
                BalanceAvailable = balance.Available,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(WithdrawViewModel model)
        {
            var user = await GetUser(required: true);

            //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(user.Exchange.Id, model.Asset);
            model.BalanceAvailable = balance.Available;

            if (ModelState.IsValid)
            {
                var wallet = _walletProvider.Get(model.Asset);

                // validate amount
                var amountInt = wallet.StringToAmount(model.Amount.ToString());
                var availableInt = wallet.StringToAmount(balance.Available);
                if (amountInt > availableInt)
                {
                    this.FlashError("Amount must be less then or equal to available balance");
                    return View(model);
                }
                if (amountInt <= 0)
                {
                    this.FlashError("Amount must be greather then or equal to 0");
                    return View(model);
                }

                // validate address
                if (!wallet.ValidateAddress(model.WithdrawalAddress))
                {
                    this.FlashError("Withdrawal address is not valid");
                    return View(model);
                }

                var consolidatedFundsTag = _walletProvider.ConsolidatedFundsTag();
                var businessId = wallet.GetNextTxWalletId(consolidatedFundsTag);

                // register withdrawal with the exchange backend
                var negativeAmount = -model.Amount;
                try
                {
                    via.BalanceUpdateQuery(user.Exchange.Id, model.Asset, "withdraw", businessId, negativeAmount.ToString(), null);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Failed to update (withdraw) user balance (xch id: {0}, asset: {1}, businessId: {2}, amount {3}",
                        user.Exchange.Id, model.Asset, businessId, negativeAmount);
                    throw;
                }
                
                // send funds and save wallet
                IEnumerable<string> txids = null;
                var assetSettings = _walletProvider.ChainAssetSettings(model.Asset);
                var res = wallet.Spend(consolidatedFundsTag, consolidatedFundsTag,
                    model.WithdrawalAddress, amountInt, assetSettings.FeeMax, assetSettings.FeeUnit, out txids);
                if (res != WalletError.Success)
                {
                    _logger.LogError("Failed to withdraw funds (wallet error: {0}, asset: {1}, address: {2}, amount: {3}, businessId: {4}",
                        res, model.Asset, model.WithdrawalAddress, amountInt, businessId);
                    this.FlashError(string.Format("Failed to withdraw funds ({0})", res));
                }
                else
                    this.FlashSuccess(string.Format("{0} {1} withdrawn to {2}", model.Amount, model.Asset, model.WithdrawalAddress));
                wallet.SetTxWalletId(consolidatedFundsTag, txids, businessId);
                wallet.SetTagOnBehalfOf(consolidatedFundsTag, txids, user.Id);
                wallet.Save();

                return View(model);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }
}
