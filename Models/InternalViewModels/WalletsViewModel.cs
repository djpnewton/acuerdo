using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using xchwallet;

namespace viafront3.Models.InternalViewModels
{
    public class WalletBalance
    {
        public BigInteger Total;
        public BigInteger Consolidated;
        public IWallet Wallet;
    }

    public class WalletsViewModel : BaseViewModel
    {
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
        public Dictionary<string, WalletBalance> Balances { get; set; }
    }
}
