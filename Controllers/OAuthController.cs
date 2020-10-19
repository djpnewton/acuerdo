using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.ApiViewModels;
using viafront3.Data;
using viafront3.Services;

namespace viafront3.Controllers
{
    [Route("oauth/v1/[action]")]
    public class OAuthController : BaseApiController
    {
        private const string USERINFO = "userinfo";
        private const string BALANCE = "balance";
        private const string KYC = "kyc";
        private readonly Dictionary<string, string> SCOPES = new Dictionary<string, string> { { USERINFO, "basic user info (name, email)" }, { BALANCE, "account balance" }, { KYC, "KYC status" } };

        private static readonly Dictionary<string, OAuthRequestViewModel> _requests = new Dictionary<string, OAuthRequestViewModel>();
 
        private readonly OAuthSettings _oauthSettings;

        public OAuthController(
            ILogger<OAuthController> logger,
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
            IDepositsWithdrawals depositsWithdrawals,
            IOptions<OAuthSettings> oauthSettings) : base(logger, userManager, signInManager, context, emailSender, settings, apiSettings, roleManager, kycSettings, walletProvider, tripwire, userLocks, fiatSettings, broker, depositsWithdrawals)
        {
            _oauthSettings = oauthSettings.Value;
        }

        string CreateToken(int len)
        {
            using var rng = new RNGCryptoServiceProvider();
            var bytes = new byte[len];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        //
        // OAuth provider endpoints
        //

        [Authorize]
        public async Task<IActionResult> Auth([FromQuery]OAuthRequestViewModel model)
        {
            var user = await GetUser(required: true);

            // validate response_type
            if (model.ResponseType != "code")
                return BadRequest("invalid response_type");
            // valid client_id
            if (!_oauthSettings.ClientIds.ContainsKey(model.ClientId))
                return BadRequest("invalid client_id");
            // valid scope
            if (string.IsNullOrEmpty(model.Scope))
                return BadRequest("invalid scope");
            foreach (var scope in model.Scope.Split(' '))
                if (!SCOPES.ContainsKey(scope))
                    return BadRequest("invalid scope");

            model.AvailableScopes = SCOPES;
            model.Allow = false;
            model.ClientIds = _oauthSettings.ClientIds;
            model.Code = CreateToken(8);
            model.Expiry = DateTimeOffset.Now.AddMinutes(5).ToUnixTimeSeconds();
            model.User = user;
            _requests[model.Code] = model;

            _logger.LogDebug("OAuth authorization request, user: {0}, scope: {1}, code: {2}", user.Email, model.Scope, model.Code);

            return View(model);
        }

        [HttpPost]
        public IActionResult Deny([FromForm] string code)
        {
            _logger.LogDebug("OAuth deny, code: {0}", code);

            if (!_requests.ContainsKey(code))
                return BadRequest("invalid code");
            var req = _requests[code];
            _requests.Remove(code);
            var uri = string.Format("{0}?error=access_denied&state={1}", req.RedirectUri, Uri.EscapeDataString(req.State));
            return Redirect(uri);
        }

        [HttpPost]
        public IActionResult Allow([FromForm] string code)
        {
            _logger.LogDebug("OAuth allow, code: {0}", code);

            if (!_requests.ContainsKey(code))
                return BadRequest("invalid code");
            var req = _requests[code];
            if (req.Allow)
                return BadRequest("already allowed");
            if (req.Expiry < DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                _requests.Remove(code);
                return BadRequest("expired");
            }
            req.Allow = true;
            var uri = string.Format("{0}?code={1}&state={2}", req.RedirectUri, Uri.EscapeDataString(req.Code), Uri.EscapeDataString(req.State));
            return Redirect(uri);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public ActionResult<OAuthTokenViewModel> Token([FromForm] OAuthTokenRequestViewModel model)
        {
            _logger.LogDebug("OAuth claim token, code: {0}, grant type: {1}, clientid: {2}", model.Code, model.GrantType, model.ClientId);

            if (model.GrantType != "authorization_code")
                return BadRequest(new OAuthTokenErrorViewModel { Error = string.Format("invalid grant type ({0})", model.GrantType) });
            if (!_requests.ContainsKey(model.Code))
                return BadRequest(new OAuthTokenErrorViewModel { Error = "invalid code" });
            var req = _requests[model.Code];
            if (req.Expiry < DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                _requests.Remove(model.Code);
                return BadRequest(new OAuthTokenErrorViewModel { Error = "expired" });
            }
            if (!req.Allow)
                return BadRequest(new OAuthTokenErrorViewModel { Error = "invalid request" });
            if (req.ClientId != model.ClientId || model.ClientSecret != _oauthSettings.ClientIds[model.ClientId].Secret)
                return BadRequest(new OAuthTokenErrorViewModel { Error = "invalid client id" });
            if (req.RedirectUri != model.RedirectUri)
                return BadRequest(new OAuthTokenErrorViewModel { Error = "invalid redirect uri" });
            var expiryIn = 60 * 60 * 24 * 7;
            var expiryAt = DateTimeOffset.Now.AddSeconds(expiryIn).ToUnixTimeSeconds();
            var token = new OAuthTokenViewModel { AccessToken = CreateToken(16), ExpiresIn = expiryIn, ExpiresAt = expiryAt, Scope = req.Scope };
            _context.OAuthTokens.Add(new OAuthToken { ApplicationUserId = req.User.Id, AccessToken = token.AccessToken, Date = DateTimeOffset.Now.ToUnixTimeSeconds(), ExpiresAt = token.ExpiresAt, ExpiresIn = token.ExpiresIn, Scope = token.Scope });
            _context.SaveChanges();
            return token;
        }

        //
        // OAuth authorized resources
        //

        OAuthToken CheckAuth()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return null;
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
                return null;
            if (!authHeader.StartsWith("Bearer "))
                return null;
            var accessToken = authHeader.Substring(7);
            var token = _context.OAuthTokens.Where(t => t.AccessToken == accessToken).FirstOrDefault();
            if (token == null)
                return null;
            if (token.ExpiresAt < DateTimeOffset.Now.ToUnixTimeSeconds())
                return null;

            return token;
        }

        bool CheckScope(string allowedScopes, string requestedScope)
        {
            foreach (var scope in allowedScopes.Split(' '))
                if (scope == requestedScope)
                    return true;
            return false;
        }

        public IActionResult Validate()
        {
            if (CheckAuth() == null)
                return Unauthorized();
            return Ok();
        }

        [Produces("application/json")]
        public async Task<ActionResult<Dictionary<string, string>>> UserInfo()
        {
            var token = CheckAuth();
            if (token == null)
                return Unauthorized();
            if (!CheckScope(token.Scope, USERINFO))
                return Unauthorized();
            var user = await _userManager.FindByIdAsync(token.ApplicationUserId);
            if (user == null)
                return BadRequest(INTERNAL_ERROR);
            return new Dictionary<string, string> { { "email", user.Email } };
        }

        [Produces("application/json")]
        public ActionResult<ApiAccountBalance> AccountBalance()
        {
            var token = CheckAuth();
            if (token == null)
                return Unauthorized();
            if (!CheckScope(token.Scope, BALANCE))
                return Unauthorized();
            return AccountBalance(token.ApplicationUserId);
        }

        [Produces("application/json")]
        public async Task<ActionResult<ApiAccountKyc>> AccountKyc()
        {
            var token = CheckAuth();
            if (token == null)
                return Unauthorized();
            if (!CheckScope(token.Scope, KYC))
                return Unauthorized();
            return await AccountKyc(token.ApplicationUserId);
        }
    }
}
