using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using viafront3.Services;
using viafront3.Models;
using viafront3.Models.TradeViewModels;
using viafront3.Data;
using RestSharp;
using Newtonsoft.Json;
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

        protected async Task PostUserEmailConfirmed(RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, KycSettings kycSettings, ApplicationUser user)
        {
            // add email confirmed role
            var role = await roleManager.FindByNameAsync(Utils.EmailConfirmedRole);
            System.Diagnostics.Debug.Assert(role != null);
            if (!await _userManager.IsInRoleAsync(user, role.Name))
                await _userManager.AddToRoleAsync(user, role.Name);

            // refresh users cookie (so they dont have to log out/ log in)
            await signInManager.RefreshSignInAsync(user);

            // grant email kyc
            for (var i = 0; i < kycSettings.Levels.Count(); i++)
            {
                if (kycSettings.Levels[i].Name == "Email Confirmed")
                {
                    user.UpdateKyc(_logger, _context, kycSettings, i);
                    _context.SaveChanges();
                    break;
                }
            }
        }

        protected string HMacWithSha256(string secret, string message)
        {
            using (var hmac = new HMACSHA256(ASCIIEncoding.ASCII.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(ASCIIEncoding.ASCII.GetBytes(message));
                return Convert.ToBase64String(hash);
            }
        }

        protected viafront3.Models.ApiViewModels.ApiAccountKycRequest CreateKycRequest(KycSettings kycSettings, string applicationUserId)
        {
            // check request does not already exist
            var kycReq = _context.KycRequests.Where(r => r.ApplicationUserId == applicationUserId).FirstOrDefault();
            if (kycReq != null)
                return null;
            // call kyc server to create request
            var token = Utils.CreateToken();
            var client = new RestClient(kycSettings.KycServerUrl);
            var request = new RestRequest("request", Method.POST);
            request.RequestFormat = DataFormat.Json;
            var jsonBody = JsonConvert.SerializeObject(new { api_key = kycSettings.KycServerApiKey, token = token });
            request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);
            var sig = HMacWithSha256(kycSettings.KycServerApiSecret, jsonBody);
            request.AddHeader("X-Signature", sig);
            var response = client.Execute(request);
            if (response.IsSuccessful)
            {
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                if (json.ContainsKey("status"))
                {
                    var status = json["status"];
                    // save to database
                    var date = DateTimeOffset.Now.ToUnixTimeSeconds();
                    kycReq = new KycRequest { ApplicationUserId = applicationUserId, Date = date, Token = token };
                    _context.KycRequests.Add(kycReq);
                    _context.SaveChanges();
                    // return to user
                    var model = new viafront3.Models.ApiViewModels.ApiAccountKycRequest
                    {
                        Token = token,
                        ServiceUrl = $"{kycSettings.KycServerUrl}/request/{token}",
                        Status = status,
                    };
                    return model;
                }
            }
            return null;
        }

        protected async Task<viafront3.Models.ApiViewModels.ApiAccountKycRequest> CheckKycRequest(KycSettings kycSettings, string applicationUserId, string token)
        {
            var client = new RestClient(kycSettings.KycServerUrl);
            var request = new RestRequest("status", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(new { token = token });
            var response = client.Execute(request);
            if (response.IsSuccessful)
            {
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                if (json.ContainsKey("status"))
                {
                    var status = json["status"];
                    // update kyc level if complete
                    if (status == "completed")
                    {
                        var newLevel = 2;
                        var user = await _userManager.FindByIdAsync(applicationUserId);
                        if (user == null)
                            return null;
                        var userKyc = user.Kyc;
                        if (userKyc == null)
                        {
                            userKyc = new Kyc { ApplicationUserId = user.Id, Level = newLevel };
                            _context.Kycs.Add(userKyc);
                        }
                        else if (userKyc.Level < newLevel)
                        {

                            userKyc.Level = newLevel;
                            _context.Kycs.Update(userKyc);
                        }
                        _context.SaveChanges();
                    }
                    // return to user
                    var model = new viafront3.Models.ApiViewModels.ApiAccountKycRequest
                    {
                        Token = token,
                        ServiceUrl = $"{kycSettings.KycServerUrl}/request/{token}",
                        Status = status,
                    };
                    return model;
                }
            }
            return null;
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

        protected decimal CalculateWithdrawalAssetEquivalent(ILogger logger, KycSettings kyc, string asset, decimal amount)
        {
            if (asset == kyc.WithdrawalAsset)
                return amount;

            foreach (var market in _settings.Markets.Keys)
            {
                if (market.StartsWith(asset) && market.EndsWith(kyc.WithdrawalAsset))
                {
                    try
                    {
                        //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
                        var via = new ViaJsonRpc(_settings.AccessHttpUrl);
                        var price = via.MarketPriceQuery(market);
                        var priceDec = decimal.Parse(price);
                        if (priceDec <= 0)
                            continue;
                        return amount * priceDec;
                    }
                    catch (ViaJsonException ex)
                    {
                        _logger.LogError(ex, $"Error getting market price for asset '{market}'");
                    }
                }
            };

            if (kyc.WithdrawalAssetBaseRates.ContainsKey(asset))
                return amount * kyc.WithdrawalAssetBaseRates[asset];

            logger.LogError($"no price found for asset {asset}");
            throw new Exception($"no price found for asset {asset}");
        }

        protected (bool success, decimal withdrawalAssetAmount, string error) ValidateWithdrawlLimit(ApplicationUser user, string asset, decimal amount)
        {
            var withdrawalTotalThisPeriod = user.WithdrawalTotalThisPeriod(_kycSettings);
            var withdrawalAssetAmount = CalculateWithdrawalAssetEquivalent(_logger, _kycSettings, asset, amount);
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
