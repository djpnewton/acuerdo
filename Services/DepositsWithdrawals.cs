using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using xchwallet;
using viafront3.Data;
using viafront3.Models;

namespace viafront3.Services
{
    public interface IDepositsWithdrawals
    {
        void ProcessChainDeposits();
        void ProcessChainWithdrawals();
        void ProcessFiatWithdrawals();
    }

    public class DepositsWithdrawals : IDepositsWithdrawals
    {
        static Object lockObj = new Object();

        private readonly IServiceProvider _services;
        private readonly ITripwire _tripwire;
        private readonly ILogger _logger;

        public DepositsWithdrawals(IServiceProvider services, ITripwire tripwire, ILogger<DepositsWithdrawals> logger)
        {
            _services = services;
            _tripwire = tripwire;
            _logger = logger;
        }

        public void ProcessChainDeposits()
        {
            lock (lockObj)
            {
                using (var scope = _services.CreateScope())
                {
                    // get exchange settings
                    var settings = scope.ServiceProvider.GetRequiredService<IOptions<ExchangeSettings>>().Value;
                    var walletProvider = scope.ServiceProvider.GetRequiredService<IWalletProvider>();
                    var walletSettings = scope.ServiceProvider.GetRequiredService<IOptions<WalletSettings>>().Value;
                    var apiSettings = scope.ServiceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;
                    // get the user manager & email sender
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    foreach (var asset in walletSettings.ChainAssetSettings.Keys)
                    {
                        // get wallet
                        var wallet = walletProvider.GetChain(asset);
                        var assetSettings = walletProvider.ChainAssetSettings(asset);

                        // update for each user
                        foreach (var user in userManager.Users)
                        {
                            // skip broker user because his chain deposits are handled in the Broker class
                            if (user.UserName == apiSettings.Broker.BrokerTag)
                                continue;

                            var addrs = wallet.GetAddresses(user.Id);
                            if (addrs != null && addrs.Any())
                            {
                                var addr = addrs.First();
                                var addrTxs = Utils.CheckAddressIncommingTxsAndUpdateWalletAndExchangeBalance(emailSender, settings, asset, wallet, assetSettings, user, addr).GetAwaiter().GetResult();
                                foreach (var tx in addrTxs.NewlySeenTxs)
                                    _logger.LogInformation($"{user.Email}: new tx: {tx}");
                                foreach (var tx in addrTxs.JustAckedTxs)
                                    _logger.LogInformation($"{user.Email}: confirmed tx: {tx}");
                            }
                        }
                    }
                }
            }
        }

        public void ProcessChainWithdrawals()
        {
            if (!_tripwire.WithdrawalsEnabled())
            {
                _logger.LogError("Tripwire tripped, exiting ProcessChainWithdrawls()");
                return;
            }
            lock (lockObj)
            {
                // if lockfile exists exit early
                var lockFile = new LockFile(_logger, "chain_withdrawals");
                if (lockFile.IsPresent())
                {
                    _logger.LogError($"lockfile ('{lockFile.MkPath()}) exists");
                    return;
                }

                using (var scope = _services.CreateScope())
                {
                    var settings = scope.ServiceProvider.GetRequiredService<IOptions<WalletSettings>>().Value;
                    var walletProvider = scope.ServiceProvider.GetRequiredService<IWalletProvider>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    foreach (var asset in settings.ChainAssetSettings.Keys)
                    {
                        // get wallet
                        var wallet = walletProvider.GetChain(asset);
                        var assetSettings = walletProvider.ChainAssetSettings(asset);
                        // get pending spends
                        var spends = wallet.PendingSpendsGet(null, new PendingSpendState[] { PendingSpendState.Pending, PendingSpendState.Error });
                        foreach (var spend in spends)
                        {
                            // recheck tripwire
                            if (!_tripwire.WithdrawalsEnabled())
                            {
                                _logger.LogError("Tripwire tripped, exiting ProcessChainWithdrawls()");
                                return;
                            }

                            // check and create lockfile to make sure we cant send withdrawals twice
                            if (lockFile.IsPresent())
                            {
                                _logger.LogError($"lockfile ('{lockFile.MkPath()}) exists");
                                return;
                            }
                            var contents = $"Pending spend: {spend.SpendCode}, {spend.State}, {spend.Date}, {spend.Amount} {asset} cents";
                            if (!lockFile.CreateIfNotPresent(contents))
                            {
                                _logger.LogError($"failed to create lockfile ('{lockFile.MkPath()})");
                                return;
                            }

                            _logger.LogInformation($"SpendCode: {spend.SpendCode}, Date: {spend.Date}, Amount: {spend.Amount}, To: {spend.To}, State: {spend.State}");
                            // process withdrawal
                            _logger.LogInformation($"Actioning pending spend: {spend.SpendCode}, asset: {asset}");
                            var err = wallet.PendingSpendAction(spend.SpendCode, assetSettings.FeeMax, assetSettings.FeeUnit, out IEnumerable<WalletTx> wtxs);
                            _logger.LogInformation($"Result: {err}");

                            // save wallet
                            wallet.Save();
                            _logger.LogInformation($"Saved {asset} wallet");

                            // remove lock file now that we have saved wallet status
                            if (!lockFile.RemoveIfPresent())
                                _logger.LogError($"Failed to remove lockfile ({lockFile.MkPath()})");

                            if (err == WalletError.Success)
                            {
                                foreach (var wtx in wtxs)
                                {
                                    // get user
                                    System.Diagnostics.Debug.Assert(spend.TagFor != null);
                                    var user = userManager.FindByIdAsync(spend.TagFor.Tag).GetAwaiter().GetResult();
                                    System.Diagnostics.Debug.Assert(user != null);

                                    // send email
                                    emailSender.SendEmailChainWithdrawalConfirmedAsync(user.Email, asset, wallet.AmountToString(wtx.AmountInputs() - wtx.ChainTx.Fee), wtx.ChainTx.TxId).GetAwaiter().GetResult();
                                    _logger.LogInformation($"Sent email to {user.Email}");
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ProcessFiatWithdrawals()
        {
            if (!_tripwire.WithdrawalsEnabled())
            {
                _logger.LogError("Tripwire tripped, exiting ProcessFiatWithdrawls()");
                return;
            }
            lock (lockObj)
            {
                // if lockfile exists exit early
                var lockFile = new LockFile(_logger, "fiat_withdrawals");
                if (lockFile.IsPresent())
                {
                    _logger.LogError($"lockfile ('{lockFile.MkPath()}) exists");
                    return;
                }

                using (var scope = _services.CreateScope())
                {
                    var settings = scope.ServiceProvider.GetRequiredService<IOptions<WalletSettings>>().Value;
                    var fiatSettings = scope.ServiceProvider.GetRequiredService<IOptions<FiatProcessorSettings>>().Value;
                    var exchangeSettings = scope.ServiceProvider.GetRequiredService<IOptions<ExchangeSettings>>().Value;
                    var apiSettings = scope.ServiceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;
                    var walletProvider = scope.ServiceProvider.GetRequiredService<IWalletProvider>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    foreach (var asset in settings.BankAccounts.Keys)
                    {
                        if (!fiatSettings.PayoutsEnabled || !fiatSettings.PayoutsAssets.Contains(asset))
                        {
                            // exit as there is no fiat payment server
                            continue;
                        }

                        // get wallet
                        var wallet = walletProvider.GetFiat(asset);
                        // get pending withdrawals
                        var withdrawals = wallet.GetPendingWithdrawals();
                        foreach (var withdrawal in withdrawals)
                        {
                            // recheck tripwire
                            if (!_tripwire.WithdrawalsEnabled())
                            {
                                _logger.LogError("Tripwire tripped, exiting ProcessFiatWithdrawls()");
                                return;
                            }

                            // check and create lockfile to make sure we cant send withdrawals twice
                            if (lockFile.IsPresent())
                            {
                                _logger.LogError($"lockfile ('{lockFile.MkPath()}) exists");
                                return;
                            }
                            var contents = $"Pending fiat withdrawal: {withdrawal.DepositCode}, {withdrawal.Date}, {withdrawal.Amount} {asset} cents";
                            if (!lockFile.CreateIfNotPresent(contents))
                            {
                                _logger.LogError($"failed to create lockfile ('{lockFile.MkPath()})");
                                return;
                            }

                            _logger.LogInformation($"Pending Fiat Withdrawal: {withdrawal.DepositCode}, Date: {withdrawal.Date}, Amount: {withdrawal.Amount}, To: {withdrawal.AccountNumber}");
                            // process withdrawal

                            var payoutReq = RestUtils.GetFiatPayoutRequest(fiatSettings, withdrawal.DepositCode);
                            if (payoutReq == null)
                            {
                                var user = userManager.FindByIdAsync(withdrawal.Tag.Tag).GetAwaiter().GetResult();
                                if (user != null && user.UserName == apiSettings.Broker.BrokerTag)
                                {
                                    user = null;
                                    var bow = context.BrokerOrderFiatWithdrawals.SingleOrDefault(o => o.DepositCode == withdrawal.DepositCode);
                                    if (bow != null)
                                    {
                                        var order = context.BrokerOrders.SingleOrDefault(o => o.Id == bow.BrokerOrderId);
                                        if (order != null)
                                            user = userManager.FindByIdAsync(order.ApplicationUserId).GetAwaiter().GetResult();
                                    }
                                }
                                if (user == null)
                                {
                                    _logger.LogError($"failed to find user for withdrawal ('{withdrawal.DepositCode})");
                                    continue;
                                }
                                payoutReq = RestUtils.CreateFiatPayoutRequest(_logger, exchangeSettings, fiatSettings, withdrawal.DepositCode, asset, wallet.AmountToDecimal(withdrawal.Amount), withdrawal.AccountNumber, user.Email);
                                if (payoutReq == null)
                                    _logger.LogError($"fiat payout request creation failed ({withdrawal.DepositCode})");
                            }
                            else if (payoutReq.Status.ToLower() == viafront3.Models.ApiViewModels.ApiRequestStatus.Completed.ToString().ToLower())
                            {
                                wallet.UpdateWithdrawal(payoutReq.Token, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Convert.ToInt64(payoutReq.Amount), "");
                                _logger.LogInformation($"Payout confirmed for withdrawal {withdrawal.DepositCode}");
                            }

                            // save wallet
                            wallet.Save();
                            _logger.LogInformation($"Saved {asset} wallet");

                            // remove lock file now that we have saved wallet status
                            if (!lockFile.RemoveIfPresent())
                                _logger.LogError($"Failed to remove lockfile ({lockFile.MkPath()})");

                            // send email if withdrawal completed
                            if (wallet.GetTx(withdrawal.DepositCode).BankTx != null)
                            {
                                // get user
                                System.Diagnostics.Debug.Assert(withdrawal.Tag != null);
                                var user = userManager.FindByIdAsync(withdrawal.Tag.Tag).GetAwaiter().GetResult();
                                System.Diagnostics.Debug.Assert(user != null);

                                // send email
                                emailSender.SendEmailFiatWithdrawalConfirmedAsync(user.Email, asset, wallet.AmountToString(withdrawal.Amount), withdrawal.DepositCode).GetAwaiter().GetResult();
                                _logger.LogInformation($"Sent email to {user.Email}");
                            }
                        }
                    }
                }
            }
        }
    }
}
