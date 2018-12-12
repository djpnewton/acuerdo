using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using via_jsonrpc;

namespace viafront3.Models.WalletViewModels
{
    public class BalanceViewModel : BaseViewModel
    {
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }

        public Dictionary<string, Balance> Balances { get; set; }
    }
}
