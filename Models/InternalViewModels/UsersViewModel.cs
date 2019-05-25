using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using via_jsonrpc;
using viafront3.Models.TradeViewModels;

namespace viafront3.Models.InternalViewModels
{
    public class UserInfo
    {
        public ApplicationUser User { set; get; }
        public List<string> Roles { set; get; } 
    }

    public class UsersViewModel : BaseViewModel
    {
        public IEnumerable<UserInfo> UserInfos { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int Count { get; set; }
        public string Role { get; set; }
        public string EmailSearch { get; set; }
        public string UserId { get; set; }
    }

    public class UserViewModel : BaseViewModel
    {
        public ApplicationUser UserInspect { get; set; }
        public BalancesPartialViewModel Balances { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
    }

    public class UserBalanceHistoryViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
        public BalanceHistory BalanceHistory { get; set; }
    }
}
