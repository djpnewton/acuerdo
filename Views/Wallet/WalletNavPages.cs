using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace viafront3.Views.Wallet
{
    public static class WalletNavPages
    {
        public static string ActivePageKey => "ActivePage";

        public static string Index => "Index";
        public static string Deposits => "Deposits";
        public static string Withdrawals => "Withdrawals";

        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);
        public static string DepositsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Deposits);
        public static string WithdrawalsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Withdrawals);

        public static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string;
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }

        public static void AddActivePage(this ViewDataDictionary viewData, string activePage) => viewData[ActivePageKey] = activePage;
    }
}
