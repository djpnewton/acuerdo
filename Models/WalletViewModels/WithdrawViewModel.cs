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
    public class WithdrawViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public string BalanceAvailable { get; set; }
        public string WithdrawalAddress { get; set; }
        public decimal Amount { get; set; }
    }

    public class WithdrawalHistoryViewModel : BaseViewModel
    {
        public IWallet Wallet { get; set; }
        public string Asset { get; set; }
        public AssetSettings AssetSettings { get; set; }
        public IEnumerable<WalletPendingSpend> PendingWithdrawals { get; set; }
        public int PendingOffset { get; set; }
        public int PendingLimit { get; set; }
        public int PendingCount { get; set; }
        public IEnumerable<WalletTx> OutgoingTransactions { get; set; }
        public int OutgoingOffset { get; set; }
        public int OutgoingLimit { get; set; }
        public int OutgoingCount { get; set; }
    }

    public class WithdrawFiatViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public string BalanceAvailable { get; set; }
        public string WithdrawalAccount { get; set; }
        public decimal Amount { get; set; }
        public FiatWalletTx PendingTx { get; set; }
    }
}
