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
        string ConsolidatedFundsTag();
        CommonAssetSettings CommonAssetSettings(string asset);
        //TODO: add more assets
    }

    public class WalletProvider : IWalletProvider
    {
        private readonly ILogger _logger;
        private readonly WalletSettings _walletSettings;

        private Dictionary<string, IWallet> _wallets = new Dictionary<string, IWallet>();

        public WalletProvider(ILogger<WalletProvider> logger, IOptions<WalletSettings> walletSettings)
        {
            _logger = logger;
            _walletSettings = walletSettings.Value;
        }

        public IWallet Get(string asset)
        {
            if (_wallets.ContainsKey(asset))
                return _wallets[asset];

            IWallet wallet = null;
            switch (asset)
            {
                case "WAVES":
                    wallet = new WavWallet(_logger, WalletContext.CreateSqliteWalletContext(_walletSettings.WavesDbFile),
                        _walletSettings.Mainnet, new Uri(_walletSettings.WavesNodeUrl));
                    break;
                case "ZAP":
                    wallet = new ZapWallet(_logger, WalletContext.CreateSqliteWalletContext(_walletSettings.ZapDbFile),
                        _walletSettings.Mainnet, new Uri(_walletSettings.WavesNodeUrl));
                    break;
                case "BTC":
                {
                    var network = NBitcoin.Network.TestNet;
                    if (_walletSettings.Mainnet)
                        network = NBitcoin.Network.Main;
                    wallet = new BtcWallet(_logger, WalletContext.CreateSqliteWalletContext(_walletSettings.BtcDbFile),
                        network, new Uri(_walletSettings.NbxplorerUrl));
                    break;
                }
                default:
                    throw new Exception(string.Format("Wallet '{0}' not supported", asset));
            }

            _wallets[asset] = wallet;
            return wallet;
        }

        public string ConsolidatedFundsTag()
        {
            return _walletSettings.ConsolidatedFundsTag;
        }

        public CommonAssetSettings CommonAssetSettings(string asset)
        {
            switch (asset)
            {
                case "WAVES":
                    return _walletSettings.WavesAssetSettings;
                case "ZAP":
                    return _walletSettings.ZapAssetSettings;
                case "BTC":
                    return _walletSettings.BtcAssetSettings;
                default:
                    throw new Exception(string.Format("Wallet '{0}' not supported", asset));
            }
        }
    }
}
