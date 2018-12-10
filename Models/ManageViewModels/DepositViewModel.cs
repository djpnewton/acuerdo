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

    public class UserTransactionsViewModel : DepositViewModel
    {
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
        public IWallet Wallet { get; set; }
        public IEnumerable<ITransaction> TransactionsIncomming { get; set; }
        public IEnumerable<ITransaction> TransactionsOutgoing { get; set; }
    }

    public class TransactionCheckViewModel : UserTransactionsViewModel
    {
        public IEnumerable<ITransaction> NewTransactionsIncomming { get; set; }
        public BigInteger NewDeposits { get; set; }
        public string NewDepositsHuman { get; set; }
    }
}
