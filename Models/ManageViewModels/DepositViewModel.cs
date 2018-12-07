using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using via_jsonrpc;
using xchwallet;

namespace viafront3.Models.ManageViewModels
{
    public class DepositViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public string DepositAddress { get; set; }
    }

    public class TransactionCheckViewModel : DepositViewModel
    {
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
        public IWallet Wallet { get; set; }
        public IEnumerable<ITransaction> Transactions { get; set; }
        public IEnumerable<ITransaction> NewTransactions { get; set; }
        public BigInteger NewDeposits { get; set; }
        public string NewDepositsHuman { get; set; }
    }
}
