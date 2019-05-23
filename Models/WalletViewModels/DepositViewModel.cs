using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using via_jsonrpc;
using xchwallet;

namespace viafront3.Models.WalletViewModels
{
    public class DepositViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public string DepositAddress { get; set; }
    }

    public class UserTransactionsViewModel : DepositViewModel
    {
        public ChainAssetSettings ChainAssetSettings { get; set; }
        public AssetSettings AssetSettings { get; set; }
        public IWallet Wallet { get; set; }
        public IEnumerable<WalletTx> TransactionsIncomming { get; set; }
        public int TxsIncommingOffset { get; set; }
        public int TxsIncommingLimit { get; set; }
        public int TxsIncommingCount { get; set; }
        public IEnumerable<WalletTx> TransactionsOutgoing { get; set; }
    }

    public class DepositFiatViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public decimal Amount { get; set; } 
        public FiatWalletTx PendingTx { get; set; }
        public BankAccount Account { get; set; }
    }

    public class FiatTransactionsViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
        public IFiatWallet Wallet { get; set; }
        public IEnumerable<FiatWalletTx> Transactions { get; set; }
    }
}
