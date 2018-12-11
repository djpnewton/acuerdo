using System;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using via_jsonrpc;

namespace viafront3.Views
{
    public static class ViewUtils
    {
        public static decimal StrFloatToDec(string value)
        {
            return Decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static string FormatDec(decimal value, int decimals)
        {
            return value.ToString("0." + new string('0', decimals));
        }

        public static string FormatStrDec(string value, int decimals)
        {
            var dec = StrFloatToDec(value);
            return FormatDec(dec, decimals);
        }

        public static string CompletedOrderPrice(Order order, int decimals)
        {
            if (order.type == OrderType.Limit)
                return FormatStrDec(order.price, decimals);
            var tradedAmount = StrFloatToDec(order.deal_stock);
            var tradedMoney = StrFloatToDec(order.deal_money);
            var price = tradedMoney / tradedAmount;
            return FormatDec(price, decimals);
        }

        public static string FormatUnixTime(long time)
        {
            return DateTimeOffset.FromUnixTimeSeconds(time).ToString("yyyy/MM/dd HH:mm");
        }

        public static string FormatUnixTime(float time)
        {
            return FormatUnixTime((long)time);
        }

        public static string FormatDict(Dictionary<string, object> dict)
        {
            return JsonConvert.SerializeObject(dict);
        }
    }
}
