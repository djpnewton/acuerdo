using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using viafront3.Models.MarketViewModels;
using viafront3.Models.ApiViewModels;
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

        static public OrdersPendingViewModel Construct(ApplicationUser loggedInUser, ApplicationUser tradeUser, string market, ExchangeSettings settings, int offset, int limit)
        {
            var via = new ViaJsonRpc(settings.AccessHttpUrl);
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            var ordersPending = via.OrdersPendingQuery(tradeUser.Exchange.Id, market, offset, limit);

            var model = new OrdersPendingViewModel
            {
                User = loggedInUser,
                Market = market,
                MarketNice = string.Format("{0}/{1}", settings.Markets[market].AmountUnit, settings.Markets[market].PriceUnit),
                AssetSettings = settings.Assets,
                Settings = settings.Markets[market],
                OrdersPending = new OrdersPendingPartialViewModel { OrdersPending = ordersPending },
            };

            return model;
        }
    }
    
    public class OrdersCompletedViewModel : BaseTradeViewModel
    {
        public OrdersCompleted OrdersCompleted { get; set; }

        static public OrdersCompletedViewModel Construct(ApplicationUser loggedInUser, ApplicationUser tradeUser, string market, OrderSide side, ExchangeSettings settings, int offset, int limit)
        {
            var via = new ViaJsonRpc(settings.AccessHttpUrl);
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            var bidOrdersCompleted = via.OrdersCompletedQuery(tradeUser.Exchange.Id, market, 1, now, offset, limit, side);

            var model = new OrdersCompletedViewModel
            {
                User = loggedInUser,
                Market = market,
                MarketNice = string.Format("{0}/{1}", settings.Markets[market].AmountUnit, settings.Markets[market].PriceUnit),
                AssetSettings = settings.Assets,
                Settings = settings.Markets[market],
                OrdersCompleted = bidOrdersCompleted,
            };

            return model;
        }
    }

    public class TradeViewModel : BaseTradeViewModel
    {
        public OrderbookPartialViewModel OrderBook { get; set; }

        public BalancesPartialViewModel Balances { get; set; }

        public OrdersPendingPartialViewModel OrdersPending { get; set; }

        public OrdersCompleted BidOrdersCompleted { get; set; }

        public OrdersCompleted AskOrdersCompleted { get; set; }

        public ApiOrderCreateLimit Order { get; set; }

        static public TradeViewModel Construct(ApplicationUser loggedInUser, ApplicationUser tradeUser, string market, string side, string amount, string price, ExchangeSettings settings)
        {
            var via = new ViaJsonRpc(settings.AccessHttpUrl);
            var balances = via.BalanceQuery(tradeUser.Exchange.Id);
            var ordersPending = via.OrdersPendingQuery(tradeUser.Exchange.Id, market, 0, 10);
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            var bidOrdersCompleted = via.OrdersCompletedQuery(tradeUser.Exchange.Id, market, 1, now, 0, 10, OrderSide.Bid);
            var askOrdersCompleted = via.OrdersCompletedQuery(tradeUser.Exchange.Id, market, 1, now, 0, 10, OrderSide.Ask);

            var orderDepth = via.OrderDepthQuery(market, settings.OrderBookLimit, settings.Markets[market].PriceInterval);
            var ob = new OrderbookPartialViewModel
            {
                AssetSettings = settings.Assets,
                AmountUnit = settings.Markets[market].AmountUnit,
                PriceUnit = settings.Markets[market].PriceUnit,
                OrderDepth = orderDepth
            };

            var model = new TradeViewModel
            {
                User = loggedInUser,
                Market = market,
                MarketNice = string.Format("{0}/{1}", settings.Markets[market].AmountUnit, settings.Markets[market].PriceUnit),
                AssetSettings = settings.Assets,
                Settings = settings.Markets[market],
                OrderBook = ob,
                Balances = new BalancesPartialViewModel{Balances=balances},
                OrdersPending = new OrdersPendingPartialViewModel { OrdersPending = ordersPending },
                BidOrdersCompleted = bidOrdersCompleted,
                AskOrdersCompleted = askOrdersCompleted,
                Order = new ApiOrderCreateLimit { Market = market, Side = side, Amount = amount, Price = price}
            };

            return model;
        }
    }
}
