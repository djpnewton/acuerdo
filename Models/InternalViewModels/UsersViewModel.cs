using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using via_jsonrpc;
using viafront3.Data;
using viafront3.Models.TradeViewModels;

namespace viafront3.Models.InternalViewModels
{
    public class UserInfo
    {
        public ApplicationUser User { set; get; }
        public List<string> Roles { set; get; }

        public static IEnumerable<UserInfo> Query(ApplicationDbContext context, string role, string email, string name)
        {
            var userInfos = (from u in context.Users
                             let query = (from ur in context.Set<IdentityUserRole<string>>()
                                          where ur.UserId.Equals(u.Id)
                                          join r in context.Roles on ur.RoleId equals r.Id
                                          select r.Name)
                             select new UserInfo() { User = u, Roles = query.ToList<string>() });
            if (role == "")
                role = null;
            if (role != null)
                userInfos = userInfos.Where(ui => ui.Roles.Contains(role));
            if (email == "")
                email = null;
            if (email != null)
                userInfos = userInfos.Where(ui => ui.User.NormalizedEmail.Contains(email.ToUpper()));
            if (name == "")
                name = null;
            if (name != null)
                userInfos = userInfos.Where(ui => ui.User.NormalizedUserName.Contains(name.ToUpper()));
            userInfos = userInfos.OrderBy(ui => ui.User.Exchange == null ? -1 : ui.User.Exchange.Id);

            return userInfos;
        }
    }

    public class UsersViewModel : BaseViewModel
    {
        public IEnumerable<UserInfo> UserInfos { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public int Count { get; set; }
        public string Role { get; set; }
        public string EmailSearch { get; set; }
        public string NameSearch { get; set; }
        public string UserId { get; set; }
    }

    public class UserViewModel : BaseViewModel
    {
        public ApplicationUser UserInspect { get; set; }
        public BalancesPartialViewModel Balances { get; set; }
        public KycLevel KycLevel { get; set; }
        public string KycRequestUrl { get; set; }
        public KycSettings KycSettings { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
    }

    public class UserBalanceHistoryViewModel : BaseViewModel
    {
        public string Asset { get; set; }
        public Dictionary<string, AssetSettings> AssetSettings { get; set; }
        public BalanceHistory BalanceHistory { get; set; }
    }
}
