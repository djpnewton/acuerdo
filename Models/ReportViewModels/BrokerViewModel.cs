using System;
using System.Collections.Generic;
using xchwallet;

namespace viafront3.Models.ReportViewModels
{
    public class BrokerViewModel : BaseViewModel
    {
        public IEnumerable<BrokerOrder> Orders { set; get; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int Count { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string OrderStatus { get; set; }
        public string NotOrderStatus { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
    }

    public class BrokerOrderViewModel : BaseViewModel
    {
        public BrokerOrder Order { set; get; }
        public WalletPendingSpend ChainWithdrawal { get; set; }
        public FiatWalletTx FiatWithdrawal { get; set; }
        public string KycRequestUrl { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
    }
}
