using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using viafront3.Models;
using viafront3.Services;
using viafront3.Data;
using xchwallet;
using via_jsonrpc;

namespace viafront3
{
    public static class Utils
    {
        public static async Task CreateRoles(IServiceProvider serviceProvider)
        {
            // create roles if they dont exist
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { "admin" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        public static async Task GiveUserRole(IServiceProvider serviceProvider, string email, string role)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var adminRole = await roleManager.FindByNameAsync(role);
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            if (!await userManager.IsInRoleAsync(user, adminRole.Name))
                await userManager.AddToRoleAsync(user, adminRole.Name);
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
            // update wallet from the blockchain
            Console.WriteLine("Updating txs from blockchain..");
            wallet.UpdateFromBlockchain();
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

            // get user
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(tx.Tag.Tag);
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            user.EnsureExchangePresent(context);

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
        }

        public static async Task ProcessFiatDeposit(IServiceProvider serviceProvider, string asset, string depositCode, long date, decimal amount, string bankMetadata)
        {
            await ProcessFiat(serviceProvider, true, asset, depositCode, date, amount, bankMetadata);
        }

        public static async Task ProcessFiatWithdrawal(IServiceProvider serviceProvider, string asset, string depositCode, long date, decimal amount, string bankMetadata)
        {
            await ProcessFiat(serviceProvider, false, asset, depositCode, date, amount, bankMetadata);
        }

        public static void ProcessChainWithdrawal(IServiceProvider serviceProvider, string asset, string spendCode)
        {
            // get wallet
            asset = asset.ToUpper();
            var walletProvider = serviceProvider.GetRequiredService<IWalletProvider>();
            var wallet = walletProvider.GetChain(asset);
            var assetSettings = walletProvider.ChainAssetSettings(asset);

            // process withdrawal
            WalletTx wtx;
            Console.WriteLine($"Actioning pending spend: {spendCode}, asset: {asset}");
            var err = wallet.PendingSpendAction(spendCode, assetSettings.FeeMax, assetSettings.FeeUnit, out wtx);
            Console.WriteLine($"Result: {err}");

            // save wallet
            wallet.Save();
            Console.WriteLine($"Saved {asset} wallet");
        }

        public static string CreateToken(int chars = 16)
        {
            const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rnd = new RNGCryptoServiceProvider();
            var tokenBytes = new byte[chars];
            rnd.GetBytes(tokenBytes);
            var token =
                Enumerable
                    .Range(0, chars)
                    .Select(i => ALPHABET[tokenBytes[i] % ALPHABET.Length])
                    .ToArray();
            return new String(token);
        }

        public static int GetDecimalPlaces(decimal n)
        {
            n = Math.Abs(n); //make sure it is positive.
            n -= (int)n;     //remove the integer part of the number.
            var decimalPlaces = 0;
            while (n > 0)
            {
                decimalPlaces++;
                n *= 10;
                n -= (int)n;
            }
            return decimalPlaces;
        }
    }
}
