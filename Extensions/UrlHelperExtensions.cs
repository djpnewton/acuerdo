using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using viafront3.Controllers;

namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string EmailConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ConfirmEmail),
                controller: "Account",
                values: new { userId, code },
                protocol: scheme);
        }

        public static string ResetPasswordCallbackLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ResetPassword),
                controller: "Account",
                values: new { userId, code },
                protocol: scheme);
        }

        public static string AccountCreationConfirmationLink(this IUrlHelper urlHelper, string token, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ConfirmAccountCreation),
                controller: "Account",
                values: new { token },
                protocol: scheme);
        }

        public static string DeviceCreationConfirmationLink(this IUrlHelper urlHelper, string token, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ConfirmDeviceCreation),
                controller: "Account",
                values: new { token },
                protocol: scheme);
        }
    }
}
