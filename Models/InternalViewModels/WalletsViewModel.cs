using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using xchwallet;

namespace viafront3.Models.InternalViewModels
{
    public class ChainWalletBalance
    {
        public BigInteger Total;
        public BigInteger Consolidated;
        public IWallet Wallet;
    }

    public class FiatWalletBalance
    {
        public long Total;
        public IFiatWallet Wallet;
    }

    public class WalletsViewModel : BaseViewModel
    {
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
        public Dictionary<string, ChainWalletBalance> ChainBalances { get; set; }
        public Dictionary<string, FiatWalletBalance> FiatBalances { get; set; }
    }

    public class WalletPendingSpendsViewModel : BaseViewModel
    {
        public IWallet Wallet { get; set; }
        public string Asset { get; set; }
        public AssetSettings AssetSettings { get; set; }
        public IEnumerable<WalletPendingSpend> PendingSpends { get; set; }
    }

    public class FiatWalletPendingTxsViewModel : BaseViewModel
    {
        public IFiatWallet Wallet { get; set; }
        public string Asset { get; set; }
        public AssetSettings AssetSettings { get; set; }
        public IEnumerable<FiatWalletTx> PendingTxs { get; set; }
    }
}
