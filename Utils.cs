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
    public static class Utils
    {
        public const string AdminRole = "admin";
        public const string EmailConfirmedRole = "emailconfirmed";

        public struct AddressIncommingTxs
        {
            public IEnumerable<WalletTx> IncommingTxs;
            public List<WalletTx> NewlySeenTxs;
            public IEnumerable<WalletTx> JustAckedTxs;
            public BigInteger NewDeposits;
        }

        public static async Task<AddressIncommingTxs> CheckAddressIncommingTxsAndUpdateWalletAndExchangeBalance(IEmailSender emailSender, ExchangeSettings settings, string asset, IWallet wallet, ChainAssetSettings chainAssetSettings, ApplicationUser user, WalletAddr addr)
        {
            // create and test backend connection
            var via = new ViaJsonRpc(settings.AccessHttpUrl); //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            via.BalanceQuery(1);

            // get wallet transactions
            var newlySeenTxs = new List<WalletTx>();
            var incommingTxs = wallet.GetAddrTransactions(addr.Address);
            if (incommingTxs != null)
                incommingTxs = incommingTxs.Where(t => t.Direction == WalletDirection.Incomming);
            else
                incommingTxs = new List<WalletTx>();
            foreach (var tx in incommingTxs)
                if (tx.State == WalletTxState.None)
                {
                    // send email: deposit detected
                    wallet.SeenTransaction(tx);
                    newlySeenTxs.Add(tx);
                    if (!string.IsNullOrEmpty(user.Email))
                        await emailSender.SendEmailChainDepositDetectedAsync(user.Email, asset, wallet.AmountToString(tx.AmountOutputs()), tx.ChainTx.TxId);
                }
            var unackedTxs = wallet.GetAddrUnacknowledgedTransactions(addr.Address);
            if (unackedTxs != null)
                unackedTxs = unackedTxs.Where(t => t.Direction == WalletDirection.Incomming && t.ChainTx.Confirmations >= chainAssetSettings.MinConf);
            else
                unackedTxs = new List<WalletTx>();
            BigInteger newDeposits = 0;
            foreach (var tx in unackedTxs)
            {
                newDeposits += tx.AmountOutputs();
                // send email: deposit confirmed
                await emailSender.SendEmailChainDepositConfirmedAsync(user.Email, asset, wallet.AmountToString(tx.AmountOutputs()), tx.ChainTx.TxId);
            }

            // ack txs and save wallet
            IEnumerable<WalletTx> justAckedTxs = unackedTxs;
            if (unackedTxs.Any())
            {
                justAckedTxs = new List<WalletTx>(unackedTxs); // wallet.Save will kill unackedTxs because they are no longer unacked
                wallet.AcknowledgeTransactions(unackedTxs);
                wallet.Save();
            }
            else if (newlySeenTxs.Any())
                wallet.Save();

            // register new deposits with the exchange backend
            foreach (var tx in justAckedTxs)
            {
                var amount = wallet.AmountToString(tx.AmountOutputs());
                var source = new Dictionary<string, object>();
                source["txid"] = tx.ChainTx.TxId;
                var businessId = tx.Id;
                via.BalanceUpdateQuery(user.Exchange.Id, asset, "deposit", businessId, amount, source);
            }

            return new AddressIncommingTxs { IncommingTxs=incommingTxs, NewlySeenTxs=newlySeenTxs, JustAckedTxs=justAckedTxs, NewDeposits=newDeposits };
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

        public static Models.ApiKey CreateApiKey(ApplicationUser user, int accountRequestId, int apiKeyRequestId, string deviceName)
        {
            var key = Utils.CreateToken();
            var secret = Utils.CreateToken(32);
            return new Models.ApiKey
            { 
                ApplicationUserId = user.Id,
                AccountCreationRequestId = accountRequestId,
                ApiKeyCreationRequestId = apiKeyRequestId,
                Name = deviceName,
                Key = key,
                Secret = secret,
                Nonce = 0
            };
        }

        public static (bool success, string error) ValidateOrderParams(ExchangeSettings settings, ApiOrderCreateMarket model, string price, bool marketOrder=false)
        {
            // check market exists
            if (model.Market == null || !settings.Markets.ContainsKey(model.Market))
                return (false, "Market does not exist");
            // check amount exists
            if (model.Amount == null)
                return (false, "Amount not present");
            // initialize amount vars for further validation
            var amount = decimal.Parse(model.Amount);
            var amountInterval = decimal.Parse(settings.Markets[model.Market].AmountInterval);
            // check amount is greater then amountInterval
            if (amount < amountInterval)
                return (false, $"Amount is less then {amountInterval}");
            // check amonut is a multiple of the amount interval
            if ((amount / amountInterval) % 1 != 0)
                return (false, $"Amount is not a multiple of {amountInterval}");
            if (!marketOrder)
            {
                // check price exists
                if (price == null)
                    return (false, "Price not present");
                // initialize price vars for further validation
                var priceDec = decimal.Parse(price);
                var priceInterval = decimal.Parse(settings.Markets[model.Market].PriceInterval);
                // check price is greater then priceInterval
                if (priceDec < priceInterval)
                    return (false, $"Price is less then {priceInterval}");
                // check price is a multiple of the price interval
                if ((priceDec / priceInterval) % 1 != 0)
                    return (false, $"Price is not a multiple of {priceInterval}");
            }

            return (true, null);
        }

        public static (OrderSide side, string error) GetOrderSide(String side)
        {
            var res = OrderSide.Bid;
            if (side == "buy")
                res = OrderSide.Bid;
            else if (side == "sell")
                res = OrderSide.Ask;
            else
                return (res, $"Invalid side '{side}'");
            return (res, null);
        }

        public static bool ValidateBankAccount(string account)
        {
            account = account.Replace("-", "");
            account = Regex.Replace(account, @"\s+", "");
            // bank account digits 2 + 4 + 7 + 2/3, 15-16 digits
            if (account.Count() != 15 && account.Count() != 16)
                return false;
            foreach (var ch in account)
                if (!Char.IsDigit(ch))
                    return false;
            return true;
        }

        public static long IntPow(long x, long exp)
        {
            if (exp == 0)
                return 1;
            if (exp % 2 == 0)
            {
                var val = IntPow(x, exp/2);
                return val * val;
            }
            return x * IntPow(x, exp - 1);
        }
    }
}
