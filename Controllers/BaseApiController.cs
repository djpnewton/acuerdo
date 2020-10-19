using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.ApiViewModels;
using viafront3.Data;
using viafront3.Services;
using via_jsonrpc;

namespace viafront3.Controllers
{
    public class BaseApiController : BaseWalletController
    {
        protected const string EXPIRED = "expired";
        protected const string INTERNAL_ERROR = "internal error";
        protected const string AUTHENTICATION_FAILED = "authentication failed";
        protected const string OLD_NONCE = "old nonce";
        protected const string KYC_SERVICE_NOT_AVAILABLE = "kyc service not available";
        protected const string FIAT_PAYMENT_SERVICE_NOT_AVAILABLE = "fiat payment service not available";
        protected const string INVALID_MARKET = "invalid market";
        protected const string INSUFFICIENT_BALANCE = "insufficient balance";
        protected const string INSUFFICIENT_LIQUIDITY = "insufficient liquidity";
        protected const string INVALID_ORDER = "invalid order";
        protected const string AMOUNT_TOO_LOW = "amount too low (minumum is: {0})";
        protected const string INVALID_RECIPIENT = "invalid recipient";
        protected const string INVALID_INTERVAL = "invalid interval";
        protected const string INVALID_AMOUNT = "invalid amount";
        protected const string INVALID_BROKER_STATUS = "invalid broker status";

        protected readonly SignInManager<ApplicationUser> _signInManager;
        protected readonly IEmailSender _emailSender;
        protected readonly RoleManager<IdentityRole> _roleManager;
        protected readonly ApiSettings _apiSettings;
        protected readonly ITripwire _tripwire;
        protected readonly IUserLocks _userLocks;
        protected readonly FiatProcessorSettings _fiatSettings;
        protected readonly IBroker _broker;
        protected readonly IDepositsWithdrawals _depositsWithdrawals;

        public BaseApiController(
            ILogger<BaseApiController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            IOptions<ExchangeSettings> settings,
            IOptions<ApiSettings> apiSettings,
            RoleManager<IdentityRole> roleManager,
            IOptions<KycSettings> kycSettings,
            IWalletProvider walletProvider,
            ITripwire tripwire,
            IUserLocks userLocks,
            IOptions<FiatProcessorSettings> fiatSettings,
            IBroker broker,
            IDepositsWithdrawals depositsWithdrawals) : base(logger, userManager, context, settings, walletProvider, kycSettings)
        {
            _signInManager = signInManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _apiSettings = apiSettings.Value;
            _tripwire = tripwire;
            _userLocks = userLocks;
            _fiatSettings = fiatSettings.Value;
            _broker = broker;
            _depositsWithdrawals = depositsWithdrawals;
        }

        protected ActionResult<ApiAccountBalance> AccountBalance(string userId) 
        {
            var xch = _context.Exchange.SingleOrDefault(x => x.ApplicationUserId == userId);
            if (xch == null)
                return BadRequest(INTERNAL_ERROR); 
            try
            {
                //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
                var via = new ViaJsonRpc(_settings.AccessHttpUrl);
                var balances = Utils.GetUsedBalances(_settings, via, xch);
                var model = new ApiAccountBalance { Assets = balances };
                return model;
            }
            catch (ViaJsonException ex)
            {
                _logger.LogError(ex, "error in balance query");
                return BadRequest(INTERNAL_ERROR);
            }
        }

        protected async Task<ActionResult<ApiAccountKyc>> AccountKyc(string userId) 
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest(INTERNAL_ERROR);
            var level = user.Kyc != null ? user.Kyc.Level : 0;
            KycLevel kycLevel = null;
            if (level < _kycSettings.Levels.Count())
                kycLevel = _kycSettings.Levels[level];
            else
                return BadRequest(INTERNAL_ERROR);
            var withdrawalTotalThisPeriod = user.WithdrawalTotalThisPeriod(_kycSettings);
            var withdrawalTotalThisPeriodString = _walletProvider.AmountToString(_kycSettings.WithdrawalAsset, withdrawalTotalThisPeriod);
            if (withdrawalTotalThisPeriodString == null)
                withdrawalTotalThisPeriodString = withdrawalTotalThisPeriod.ToString();
            var model = new ApiAccountKyc
            {
                Level = level.ToString(),
                WithdrawalLimit = kycLevel.WithdrawalLimit,
                WithdrawalAsset = _kycSettings.WithdrawalAsset,
                WithdrawalPeriod = _kycSettings.WithdrawalPeriod.ToString(),
                WithdrawalTotal = withdrawalTotalThisPeriodString,

            };
            return model;
        }
    }
}