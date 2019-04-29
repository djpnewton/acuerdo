using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using viafront3.Services;
using viafront3.Models;
using viafront3.Models.TradeViewModels;
using viafront3.Data;
using via_jsonrpc;

namespace viafront3.Controllers
{
    public class BaseSettingsController : BaseController
    {
        protected readonly ExchangeSettings _settings;

        public BaseSettingsController(
          ILogger logger,
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context,
          IOptions<ExchangeSettings> settings) : base(logger, userManager, context)
        {
            _settings = settings.Value;
        }

        protected async Task<(IdentityResult result, ApplicationUser user)> CreateUser(SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, string username, string email, string password, bool sendEmail, bool signIn)
        {
            var user = new ApplicationUser { UserName = username, Email = email };
            IdentityResult result;
            if (password != null)
                result = await _userManager.CreateAsync(user, password);
            else
                result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Created a new user account.");
                if (email != null && sendEmail)
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                    await emailSender.SendEmailConfirmationAsync(email, callbackUrl);
                }

                if (signIn)
                    await signInManager.SignInAsync(user, isPersistent: false);

                if (user.EnsureExchangePresent(_context))
                    _context.SaveChanges();
                if (!user.EnsureExchangeBackendTablesPresent(_logger, _settings.MySql))
                    _logger.LogError("Failed to ensure backend tables present");
            }
            return (result, user);
        }
    }

    public class BaseWalletController : BaseSettingsController
    {
        protected readonly IWalletProvider _walletProvider;
        protected readonly KycSettings _kycSettings;

        public BaseWalletController(
          ILogger logger,
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context,
          IOptions<ExchangeSettings> settings,
          IWalletProvider walletProvider,
          IOptions<KycSettings> kycSettings) : base(logger, userManager, context, settings)
        {
            _walletProvider = walletProvider;
            _kycSettings = kycSettings.Value;
        }

        protected decimal CalculateWithdrawalAssetEquivalent(ILogger logger, ExchangeSettings settings, KycSettings kyc, string asset, decimal amount)
        {
            if (asset == kyc.WithdrawalAsset)
                return amount;

            foreach (var market in settings.Markets.Keys)
            {
                if (market.StartsWith(asset) && market.EndsWith(kyc.WithdrawalAsset))
                {
                    //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
                    var via = new ViaJsonRpc(_settings.AccessHttpUrl);
                    var price = via.MarketPriceQuery(market);
                    return amount * decimal.Parse(price);
                }
            };
            logger.LogError($"no price found for asset {asset}");
            throw new Exception($"no price found for asset {asset}");
        }

        protected (bool success, decimal withdrawalAssetAmount, string error) ValidateWithdrawlLimit(ApplicationUser user, string asset, decimal amount)
        {
            var withdrawalTotalThisPeriod = user.WithdrawalTotalThisPeriod(_kycSettings);
            var withdrawalAssetAmount = CalculateWithdrawalAssetEquivalent(_logger, _settings, _kycSettings, asset, amount);
            var newWithdrawalTotal = withdrawalTotalThisPeriod + withdrawalAssetAmount;
            var kycLevel = _kycSettings.Levels[0];
            if (user.Kyc != null && user.Kyc.Level < _kycSettings.Levels.Count)
                kycLevel = _kycSettings.Levels[user.Kyc.Level];
            if (decimal.Parse(kycLevel.WithdrawalLimit) <= newWithdrawalTotal)
                return (false, 0,
                    $"Your withdrawal limit is {kycLevel.WithdrawalLimit} {_kycSettings.WithdrawalAsset} equivalent, your current withdrawal total this period ({_kycSettings.WithdrawalPeriod}) is {withdrawalTotalThisPeriod} {_kycSettings.WithdrawalAsset}");

            return (true, withdrawalAssetAmount, null);
        }
    }
}
