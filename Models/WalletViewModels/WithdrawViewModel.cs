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
}
