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

        public static async Task<WalletError> ConsolidateWallet(IServiceProvider serviceProvider, string asset, string userEmail)
        {
            var _walletSettings = serviceProvider.GetRequiredService<IOptions<WalletSettings>>().Value;
            
            var factory = new LoggerFactory().AddConsole(LogLevel.Debug);
            var _logger = factory.CreateLogger("main");

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(userEmail);

            asset = asset.ToUpper();
            if (asset != "WAVES")
                throw new Exception("only waves supported atm");

            Console.WriteLine("Consolidating {0} to {1} for asset {2}", userEmail, _walletSettings.ConsolidatedFundsTag, asset);
            
            var wallet = new WavWallet(_logger, _walletSettings.WavesSeedHex, _walletSettings.WavesWalletFile,
                _walletSettings.Mainnet, new Uri(_walletSettings.WavesNodeUrl));

            IEnumerable<string> txids;
            var res = wallet.Consolidate(new List<string>() {user.Id}, _walletSettings.ConsolidatedFundsTag, _walletSettings.WavesFeeMax, _walletSettings.WavesFeeUnit, out txids);
            Console.WriteLine(res);
            foreach (var txid in txids)
                Console.WriteLine(txid);
            wallet.Save(_walletSettings.WavesWalletFile);
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
