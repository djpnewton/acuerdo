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

        public static Task SendNewEmailChangeAsync(this IEmailSender emailSender, string email)
        {
            return emailSender.SendEmailAsync(email, "Change your email",
                $"Someone requested to change your email address. If you confirm the change, this (<strong>{email}</strong>) will be your new address.");
        }

        public static Task SendOldEmailChangeAsync(this IEmailSender emailSender, string email, string newEmail, string link)
        {
            return emailSender.SendEmailAsync(email, "Change your email",
                $"Someone requested to change your email address (the new address will be <strong>{newEmail}</strong>). Ignore this email if you did not request it!<br/>To confirm the change to your email click this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
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

        public static Task SendEmailChainWithdrawalCreatedAsync(this IEmailSender emailSender, string email, string asset, string amount)
        {
            return emailSender.SendEmailAsync(email, "Withdrawal Created",
                $"A new blockchain withdrawal created, {amount} {asset}");
        }

        public static Task SendEmailChainWithdrawalConfirmedAsync(this IEmailSender emailSender, string email, string asset, string amount, string txid)
        {
            return emailSender.SendEmailAsync(email, "Withdrawal Confirmed",
                $"A blockchain withdrawal has confirmed, {amount} {asset} ({txid})");
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

        public static Task SendEmailFiatWithdrawalCreatedAsync(this IEmailSender emailSender, string email, string asset, string amount, string depositCode)
        {
            return emailSender.SendEmailAsync(email, "Withdrawal Create",
                $"A new fiat withdrawal created, {amount} {asset}");
        }

        public static Task SendEmailFiatWithdrawalConfirmedAsync(this IEmailSender emailSender, string email, string asset, string amount, string depositCode)
        {
            return emailSender.SendEmailAsync(email, "Withdrawal Confirmed",
                $"A fiat withdrawal has confirmed, {amount} {asset}");
        }

        public static Task SendEmailLimitOrderCreatedAsync(this IEmailSender emailSender, string email, string market, string side, string amount, string amountUnit, string price, string priceUnit)
        {
            return emailSender.SendEmailAsync(email, "Limit Order Created",
                $"Limit Order Created ({market} - {side}, Amount: {amount} {amountUnit}, Price: {price} {priceUnit})");
        }

        public static Task SendEmailMarketOrderCreatedAsync(this IEmailSender emailSender, string email, string market, string side, string amount, string amountUnit)
        {
            return emailSender.SendEmailAsync(email, "Market Order Created",
                $"Market Order Created ({market} - {side}, Amount: {amount} {amountUnit})");
        }

        public static Task SendEmailLimitOrderUpdatedAsync(this IEmailSender emailSender, string email, string market, string side, string amount, string amountUnit, string price, string priceUnit, string left)
        {
            return emailSender.SendEmailAsync(email, "Limit Order Updated",
                $"Limit Order Updated ({market} - {side}, Amount: {amount} {amountUnit}, Price: {price} {priceUnit}), Amount remaining: {left} {amountUnit})");
        }

        public static Task SendEmailMarketOrderUpdatedAsync(this IEmailSender emailSender, string email, string market, string side, string amount, string amountUnit, string left)
        {
            return emailSender.SendEmailAsync(email, "Market Order Updated",
                $"Market Order Updated ({market} - {side}, Amount: {amount} {amountUnit}, Amount remaining: {left} {amountUnit})");
        }

        public static Task SendEmailLimitOrderFinishedAsync(this IEmailSender emailSender, string email, string market, string side, string amount, string amountUnit, string price, string priceUnit, string left, decimal amountInterval)
        {
            var leftDec = decimal.Parse(left, System.Globalization.NumberStyles.Any);
            if (leftDec >= amountInterval)
                return emailSender.SendEmailAsync(email, "Limit Order Cancelled",
                    $"Limit Order Cancelled ({market} - {side}, Amount: {amount} {amountUnit}, Price: {price} {priceUnit})");
            else
                return emailSender.SendEmailAsync(email, "Limit Order Completed",
                    $"Limit Order Completed ({market} - {side}, Amount: {amount} {amountUnit}, Price: {price} {priceUnit})");
        }

        public static Task SendEmailMarketOrderFinishedAsync(this IEmailSender emailSender, string email, string market, string side, string amount, string amountUnit, string left, decimal amountInterval)
        {
            var leftDec = decimal.Parse(left, System.Globalization.NumberStyles.Any);
            if (leftDec >= amountInterval)
                return emailSender.SendEmailAsync(email, "Market Order Cancelled",
                    $"Market Order Cancelled ({market} - {side}, Amount: {amount} {amountUnit})");
            else
                return emailSender.SendEmailAsync(email, "Market Order Completed",
                    $"Market Order Completed ({market} - {side}, Amount: {amount} {amountUnit})");
        }

        public static Task SendEmailApiAccountCreationRequest(this IEmailSender emailSender, string email, int expiryMins, string link)
        {
            return emailSender.SendEmailAsync(email, "Confirm your account",
                $"Someone has requested to create an account for this email address. Only click the link if it was you who created this request.<br/>Confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a> (expires in {expiryMins} minutes)");
        }

        public static Task SendEmailApiKeyCreationRequest(this IEmailSender emailSender, string email, int expiryMins, string link)
        {
            return emailSender.SendEmailAsync(email, "Confirm API KEY creation",
                $"Someone has requested to create an API KEY for your account. Only click the link if it was you who created this request.<br/>Confirm your new API KEY creation by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a> (expires in {expiryMins} minutes)");
        }

        public static Task SendEmailBrokerSeenIncomingFunds(this IEmailSender emailSender, string email, string assetSent, string amountSent, string invoiceId)
        {
            return emailSender.SendEmailAsync(email, "Order initiated",
                $"The incoming funds of {amountSent} {assetSent} for your order ({invoiceId}) have arrived we are now processing the order.");
        }

        public static Task SendEmailBrokerSentOutgoingFunds(this IEmailSender emailSender, string email, string assetSent, string amountSent, string assetReceive, string amountReceive, string invoiceId)
        {
            return emailSender.SendEmailAsync(email, "Order completed funds sent",
                $"Your order ({invoiceId}) to sell {amountSent} {assetSent} has completed and {amountReceive} {assetReceive} has been sent to you.");
        }
    }
}
