using System;
using System.Collections.Generic;
using System.Linq;

namespace viafront3.Models.DevApiViewModels
{
    public class DevApiUserCreate
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool SendEmail { get; set; }
    }

    public class DevApiUserApiKeyCreate
    {
        public string Email { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
    }

    public class DevApiUserFundGive
    {
        public string Email { get; set; }
        public string Asset { get; set; }
        public decimal Amount { get; set; }
    }

    public class DevApiUserFundSet : DevApiUserFundGive
    { }

    public class DevApiUserFundGet
    {
        public string Email { get; set; }
        public string Asset { get; set; }
    }

    public class DevApiUserFundGetResult
    {
        public decimal Amount { get; set; }
    }

    public class DevApiUserFundCheck : DevApiUserFundGive
    { }

    public class DevApiUserLimitOrder
    {
        public string Email { get; set; }
        public string Market { get; set; }
        public string Side { get; set; }
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
    }

    public class DevApiClearAllOrders
    {
        public string Market { get; set; }
    }

    public class DevApiResetWithdrawalLimit
    {
        public string Email { get; set; }
    }
}
