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
                    // get the user manager & email sender
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    foreach (var asset in walletSettings.ChainAssetSettings.Keys)
                    {
                        // get wallet
                        var wallet = walletProvider.GetChain(asset);
                        var assetSettings = walletProvider.ChainAssetSettings(asset);

                        // update wallet
                        var dbtx = wallet.BeginDbTransaction();
                        wallet.UpdateFromBlockchain(dbtx);
                        wallet.Save();
                        dbtx.Commit();

                        // update for each user
                        foreach (var user in userManager.Users)
                        {
                            var addrs = wallet.GetAddresses(user.Id);
                            if (addrs != null && addrs.Any())
                            {
                                var addr = addrs.First();
                                var task = Utils.CheckAddressIncommingTxsAndUpdateWalletAndExchangeBalance(emailSender, settings, asset, wallet, assetSettings, user, addr);
                                task.Wait();
                                var addrTxs = task.Result;
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

                            _logger.LogInformation($"SpendCode: {spend.SpendCode}, Date: {spend.Date}, Amount: {spend.Amount}, To: {spend.To}, State: {spend.State}");
                            // process withdrawal
                            _logger.LogInformation($"Actioning pending spend: {spend.SpendCode}, asset: {asset}");
                            var err = wallet.PendingSpendAction(spend.SpendCode, assetSettings.FeeMax, assetSettings.FeeUnit, out IEnumerable<WalletTx> wtxs);
                            _logger.LogInformation($"Result: {err}");

                            // save wallet
                            wallet.Save();
                            _logger.LogInformation($"Saved {asset} wallet");

                            if (err == WalletError.Success)
                            {
                                foreach (var wtx in wtxs)
                                {
                                    // get user
                                    var task = userManager.FindByIdAsync(wtx.TagOnBehalfOf.Tag);
                                    task.Wait();
                                    var user = task.Result;
                                    System.Diagnostics.Debug.Assert(user != null);

                                    // send email
                                    emailSender.SendEmailChainWithdrawalConfirmedAsync(user.Email, asset, wallet.AmountToString(wtx.AmountInputs() - wtx.ChainTx.Fee), wtx.ChainTx.TxId).Wait();
                                    _logger.LogInformation($"Sent email to {user.Email}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
