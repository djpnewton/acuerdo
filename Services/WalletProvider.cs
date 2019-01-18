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
        void Save(IWallet wallet, string asset);
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
            if (asset == "WAVES")
                wallet = new WavWallet(_logger, _walletSettings.WavesSeedHex, _walletSettings.WavesAssetSettings.WalletFile,
                    _walletSettings.Mainnet, new Uri(_walletSettings.WavesNodeUrl));
            else if (asset == "BTC")
            {
                var network = NBitcoin.Network.TestNet;
                if (_walletSettings.Mainnet)
                    network = NBitcoin.Network.Main;
                wallet = new BtcWallet(_logger, _walletSettings.BtcSeedHex, _walletSettings.BtcAssetSettings.WalletFile,
                    network, new Uri(_walletSettings.NbxplorerUrl));
            }

            if (wallet != null)
            {
                _wallets[asset] = wallet;
                return wallet;
            }

            throw new Exception(string.Format("Wallet '{0}' not supported", asset));
        }

        public void Save(IWallet wallet, string asset)
        {
            if (asset == "WAVES")
                wallet.Save(_walletSettings.WavesAssetSettings.WalletFile);
            else if (asset == "BTC")
                wallet.Save(_walletSettings.BtcAssetSettings.WalletFile); 
            else
                throw new Exception(string.Format("Wallet '{0}' not supported", asset));
        }

        public string ConsolidatedFundsTag()
        {
            return _walletSettings.ConsolidatedFundsTag;
        }

        public CommonAssetSettings CommonAssetSettings(string asset)
        {
            if (asset == "WAVES")
                return _walletSettings.WavesAssetSettings;
            else if (asset == "BTC")
                return _walletSettings.BtcAssetSettings;
            else
                throw new Exception(string.Format("Wallet '{0}' not supported", asset));
        }
    }
}
