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

        public Dictionary<string, AssetSettings> AssetSettings { get; set; }

        public MarketSettings Settings { get; set; }

        public Dictionary<string, Balance> Balances { get; set; }

        public OrdersPending OrdersPending { get; set; }

        public OrderSide Side { get; set; }

        public string Amount { get; set; }

        public string Price { get; set; }

        public string FeeUnit(Order order)
        {
            return order.side == OrderSide.Ask ? Settings.PriceUnit : Settings.AmountUnit;
        }

        public int FeeDecimals(Order order)
        {
            return order.side == OrderSide.Ask ? Settings.PriceDecimals : Settings.AmountDecimals;
        }
    }
}
