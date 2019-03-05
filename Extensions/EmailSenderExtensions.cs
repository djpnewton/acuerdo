using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using viafront3.Services;

namespace viafront3.Services
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link)
        {
            return emailSender.SendEmailAsync(email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }

        public static Task SendEmailChainDepositDetectedAsync(this IEmailSender emailSender, string email, string asset, string amount, string txid)
        {
            return emailSender.SendEmailAsync(email, "Deposit Detected",
                $"A new blockchain deposit detected, {amount} {asset} ({txid})");
        }

        public static Task SendEmailChainDepositConfirmedAsync(this IEmailSender emailSender, string email, string asset, string amount, string txid)
        {
            return emailSender.SendEmailAsync(email, "Deposit Confirmed",
                $"A blockchain deposit has confirmed, {amount} {asset} ({txid})");
        }

        public static Task SendEmailFiatDepositCreatedAsync(this IEmailSender emailSender, string email, string asset, string amount, string depositCode, xchwallet.BankAccount account)
        {
            return emailSender.SendEmailAsync(email, "Deposit Create",
                $"A new fiat deposit created, {amount} {asset}<br/><br/>Deposit Code: {depositCode}<br/>Bank Name: {account.BankName}<br/>Bank Address: {account.BankAddress}<br/>Account Name: {account.BankName}<br/>Account Number: {account.AccountNumber}<br/>");
        }

        public static Task SendEmailFiatDepositConfirmedAsync(this IEmailSender emailSender, string email, string asset, string amount, string depositCode)
        {
            return emailSender.SendEmailAsync(email, "Deposit Confirmed",
                $"A fiat deposit has confirmed, {amount} {asset}<br/><br/>Deposit Code: {depositCode}<br/>");
        }
    }
}
