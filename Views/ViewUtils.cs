using System;
using System.Globalization;

namespace viafront3.Views
{
    public static class ViewUtils
    {
        public static decimal StrFloatToDec(string value)
        {
            return Decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static string FormatStrDec(string value, int decimals)
        {
            var dec = StrFloatToDec(value);
            return dec.ToString("0." + new string('0', decimals));
        }
    }
}
