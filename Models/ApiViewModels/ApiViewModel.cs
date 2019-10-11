using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using via_jsonrpc;

namespace viafront3.Models.ApiViewModels
{
    public struct ApiToken
    {
        [Required]
        public string Token;
    }

    public struct ApiKey
    {
        public bool Completed;
        public string Key;
        public string Secret;
    }

    public class ApiAccountCreate
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string DeviceName { get; set; }
    }

    public class ApiKeyCreate
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string DeviceName { get; set; }
    }

    public class ApiAuth
    {
        [Required]
        public String Key { get; set; }
        [Required]
        public long Nonce { get; set; }
    }

    public class ApiAccountBalance
    {
        public Dictionary<string, Balance> Assets { get; set; }
    }

    public class ApiAccountKyc
    {
        public String Level { get; set; }
        public String WithdrawalLimit { get; set; }
        public String WithdrawalAsset { get; set; }
        public String WithdrawalPeriod { get; set; }
        public String WithdrawalTotal { get; set; }
    }

    public enum ApiRequestStatus
    {
        Completed,
    }

    public class ApiAccountKycRequest
    {
        public String Token { get; set; }
        public String ServiceUrl { get; set; }
        public String Status { get; set; }
    }

    public class ApiAccountKycRequestStatus : ApiAuth
    {
        [Required]
        public String Token { get; set; }
    }

    public class ApiFiatPaymentRequest
    {
        public String Token { get; set; }
        public String ServiceUrl { get; set; }
        public String Status { get; set; }
        public String Asset { get; set; }
        public Decimal Amount { get; set; }
    }

    public class ApiFiatPayoutRequest
    {
        public String Token { get; set; }
        public String Status { get; set; }
        public String Asset { get; set; }
        public Decimal Amount { get; set; }
    }
    
    public class ApiMarketList
    {
        public List<String> Markets { get; set; }
    }
    
    public class ApiMarketPeriod
    {
        [Required]
        public string Market { get; set; }
        public int? Period { get; set; }
    }

    public class ApiMarketStatus
    {
        public int Period { get; set; }
        public string Open { get; set; }
        public string Close { get; set; }
        public string High { get; set; }
        public string Low { get; set; }
        public string Volume { get; set; }
    }

    public class ApiMarket
    {
        [Required]
        public string Market { get; set; }
    }

    public class ApiMarketDetail
    {
        public string TakerFeeRate { get; set; }
        public string MakerFeeRate { get; set; }
        public string MinAmount { get; set; }
        public string TradeAsset { get; set; }
        public string PriceAsset { get; set; }
        public int TradeDecimals { get; set; }
        public int PriceDecimals { get; set; }        
    }

    public class ApiMarketDepth
    {
        [Required]
        public string Market { get; set; }
        [Required]
        public string Merge { get; set; }
        public int? Limit { get; set; }
    }

    public class ApiMarketDepthResponse
    {
        public IList<IList<string>> Asks { get; set; }
        public IList<IList<string>> Bids { get; set; }
    }

    public class ApiMarketHistory
    {
        [Required]
        public string Market { get; set; }
        public int? Limit { get; set; }
    }

    public class ApiMarketTrade
    {
        public int Date { get; set; }
        public string Price { get; set; }
        public string Amount { get; set; }
        public string Type { get; set; }
    }

    public class ApiMarketHistoryResponse
    {
        public IList<ApiMarketTrade> Trades;
    }

    public class ApiMarketChart
    {
        [Required]
        public string Market { get; set; }
        [Required]
        public int Start { get; set; }
        [Required]
        public int End { get; set; }
        [Required]
        public int Interval { get; set; }
    }

    public class ApiMarketCandlestick
    {
        public int Date { get; set; }
        public string Open { get; set; }
        public string Close { get; set; }
        public string High { get; set; }
        public string Low { get; set; }
        public string Volume { get; set; }
    }

    public class ApiMarketChartResponse
    {
        public IList<ApiMarketCandlestick> Candlesticks;
    }

    public class ApiOrderCreateMarket : ApiAuth
    {
        [Required]
        public string Market { get; set; }
        [Required]
        public string Side { get; set; }
        [Required]
        [Range(typeof(decimal), "0", "1000000000000", ErrorMessage = "Please enter a value between 0 and 1000000000000")]
        public string Amount { get; set; }
    }

    public class ApiOrderCreateLimit : ApiOrderCreateMarket
    {
        [Required]
        [Range(typeof(decimal), "0", "1000000000000", ErrorMessage = "Please enter a value between 0 and 1000000000000")]
        public string Price { get; set; }
    }

    public class ApiOrder
    {
        public int Id { get; set; }
        public string Market { get; set; }
        public string Type { get; set; }
        public string Side { get; set; }
        public string Amount { get; set; }
        public string Price { get; set; }
        public string Status { get; set; }
        public int DateCreated { get; set; }
        public int DateModified { get; set; }
        public string AmountTraded { get; set; }
        public string ExecutedValue { get; set; }
        public string FeePaid { get; set; }
        public string MakerFeeRate { get; set; }
        public string TakerFeeRate { get; set; }
    }

    public class ApiOrders: ApiAuth
    {
        [Required]
        public string Market { get; set; }
        [Required]
        public int Offset { get; set; }
        [Required]
        public int Limit { get; set; }       
    }

    public class ApiOrdersResponse
    {
        public int Offset { get; set; }
        public int Limit { get; set; }       
        public IList<ApiOrder> Orders;   
    }

    public class ApiOrderPendingStatus: ApiAuth
    {
        [Required]
        public string Market { get; set; }
        [Required]
        public int Id { get; set; }
    }

    public class ApiOrderExecutedStatus: ApiAuth
    {
        [Required]
        public int Id { get; set; }
    }

    public class ApiOrderCancel: ApiAuth
    {
        [Required]
        public string Market { get; set; }
        [Required]
        public int Id { get; set; }
    }

    public class ApiTrade
    {
        public int Id { get; set; }
        public string Market { get; set; }
        public string Role { get; set; }
        public string Side { get; set; }
        public string Amount { get; set; }
        public string Price { get; set; }
        public string ExecutedValue { get; set; }
        public string Fee { get; set; }
        public string FeeAsset { get; set; }
        public int Date { get; set; }
        public int OrderId { get; set; }
    }

    public class ApiTrades: ApiAuth
    {
        [Required]
        public string Market { get; set; }
        [Required]
        public int Offset { get; set; }
        [Required]
        public int Limit { get; set; }       
    }

    public class ApiTradesResponse
    {
        public int Offset { get; set; }
        public int Limit { get; set; }       
        public IList<ApiTrade> Trades;   
    }

    public class ApiBrokerMarkets
    {
        public List<string> SellMarkets { get; set; }
        public List<string> BuyMarkets { get; set; } 
    }

    public class ApiBrokerQuote : ApiAuth
    {
        [Required]
        public string Market { get; set; }
        [Required]
        public string Side { get; set; }
        [Required]
        public string Amount { get; set; }
    }

        public class ApiBrokerQuoteInternal
    {
        public string AssetSend { get; set; }
        public decimal AmountSend { get; set; }
        public string AssetReceive { get; set; }
        public decimal AmountReceive { get; set; }
        public int TimeLimit { get; set; }
    }

    public class ApiBrokerQuoteResponse
    {
        public string AssetSend { get; set; }
        public string AmountSend { get; set; }
        public string AssetReceive { get; set; }
        public string AmountReceive { get; set; }
        public int TimeLimit { get; set; }
    }
    
    public class ApiBrokerCreate : ApiBrokerQuote
    {
        [Required]
        public string Recipient { get; set; }
    }
    
    public class ApiBrokerOrder
    {
        public long Date { get; set; }
        public string AssetSend { get; set; }
        public string AmountSend { get; set; }
        public string AssetReceive { get; set; }
        public string AmountReceive { get; set; }
        public long Expiry { get; set; }
        public string Token { get; set; }
        public string InvoiceId { get; set; }
        public string PaymentAddress { get; set; }
        public string PaymentUrl { get; set; }
        public string TxIdPayment { get; set; }
        public string Recipient { get; set; }
        public string TxIdRecipient { get; set; }
        public string Status { get; set; }
    }

    public class ApiBrokerStatus : ApiAuth
    {
        [Required]
        public string Token { get; set; }
    }

    public class ApiBrokerOrders : ApiAuth
    {
        [Required]
        public int Offset { get; set; }
        [Required]
        public int Limit { get; set; }
        public string Status { get; set; }     
    }

    public class ApiBrokerOrdersResponse
    {
        public int Offset { get; set; }
        public int Limit { get; set; }       
        public IList<ApiBrokerOrder> Orders;   
    }
}
