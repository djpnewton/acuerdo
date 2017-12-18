using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using via_jsonrpc;

namespace viafront3.Models.TradeViewModels
{
    using Balances = Dictionary<string, Balance>;
    
    public class BaseTradeViewModel : BaseViewModel
    {
        public string Market { get; set; }

        public string MarketNice { get; set; }

        public Dictionary<string, AssetSettings> AssetSettings { get; set; }

        public MarketSettings Settings { get; set; }

        public string FeeUnit(Order order)
        {
            return order.side == OrderSide.Ask ? Settings.PriceUnit : Settings.AmountUnit;
        }

        public int FeeDecimals(Order order)
        {
            return order.side == OrderSide.Ask ? Settings.PriceDecimals : Settings.AmountDecimals;
        }
    }

    public class BalancesPartialViewModel
    {
        public Balances Balances { get; set; }
    }

    public class OrdersPendingPartialViewModel
    {
        public OrdersPending OrdersPending { get; set; }
        public string Market { get; set; }
        public int OrderId { get; set; }
    }

    public class OrdersPendingViewModel : BaseTradeViewModel
    {
        public OrdersPendingPartialViewModel OrdersPending { get; set; }
    }
    
    public class OrdersCompletedViewModel : BaseTradeViewModel
    {
        public OrdersCompleted OrdersCompleted { get; set; }
    }

    public class TradeViewModel : BaseTradeViewModel
    {
        public BalancesPartialViewModel Balances { get; set; }

        public OrdersPendingPartialViewModel OrdersPending { get; set; }

        public OrdersCompleted BidOrdersCompleted { get; set; }

        public OrdersCompleted AskOrdersCompleted { get; set; }

        public OrderSide Side { get; set; }

        public string Amount { get; set; }

        public string Price { get; set; }
    }
}
