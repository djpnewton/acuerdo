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
        long FeeUnit(string asset);
        long FeeMax(string asset);
        //TODO: return some generic wallet settings structure FeeUnit/FeeMax etc... 
        //      which excludes the wallet backend specific stuff
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
                wallet = new WavWallet(_logger, _walletSettings.WavesSeedHex, _walletSettings.WavesWalletFile,
                    _walletSettings.Mainnet, new Uri(_walletSettings.WavesNodeUrl));

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
                wallet.Save(_walletSettings.WavesWalletFile);
            else
                throw new Exception(string.Format("Wallet '{0}' not supported", asset));
        }

        public string ConsolidatedFundsTag()
        {
            return _walletSettings.ConsolidatedFundsTag;
        }

        public long FeeUnit(string asset)
        {
            if (asset == "WAVES")
                return _walletSettings.WavesFeeUnit;
            else
                throw new Exception(string.Format("Wallet '{0}' not supported", asset));
        }

        public long FeeMax(string asset)
        {
            if (asset == "WAVES")
                return _walletSettings.WavesFeeMax;
            else
                throw new Exception(string.Format("Wallet '{0}' not supported", asset));
        }
    }
}
