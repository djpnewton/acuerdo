using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using via_jsonrpc;

namespace viafront3.Models.TradeViewModels
{
    public class TradeViewModel
    {
        public string Market { get; set; }

        public string MarketNice { get; set; }

        public string AmountUnit { get; set; }

        public string PriceUnit { get; set; }

        public Dictionary<string, Balance> Balances { get; set; }

        public OrderSide Side { get; set; }

        public string Amount { get; set; }

        public string Price { get; set; }
    }
}
