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

        public static string EmailChangeLink(this IUrlHelper urlHelper, string oldEmail, string newEmail, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(ManageController.ConfirmEmailChange),
                controller: "Manage",
                values: new { oldEmail, newEmail, code },
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

        public static string AccountCreationConfirmationLink(this IUrlHelper urlHelper, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ConfirmAccountCreation),
                controller: "Account",
                values: new { code },
                protocol: scheme);
        }

        public static string ApiKeyCreationConfirmationLink(this IUrlHelper urlHelper, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ConfirmApiKeyCreation),
                controller: "Account",
                values: new { code },
                protocol: scheme);
        }
    }
}
