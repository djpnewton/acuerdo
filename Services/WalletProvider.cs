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
        IFiatWallet GetFiat(string asset);
        string ConsolidatedFundsTag();
        ChainAssetSettings ChainAssetSettings(string asset);
    }

    public class WalletProvider : IWalletProvider
    {
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
            var db = WalletContext.CreateMySqlWalletContext<WalletContext>(_walletSettings.MySql.Host, dbName, _walletSettings.MySql.User, _walletSettings.MySql.Password, false, false);
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
            var wallet = new FiatWallet(_logger, WalletContext.CreateMySqlWalletContext<FiatWalletContext>(_walletSettings.MySql.Host, dbName, _walletSettings.MySql.User, _walletSettings.MySql.Password, false, false), asset, account);
            return wallet;
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
    }
}
