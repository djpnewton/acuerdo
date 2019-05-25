using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using via_jsonrpc;
using viafront3.Models.TradeViewModels;

namespace viafront3.Models.InternalViewModels
{
    public class BrokerViewModel : BaseViewModel
    {
        public IEnumerable<BrokerOrder> Orders { set; get; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int Count { get; set; }
        public string OrderStatus { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
    }
}
