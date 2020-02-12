using System;
using System.Collections.Generic;

namespace viafront3.Models.ReportViewModels
{
    public class ExchangeViewModel : BaseViewModel
    {
        public IEnumerable<via_jsonrpc.sql.Deal> Deals { set; get; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int Count { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string OrderStatus { get; set; }
        public string NotOrderStatus { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
    }
}
