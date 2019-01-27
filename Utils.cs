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
using xchwallet;

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

        public static async Task<WalletError> ConsolidateWallet(IServiceProvider serviceProvider, string asset, IEnumerable<string> userEmails)
        {
            var factory = new LoggerFactory().AddConsole(LogLevel.Debug);
            var _logger = factory.CreateLogger("main");

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            asset = asset.ToUpper();
            var walletProvider = serviceProvider.GetRequiredService<IWalletProvider>();
            var wallet = walletProvider.Get(asset);
            var assetSettings = walletProvider.CommonAssetSettings(asset);

            var userIds = new List<string>();
            Console.WriteLine("Consolidating asset '{0}' to tag '{1}'", asset, walletProvider.ConsolidatedFundsTag());
            foreach (var email in userEmails)
            {
                var emailTrimmed = email.Trim();
                var user = await userManager.FindByEmailAsync(emailTrimmed);
                Console.WriteLine("  from user: {0} - {1}", emailTrimmed, user.Id);
                userIds.Add(user.Id);
            }
            Console.WriteLine();

            IEnumerable<string> txids;
            var res = wallet.Consolidate(userIds, walletProvider.ConsolidatedFundsTag(), assetSettings.FeeMax, assetSettings.FeeUnit, out txids);
            Console.WriteLine(res);
            foreach (var txid in txids)
                Console.WriteLine(txid);
            Console.WriteLine("Saving wallet..");
            wallet.Save();
            return res;
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
    }
}
