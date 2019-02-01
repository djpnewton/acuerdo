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
        IWallet Get(string asset);
        IFiatWallet GetFiat(string asset);
        string ConsolidatedFundsTag();
        ChainAssetSettings ChainAssetSettings(string asset);
    }

    public class WalletProvider : IWalletProvider
    {
        private readonly ILogger _logger;
        private readonly WalletSettings _walletSettings;

        private Dictionary<string, IWallet> _wallets = new Dictionary<string, IWallet>();
        private Dictionary<string, IFiatWallet> _fiatWallets = new Dictionary<string, IFiatWallet>();

        public WalletProvider(ILogger<WalletProvider> logger, IOptions<WalletSettings> walletSettings)
        {
            _logger = logger;
            _walletSettings = walletSettings.Value;
        }

        public IWallet Get(string asset)
        {
            if (_wallets.ContainsKey(asset))
                return _wallets[asset];

            string dbFile = null;
            if (_walletSettings.DbFiles.ContainsKey(asset))
                dbFile = _walletSettings.DbFiles[asset];
            ChainAssetSettings cas = null;
            if (_walletSettings.ChainAssetSettings.ContainsKey(asset))
                cas = _walletSettings.ChainAssetSettings[asset];
            IWallet wallet = null;
            switch (asset)
            {
                case "WAVES":
                    wallet = new WavWallet(_logger, WalletContext.CreateSqliteWalletContext<WalletContext>(dbFile),
                        _walletSettings.Mainnet, new Uri(cas.NodeUrl));
                    break;
                case "ZAP":
                    wallet = new ZapWallet(_logger, WalletContext.CreateSqliteWalletContext<WalletContext>(dbFile),
                        _walletSettings.Mainnet, new Uri(cas.NodeUrl));
                    break;
                case "BTC":
                {
                    var network = NBitcoin.Network.TestNet;
                    if (_walletSettings.Mainnet)
                        network = NBitcoin.Network.Main;
                    wallet = new BtcWallet(_logger, WalletContext.CreateSqliteWalletContext<WalletContext>(dbFile),
                        network, new Uri(cas.NodeUrl));
                    break;
                }
                default:
                    throw new Exception(string.Format("Wallet '{0}' not supported", asset));
            }

            _wallets[asset] = wallet;
            return wallet;
        }

        public IFiatWallet GetFiat(string asset)
        {
            if (_fiatWallets.ContainsKey(asset))
                return _fiatWallets[asset];

            string dbFile = null;
            if (_walletSettings.DbFiles.ContainsKey(asset))
                dbFile = _walletSettings.DbFiles[asset];
            BankAccount account = null;
            if (_walletSettings.BankAccounts.ContainsKey(asset))
                account = _walletSettings.BankAccounts[asset];
            var wallet = new FiatWallet(_logger, WalletContext.CreateSqliteWalletContext<FiatWalletContext>(dbFile), asset, account);
            _fiatWallets[asset] = wallet;
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
