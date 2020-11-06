﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Numerics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.ManageViewModels;
using viafront3.Services;
using viafront3.Data;
using via_jsonrpc;
using xchwallet;

namespace viafront3.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : BaseSettingsController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly UrlEncoder _urlEncoder;
        private readonly GeneralSettings _genSettings;
        private readonly KycSettings _kycSettings;
        private readonly IWalletProvider _walletProvider;

        private const string AuthenicatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public ManageController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          IEmailSender emailSender,
          ILogger<ManageController> logger,
          UrlEncoder urlEncoder,
          ApplicationDbContext context,
          IOptions<ExchangeSettings> settings,
          IOptions<GeneralSettings> gen,
          IOptions<KycSettings> kyc,
          IWalletProvider walletProvider) : base(logger, userManager, context, settings)
        {
            _signInManager = signInManager;
            _emailSender = emailSender;
            _urlEncoder = urlEncoder;
            _genSettings = gen.Value;
            _kycSettings = kyc.Value;
            _walletProvider = walletProvider;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetUser(required: true);

            var model = new IndexViewModel
            {
                User = user,
                Username = user.UserName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendVerificationEmail(IndexViewModel model)
        {
            var user = await GetUser(required: true);

            if (!ModelState.IsValid)
            {
                model.User = user;
                return View(model);
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
            var email = user.Email;
            await _emailSender.SendEmailConfirmationAsync(email, callbackUrl);

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangeEmail()
        {
            var user = await GetUser(required: true);

            var model = new ChangeEmailViewModel { User = user, TwoFactorRequired = user.TwoFactorEnabled, StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            var user = await GetUser(required: true);
            model.User = user;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // check 2fa authentication
            if (user.TwoFactorEnabled)
            {
                if (model.TwoFactorCode == null)
                    model.TwoFactorCode = "";
                var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
                if (!await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, authenticatorCode))
                {
                    this.FlashError($"Invalid authenticator code");
                    return View(model);
                }
            }

            var existingUser = await _userManager.FindByEmailAsync(model.NewEmail);
            if (existingUser == null)
            {
                await _emailSender.SendNewEmailChangeAsync(model.NewEmail);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);
                var callbackUrl = Url.EmailChangeLink(user.Email, model.NewEmail, code, Request.Scheme);
                await _emailSender.SendOldEmailChangeAsync(user.Email, model.NewEmail, callbackUrl);
                StatusMessage = "Email change request sent.";
            }
            else
                StatusMessage = "Email not available.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ConfirmEmailChange([FromQuery] string code, [FromQuery] string oldEmail, [FromQuery] string newEmail)
        {
            if (code == null || oldEmail == null || newEmail == null)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var model = new ConfirmEmailChangeViewModel { Code = code, OldEmail = oldEmail, NewEmail = newEmail };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailChange(ConfirmEmailChangeViewModel model)
        {
            if (model.Code == null || model.OldEmail == null || model.NewEmail == null)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var user = await _userManager.FindByEmailAsync(model.OldEmail);
            if (user == null)
            {
                this.FlashError("Invalid email code");
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            var existingUser = await _userManager.FindByEmailAsync(model.NewEmail);
            if (existingUser != null)
            {
                this.FlashError("Invalid email code");
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            // update user name
            user.UserName = model.NewEmail; // ChangeEmailAsync should persist this change if successful (it calls UpdateUserAsync)
            // change email
            var changeEmailResult = await _userManager.ChangeEmailAsync(user, model.NewEmail, model.Code);
            if (!changeEmailResult.Succeeded)
            {
                this.FlashError("Invalid email code");
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            this.FlashSuccess("Changed email");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await GetUser(required: true);

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToAction(nameof(SetPassword));
            }

            var model = new ChangePasswordViewModel { User = user, StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await GetUser(required: true);
            model.User = user;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        public async Task<IActionResult> SetPassword()
        {
            var user = await GetUser(required: true);

            var hasPassword = await _userManager.HasPasswordAsync(user);

            if (hasPassword)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            var model = new SetPasswordViewModel { User = user, StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            var user = await GetUser(required: true);
            model.User = user;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                AddErrors(addPasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "Your password has been set.";

            return RedirectToAction(nameof(SetPassword));
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLogins()
        {
            var user = await GetUser(required: true);

            var model = new ExternalLoginsViewModel { User = user, CurrentLogins = await _userManager.GetLoginsAsync(user) };
            model.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => model.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            model.ShowRemoveButton = await _userManager.HasPasswordAsync(user) || model.CurrentLogins.Count > 1;
            model.StatusMessage = StatusMessage;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkLogin(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback()
        {
            var user = await GetUser(required: true);

            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                throw new ApplicationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            }

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occurred adding external login for user with ID '{user.Id}'.");
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            StatusMessage = "The external login was added.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model)
        {
            var user = await GetUser(required: true);

            var result = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occurred removing external login for user with ID '{user.Id}'.");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "The external login was removed.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpGet]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await GetUser(required: true);

            var model = new TwoFactorAuthenticationViewModel
            {
                User = user,
                HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null,
                Is2faEnabled = user.TwoFactorEnabled,
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Disable2faWarning()
        {
            var user = await GetUser(required: true);

            if (!user.TwoFactorEnabled)
            {
                throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
            }

            return View(nameof(Disable2fa), new TwoFactorRequiredViewModel { User = user, TwoFactorRequired = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2fa(TwoFactorRequiredViewModel model)
        {
            var user = await GetUser(required: true);

            // check 2fa authentication
            if (user.TwoFactorEnabled)
            {
                if (model.TwoFactorCode == null)
                    model.TwoFactorCode = "";
                var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
                if (!await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, authenticatorCode))
                {
                    this.FlashError($"Invalid authenticator code");
                    return RedirectToAction(nameof(TwoFactorAuthentication));
                }
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
            }
            // we auto reset when 2fa is disabled (we dont want the user manually handling 2fa key resets)
            await _userManager.ResetAuthenticatorKeyAsync(user);

            _logger.LogInformation("User with ID {UserId} has disabled 2fa.", user.Id);
            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        [HttpGet]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await GetUser(required: true);

            if (user.TwoFactorEnabled)
                return Redirect("/");

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var model = new EnableAuthenticatorViewModel
            {
                User = user,
                SharedKey = FormatKey(unformattedKey),
                AuthenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
        {
            var user = await GetUser(required: true);
            model.User = user;

            if (user.TwoFactorEnabled)
                return Redirect("/");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Strip spaces and hypens
            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("model.Code", "Verification code is invalid.");

                var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
                model.SharedKey = FormatKey(unformattedKey);
                model.AuthenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey);
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            _logger.LogInformation("User with ID {UserId} has enabled 2FA with an authenticator app.", user.Id);
            //return RedirectToAction(nameof(GenerateRecoveryCodes));
            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        /*
        [HttpGet]
        public async Task<IActionResult> ResetAuthenticatorWarning()
        {
            var user = await GetUser(required: true);
            if (user.TwoFactorEnabled)
                return Redirect("/");

            return View(nameof(ResetAuthenticator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await GetUser(required: true);

            if (user.TwoFactorEnabled)
                return Redirect("/");

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation("User with id '{UserId}' has reset their authentication app key.", user.Id);

            return RedirectToAction(nameof(EnableAuthenticator));
        }

        [HttpGet]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await GetUser(required: true);

            if (!user.TwoFactorEnabled)
            {
                throw new ApplicationException($"Cannot generate recovery codes for user with ID '{user.Id}' as they do not have 2FA enabled.");
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            var model = new GenerateRecoveryCodesViewModel { User = user, RecoveryCodes = recoveryCodes.ToArray() };

            _logger.LogInformation("User with ID {UserId} has generated new 2FA recovery codes.", user.Id);

            return View(model);
        }
        */

        [HttpGet]
        public async Task<IActionResult> Api()
        {
            var user = await GetUser(required: true);

            var model = new ApiViewModel
            {
                User = user,
                ApiKeys = _context.ApiKeys.Where(d => d.ApplicationUserId == user.Id).ToList(),
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApiDelete(ApiViewModel model)
        {
            var user = await GetUser(required: true);

            if (model.DeleteApiKey != null)
            {
                var apikey = _context.ApiKeys.SingleOrDefault(d => d.Key == model.DeleteApiKey && d.ApplicationUserId == user.Id);
                if (apikey != null)
                {
                    _context.ApiKeys.Remove(apikey);
                    _context.SaveChanges();
                    this.FlashSuccess($"Deleted API KEY ({apikey.Name})");
                    return RedirectToAction(nameof(Api));
                }
            }

            this.FlashError($"Failed to delete API KEY");
            return RedirectToAction(nameof(Api));
        }

        [HttpGet]
        public async Task<IActionResult> ApiCreate()
        {
            var user = await GetUser(required: true);

            var model = new ApiCreateViewModel { User = user, TwoFactorRequired = user.TwoFactorEnabled };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApiCreate(ApiCreateViewModel model)
        {
            var user = await GetUser(required: true);

            // check 2fa authentication
            if (user.TwoFactorEnabled)
            {
                if (model.TwoFactorCode == null)
                    model.TwoFactorCode = "";
                var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
                if (!await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, authenticatorCode))
                {
                    this.FlashError($"Invalid authenticator code");
                    model.TwoFactorRequired = true;
                    return View(model);     
                }
            }

            var apikey = Utils.CreateApiKey(user, -1, -1, model.DeviceName);
            _context.ApiKeys.Add(apikey);
            _context.SaveChanges();
            this.FlashSuccess($"Created API KEY ({apikey.Name} - Key: {apikey.Key} - Secret: {apikey.Secret})");
            return RedirectToAction(nameof(Api));
        }

        [HttpGet]
        public async Task<IActionResult> Kyc()
        {
            var user = await GetUser(required: true);

            // get kyc request url and check the status
            string kycRequestUrl = null;
            string kycRequestStatus = null;
            var kycRequest = _context.KycRequests.Where(r => r.ApplicationUserId == user.Id).OrderByDescending(r => r.Date).FirstOrDefault();
            if (kycRequest != null)
            {
                kycRequestUrl = $"{_kycSettings.KycServerUrl}/request/{kycRequest.Token}";
                var _model = await RestUtils.CheckKycRequest(_logger, _context, _userManager, _kycSettings, kycRequest.Token);
                if (_model != null)
                    kycRequestStatus = _model.Status;
            }
            // get users kyc level
            var levelNum = user.Kyc != null ? user.Kyc.Level : 0;
            KycLevel level = null;
            if (_kycSettings.Levels.Count > levelNum)
                level = _kycSettings.Levels[levelNum];
            var withdrawalTotalThisPeriod = user.WithdrawalTotalThisPeriod(_kycSettings);
            // convert withdrawal amount to string
            var withdrawalTotalThisPeriodString = _walletProvider.AmountToString(_kycSettings.WithdrawalAsset, withdrawalTotalThisPeriod);
            if (withdrawalTotalThisPeriodString == null)
                withdrawalTotalThisPeriodString = withdrawalTotalThisPeriod.ToString();

            var model = new KycViewModel
            {
                User = user,
                LevelNum = levelNum,
                Level = level,
                WithdrawalTotalThisPeriod = withdrawalTotalThisPeriodString,
                KycSettings = _kycSettings,
                KycRequestUrl = kycRequestUrl,
                KycRequestStatus = kycRequestStatus,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> KycUpgrade()
        {
            if (!_kycSettings.KycServerEnabled)
            {
                _logger.LogError("kyc server not enabled");
                return NotFound();
            }
            var user = await GetUser(required: true);

            var _model = RestUtils.CreateKycRequest(_logger, _context, _userManager, _kycSettings, user.Id, user.Email, Url, Request.Scheme);
            if (_model != null)
                this.FlashSuccess("Created KYC upgrade request");

            return RedirectToAction(nameof(Kyc));
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenicatorUriFormat,
                _urlEncoder.Encode(_genSettings.SiteName),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

        #endregion
    }
}
