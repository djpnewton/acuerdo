using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using viafront3.Models;
using viafront3.Models.ApiViewModels;
using viafront3.Services;
using viafront3.Data;
using xchwallet;
using via_jsonrpc;

namespace viafront3
{
    public static class ConsoleTasks
    {
        public static async Task CreateRoles(IServiceProvider serviceProvider)
        {
            // create roles if they dont exist
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { Utils.AdminRole, Utils.FinanceRole, Utils.EmailConfirmedRole };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    Console.WriteLine($"Creating {roleName}");
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
                else
                    Console.WriteLine($"{roleName} exists");
            }
        }

        public static async Task GiveUserRole(IServiceProvider serviceProvider, string email, string role)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var _role = await roleManager.FindByNameAsync(role);
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            if (!await userManager.IsInRoleAsync(user, _role.Name))
                await userManager.AddToRoleAsync(user, _role.Name);
        }

        public static async Task<bool> ChangeUserEmail(IServiceProvider serviceProvider, string oldEmail, string newEmail)
        {
            Console.WriteLine($"Changing {oldEmail} to {newEmail}...");
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(oldEmail);
            if (user == null)
            {
                Console.WriteLine($"Could not find user {oldEmail}");
                return false;
            }
            
            // update user name
            user.UserName = newEmail;
            user.Email = newEmail;
            // change email
            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                Console.WriteLine($"Success");
                return true;
            }

            Console.WriteLine($"Could not change email to {newEmail}");
            return false;
        }

        public static async Task<WalletError> ConsolidateWallet(IServiceProvider serviceProvider, string asset, IEnumerable<string> userEmails, bool allUsers)
        {
            // check for conflicting options
            if (userEmails.Count() > 0 && allUsers)
            {
                Console.WriteLine("ERROR: use either a list of emails *OR* the all users flag");
                return WalletError.Cancelled;
            }
            // create our logger
            var factory = new LoggerFactory().AddConsole(LogLevel.Debug);
            var _logger = factory.CreateLogger("main");
            // get the user manager
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            // get the wallet
            asset = asset.ToUpper();
            var walletProvider = serviceProvider.GetRequiredService<IWalletProvider>();
            var wallet = walletProvider.GetChain(asset);
            var assetSettings = walletProvider.ChainAssetSettings(asset);
            // ensure consolidate tag exists
            if (!wallet.HasTag(walletProvider.ConsolidatedFundsTag()))
            {
                wallet.NewTag(walletProvider.ConsolidatedFundsTag());
                wallet.Save();
            }
            // get user ids list
            var userIds = new List<string>();
            Console.WriteLine("Consolidating asset '{0}' to tag '{1}'", asset, walletProvider.ConsolidatedFundsTag());
            if (allUsers)
            {
                Console.WriteLine("  from all users");
                foreach (var user in userManager.Users)
                    userIds.Add(user.Id);
            }
            else
                foreach (var email in userEmails)
                {
                    var emailTrimmed = email.Trim();
                    var user = await userManager.FindByEmailAsync(emailTrimmed);
                    if (user != null)
                    {
                        Console.WriteLine("  from user: {0} - {1}", emailTrimmed, user.Id);
                        userIds.Add(user.Id);
                    }
                }
            Console.WriteLine();
            var dbtx = wallet.BeginDbTransaction();
            // update wallet from the blockchain
            Console.WriteLine("Updating txs from blockchain..");
            wallet.UpdateFromBlockchain(dbtx);
            Console.WriteLine("Saving wallet..");
            wallet.Save();
            Console.WriteLine();
            // Get amount that will be consolidated
            var minConfs = assetSettings.MinConf;
            var balance = wallet.GetBalance(userIds, minConfs);
            Console.WriteLine($"Users available balance: {balance} ({wallet.AmountToString(balance)} {wallet.Type()}, {minConfs} Confirmations)");
            Console.WriteLine("Do you want to continue? (y/N)");
            string input = Console.ReadLine();
            if (input.Count() < 1 || input[0] != 'y')
                return WalletError.Cancelled;
            // Create and broadcast transactions
            Console.WriteLine("Creating consolidations txs..");
            IEnumerable<WalletTx> wtxs;
            var res = wallet.Consolidate(userIds, walletProvider.ConsolidatedFundsTag(), assetSettings.FeeMax, assetSettings.FeeUnit, out wtxs, minConfs);
            Console.WriteLine(res);
            foreach (var tx in wtxs)
                Console.WriteLine(tx.ChainTx.TxId);
            Console.WriteLine("Saving wallet..");
            wallet.Save();
            dbtx.Commit();
            return res;
        }

        static async Task ProcessFiat(IServiceProvider serviceProvider, bool isDeposit, string asset, string depositCode, long date, decimal amount, string bankMetadata)
        {
            // deposit code to int
            var depositCodeInt = long.Parse(depositCode);

            // convert amount to int
            asset = asset.ToUpper();
            var walletProvider = serviceProvider.GetRequiredService<IWalletProvider>();
            var wallet = walletProvider.GetFiat(asset);
            var amountInt = wallet.StringToAmount(amount.ToString());

            // process deposit 
            FiatWalletTx tx;
            if (isDeposit)
                tx = wallet.UpdateDeposit(depositCode, date, amountInt, bankMetadata);
            else
                tx = wallet.UpdateWithdrawal(depositCode, date, amountInt, bankMetadata);
            System.Diagnostics.Debug.Assert(tx != null);

            // get user
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(tx.Tag.Tag);
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // test backend connection (get user balance)
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeSettings>>();
            var via = new ViaJsonRpc(settings.Value.AccessHttpUrl); //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            var balance = via.BalanceQuery(user.Exchange.Id, asset);
            Console.WriteLine($"Before - available {asset} balance: {balance.Available}");

            // save wallet
            wallet.Save();
            Console.WriteLine($"Saved {asset} wallet");

            // register new deposits with the exchange backend
            var source = new Dictionary<string, object>();
            source["bankMetadata"] = bankMetadata;
            var amountStr = amount.ToString();
            if (!isDeposit)
                amountStr = (-amount).ToString();
            via.BalanceUpdateQuery(user.Exchange.Id, asset, "deposit", depositCodeInt, amountStr, source);
            Console.WriteLine($"Updated exchange backend");

            balance = via.BalanceQuery(user.Exchange.Id, asset);
            Console.WriteLine($"After - available {asset} balance: {balance.Available}");

            // send email
            var emailSender = serviceProvider.GetRequiredService<IEmailSender>();
            if (isDeposit)
                await emailSender.SendEmailFiatDepositConfirmedAsync(user.Email, asset, wallet.AmountToString(tx.Amount), tx.DepositCode);
            else
                await emailSender.SendEmailFiatWithdrawalConfirmedAsync(user.Email, asset, wallet.AmountToString(tx.Amount), tx.DepositCode);
            Console.WriteLine($"Sent email to {user.Email}");
        }

        public static async Task ProcessFiatDeposit(IServiceProvider serviceProvider, string asset, string depositCode, long date, decimal amount, string bankMetadata)
        {
            await ProcessFiat(serviceProvider, true, asset, depositCode, date, amount, bankMetadata);
        }

        public static async Task ProcessFiatWithdrawal(IServiceProvider serviceProvider, string asset, string depositCode, long date, decimal amount, string bankMetadata)
        {
            await ProcessFiat(serviceProvider, false, asset, depositCode, date, amount, bankMetadata);
        }

        public static async Task CompletedFiatWithdrawForBrokerOrder(IServiceProvider serviceProvider, string token)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var walletProvider = serviceProvider.GetRequiredService<IWalletProvider>();
            var broker = serviceProvider.GetRequiredService<IBroker>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var apiSettings = serviceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;

            // find broker order
            var order = context.BrokerOrders.SingleOrDefault(o => o.Token == token);
            if (order == null)
            {
                Console.WriteLine($"ERROR: could not find order ({token})");
                return;
            }
            // check BrokerOrderFiatWithdrawal does not exist
            var bow = context.BrokerOrderFiatWithdrawals.SingleOrDefault(b => b.BrokerOrderId == order.Id);
            if (bow != null)
            {
                Console.WriteLine($"ERROR: BrokerOrderFiatWithdrawal object already exists for this order");
                return;
            }
            // check receive asset is fiat
            if (!walletProvider.IsFiat(order.AssetReceive))
            {
                Console.WriteLine($"ERROR: order.AssetReceive ({order.AssetReceive}) is not fiat");
                return;
            }
            // get fiat wallet
            var wallet = walletProvider.GetFiat(order.AssetReceive);
            if (wallet == null)
            {
                Console.WriteLine($"ERROR: could not get wallet ({order.AssetReceive})");
                return;
            }
            // get broker user
            var brokerUser = userManager.FindByNameAsync(apiSettings.Broker.BrokerTag).GetAwaiter().GetResult();
            if (brokerUser == null)
            {
                Console.WriteLine("Failed to find broker user");
                return;
            }
            // create BrokerOrderFiatWithdrawal and pending wallet withdrawal
            if (!broker.FiatWithdrawToCustomer(brokerUser, order))
            {
                Console.WriteLine("Failed call to FiatWithdrawToCustomer");
                return;
            }
            // find created BrokerOrderFiatWithdrawal
            bow = context.BrokerOrderFiatWithdrawals.SingleOrDefault(b => b.BrokerOrderId == order.Id);
            if (bow == null)
            {
                Console.WriteLine($"ERROR: BrokerOrderFiatWithdrawal object does not exist (we should have just created it)");
                return;
            }
            // update newly created withdrawal and set to processed
            var amountInt = wallet.AmountToLong(order.AmountReceive);
            var fiatTx = wallet.UpdateWithdrawal(bow.DepositCode, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), amountInt, "");
            if (fiatTx == null)
            {
                Console.WriteLine("Failed call to wallet.UpdateWithdrawal");
                return;
            }
            wallet.Save();
        }

        public static void ProcessChainWithdrawal(IServiceProvider serviceProvider, string asset, string spendCode)
        {
            // get wallet
            asset = asset.ToUpper();
            var walletProvider = serviceProvider.GetRequiredService<IWalletProvider>();
            var wallet = walletProvider.GetChain(asset);
            var assetSettings = walletProvider.ChainAssetSettings(asset);

            // process withdrawal
            Console.WriteLine($"Actioning pending spend: {spendCode}, asset: {asset}");
            var err = wallet.PendingSpendAction(spendCode, assetSettings.FeeMax, assetSettings.FeeUnit, out IEnumerable<WalletTx> wtxs);
            Console.WriteLine($"Result: {err}");

            // save wallet
            wallet.Save();
            Console.WriteLine($"Saved {asset} wallet");

            if (err == WalletError.Success)
            {
                foreach (var wtx in wtxs)
                {
                    // get user
                    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var tags = wallet.GetTagsFor(wtx);
                    System.Diagnostics.Debug.Assert(tags.Count() == 1);
                    var user = userManager.FindByIdAsync(tags.First().Tag).GetAwaiter().GetResult();
                    System.Diagnostics.Debug.Assert(user != null);

                    // send email
                    var emailSender = serviceProvider.GetRequiredService<IEmailSender>();
                    emailSender.SendEmailChainWithdrawalConfirmedAsync(user.Email, asset, wallet.AmountToString(wtx.AmountInputs() - wtx.ChainTx.Fee), wtx.ChainTx.TxId).GetAwaiter().GetResult();
                    Console.WriteLine($"Sent email to {user.Email}");
                }
            }
        }

        public static void ShowPendingChainWithdrawals(IServiceProvider serviceProvider, string asset)
        {
            // get wallet
            asset = asset.ToUpper();
            var walletProvider = serviceProvider.GetRequiredService<IWalletProvider>();
            var wallet = walletProvider.GetChain(asset);

            // show pending spends
            var spends = wallet.PendingSpendsGet(null, new PendingSpendState[]{ PendingSpendState.Pending, PendingSpendState.Error });
            foreach (var spend in spends)
                Console.WriteLine($"SpendCode: {spend.SpendCode}, Date: {spend.Date}, Amount: {spend.Amount}, To: {spend.To}, State: {spend.State}");
        }
        
        public static void CheckChainDeposits(IServiceProvider serviceProvider, string asset)
        {
            // get exchange settings
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeSettings>>().Value;
            var apiSettings = serviceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;

            // get wallet
            asset = asset.ToUpper();
            var walletProvider = serviceProvider.GetRequiredService<IWalletProvider>();
            var wallet = walletProvider.GetChain(asset);
            var assetSettings = walletProvider.ChainAssetSettings(asset);

            // update wallet
            var dbtx = wallet.BeginDbTransaction();
            wallet.UpdateFromBlockchain(dbtx);
            wallet.Save();
            dbtx.Commit();

            // get the user manager & email sender
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var emailSender = serviceProvider.GetRequiredService<IEmailSender>();

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
                        Console.WriteLine($"{user.Email}: new tx: {tx}");
                    foreach (var tx in addrTxs.JustAckedTxs)
                        Console.WriteLine($"{user.Email}: confirmed tx: {tx}");
                }
            }
        }
    }
}