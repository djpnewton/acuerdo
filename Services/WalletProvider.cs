using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using xchwallet;

namespace viafront3.Services
{
    public interface IWalletProvider
    {
        bool IsChain(string asset);
        bool IsFiat(string asset);
        IWallet GetChain(string asset);
        IWallet2 GetFastChain(string asset);
        IFiatWallet GetFiat(string asset);
        IFiatWallet2 GetFastFiat(string asset);
        string ConsolidatedFundsTag();
        ChainAssetSettings ChainAssetSettings(string asset);
        string AmountToString(string asset, decimal amount);
        DateTimeOffset LastBlockchainWalletUpdate(string asset);
        void UpdateBlockchainWallet(string asset);
        void UpdateBlockchainWallets();
    }

    public class WalletProvider : IWalletProvider
    {
        readonly static Object walletUpdateLock = new Object();
        readonly static Dictionary<string, DateTimeOffset> walletUpdateTimes = new Dictionary<string, DateTimeOffset>();

        private readonly ILogger _logger;
        private readonly WalletSettings _walletSettings;

        public WalletProvider(ILogger<WalletProvider> logger, IOptions<WalletSettings> walletSettings)
        {
            _logger = logger;
            _walletSettings = walletSettings.Value;
        }

        public bool IsChain(string asset)
        {
            return _walletSettings.ChainAssetSettings.ContainsKey(asset);
        }

        public bool IsFiat(string asset)
        {
            return _walletSettings.BankAccounts.ContainsKey(asset);
        }

        public IWallet GetChain(string asset)
        {
            if (!IsChain(asset))
                throw new Exception($"Wallet '{asset}' not supported");

            string dbName = null;
            if (_walletSettings.DbNames.ContainsKey(asset))
                dbName = _walletSettings.DbNames[asset];
            ChainAssetSettings cas = null;
            if (_walletSettings.ChainAssetSettings.ContainsKey(asset))
                cas = _walletSettings.ChainAssetSettings[asset];
            IWallet wallet = null;
            var db = WalletContext.CreateMySqlWalletContext<WalletContext>(_walletSettings.MySql.Host, dbName, _walletSettings.MySql.User, _walletSettings.MySql.Password, false, false, true);
            switch (asset)
            {
                case "WAVES":
                    wallet = new WavWallet(_logger, db, _walletSettings.Mainnet, new Uri(cas.NodeUrl));
                    break;
                case "ZAP":
                    wallet = new ZapWallet(_logger, db, _walletSettings.Mainnet, new Uri(cas.NodeUrl));
                    break;
                case "BTC":
                    wallet = new BtcWallet(_logger, db, _walletSettings.Mainnet, new Uri(cas.NodeUrl));
                    break;
                default:
                    throw new Exception($"Wallet '{asset}' not supported");
            }

            return wallet;
        }

        public IWallet2 GetFastChain(string asset)
        {
            if (!IsChain(asset))
                throw new Exception($"Wallet '{asset}' not supported");

            string dbName = null;
            if (_walletSettings.DbNames.ContainsKey(asset))
                dbName = _walletSettings.DbNames[asset];
            var db = WalletContext.CreateMySqlWalletContext<WalletContext>(_walletSettings.MySql.Host, dbName, _walletSettings.MySql.User, _walletSettings.MySql.Password, false, false, false);
            return new FastWallet(_logger, db, _walletSettings.Mainnet, asset);
        }

        public IFiatWallet GetFiat(string asset)
        {
            if (!IsFiat(asset))
                throw new Exception($"Wallet '{asset}' not supported");

            string dbName = null;
            if (_walletSettings.DbNames.ContainsKey(asset))
                dbName = _walletSettings.DbNames[asset];
            BankAccount account = null;
            if (_walletSettings.BankAccounts.ContainsKey(asset))
                account = _walletSettings.BankAccounts[asset];
            return new FiatWallet(_logger, WalletContext.CreateMySqlWalletContext<FiatWalletContext>(_walletSettings.MySql.Host, dbName, _walletSettings.MySql.User, _walletSettings.MySql.Password, false, false, true), asset, account);
        }

        public IFiatWallet2 GetFastFiat(string asset)
        {
            if (!IsFiat(asset))
                throw new Exception($"Wallet '{asset}' not supported");

            string dbName = null;
            if (_walletSettings.DbNames.ContainsKey(asset))
                dbName = _walletSettings.DbNames[asset];
            BankAccount account = null;
            if (_walletSettings.BankAccounts.ContainsKey(asset))
                account = _walletSettings.BankAccounts[asset];
            return new FastFiatWallet(_logger, WalletContext.CreateMySqlWalletContext<FiatWalletContext>(_walletSettings.MySql.Host, dbName, _walletSettings.MySql.User, _walletSettings.MySql.Password, false, false, false), asset, account);
        }

        public string ConsolidatedFundsTag()
        {
            return _walletSettings.ConsolidatedFundsTag;
        }

        public ChainAssetSettings ChainAssetSettings(string asset)
        {
            if (_walletSettings.ChainAssetSettings.ContainsKey(asset))
                return _walletSettings.ChainAssetSettings[asset];
            throw new Exception(string.Format("Wallet '{0}' not supported", asset));
        }

        public string AmountToString(string asset, decimal amount)
        {
            if (IsChain(asset))
            {
                var wallet = GetChain(asset);
                if (wallet != null)
                    return wallet.AmountToString(amount);
            }
            else
            {
                var wallet = GetFiat(asset);
                if (wallet != null)
                    return wallet.AmountToString(amount);
            }
            return null;
        }

        public DateTimeOffset LastBlockchainWalletUpdate(string asset)
        {
            if (walletUpdateTimes.ContainsKey(asset))
                return walletUpdateTimes[asset];
            return DateTimeOffset.FromUnixTimeSeconds(0);
        }

        public void UpdateBlockchainWallet(string asset)
        {
            lock (walletUpdateLock)
            {
                // get wallet
                var wallet = GetChain(asset);
                var assetSettings = ChainAssetSettings(asset);

                // update wallet
                var dbtx = wallet.BeginDbTransaction();
                wallet.UpdateFromBlockchain(dbtx);
                wallet.Save();
                dbtx.Commit();
                walletUpdateTimes[asset] = DateTimeOffset.Now;
                _logger.LogInformation("Completed wallet.UpdateFromBlockchain() for '{0}'", asset);
            }
        }

        public void UpdateBlockchainWallets()
        {
            foreach (var asset in _walletSettings.ChainAssetSettings.Keys)
                UpdateBlockchainWallet(asset);
        }
    }
}
