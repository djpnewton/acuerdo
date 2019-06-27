using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.AccountViewModels;
using viafront3.Data;
using viafront3.Services;

namespace viafront3.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : BaseSettingsController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly KycSettings _kycSettings;
        private readonly ITripwire _tripwire;

        public AccountController(
            ILogger<AccountController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            IOptions<ExchangeSettings> settings,
            RoleManager<IdentityRole> roleManager,
            IOptions<KycSettings> kycSettings,
            ITripwire tripwire) : base(logger, userManager, context, settings)
        {
            _signInManager = signInManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _kycSettings = kycSettings.Value;
            _tripwire = tripwire;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { User = null });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            model.User = null;

            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    await _tripwire.RegisterEvent(TripwireEventType.Login);
                    this.FlashSuccess("Logged in");
                    return RedirectToLocal(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    await _tripwire.RegisterEvent(TripwireEventType.LoginAttempt);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var model = new LoginWith2faViewModel { User = null, RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        {
            model.User = null;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var rememberMachine = false;
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, rememberMachine);

            if (result.Succeeded)
            {
                await _tripwire.RegisterEvent(TripwireEventType.Login);
                _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                await _tripwire.RegisterEvent(TripwireEventType.LoginAttempt);
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                await _tripwire.RegisterEvent(TripwireEventType.LoginAttempt);
                _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginWithRecoveryCodeViewModel{ User = null });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
        {
            model.User = null;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                await _tripwire.RegisterEvent(TripwireEventType.Login);
                _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                await _tripwire.RegisterEvent(TripwireEventType.LoginAttempt);
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                await _tripwire.RegisterEvent(TripwireEventType.LoginAttempt);
                _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return View(new LoginWithRecoveryCodeViewModel{ User = null });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View(BaseViewModel());
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel { User = null });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                (var result, var user) = await CreateUser(_signInManager, _emailSender, model.Email, model.Email, model.Password, sendEmail: true, signIn: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            model.User = null;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            this.FlashSuccess("Logged out");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                await PostUserEmailConfirmed(_roleManager, _signInManager, _kycSettings, user);

                return View(BaseViewModel());
            }

            this.FlashError("Invalid email code");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ConfirmAccountCreation(string token)
        {
            if (token == null)
                return RedirectToAction(nameof(HomeController.Index), "Home");
            var accountReq = _context.AccountCreationRequests.SingleOrDefault(r => r.Token == token);
            if (accountReq != null && !accountReq.Completed)
            {
                var model = new ConfirmAccountCreationViewModel { Token = token };
                return View(model);
            }

            this.FlashError("Invalid account code");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmAccountCreation(ConfirmAccountCreationViewModel model)
        {
            if (model.Token == null)
                return RedirectToAction(nameof(HomeController.Index), "Home");
            var accountReq = _context.AccountCreationRequests.SingleOrDefault(r => r.Token == model.Token);
            if (accountReq != null && !accountReq.Completed)
            {
                // create new user
                (var result, var user) = await CreateUser(_signInManager, _emailSender, accountReq.RequestedEmail, accountReq.RequestedEmail, model.Password, sendEmail: false, signIn: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with api.");

                    accountReq.ApplicationUserId = user.Id;
                    accountReq.Completed = true;
                    _context.AccountCreationRequests.Update(accountReq);
                    _context.SaveChanges();

                    this.FlashSuccess("Account created");
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
                AddErrors(result);

                // If we got this far, something failed, redisplay form
                model.User = null;
                return View(model);
            }

            this.FlashError("Invalid account code");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmApiKeyCreation(string token)
        {
            if (token == null)
                return RedirectToAction(nameof(HomeController.Index), "Home");
            var apiKeyReq = _context.ApiKeyCreationRequests.SingleOrDefault(r => r.Token == token);
            if (apiKeyReq != null && !apiKeyReq.Completed)
            {
                var user = await _userManager.FindByIdAsync(apiKeyReq.ApplicationUserId);
                var model = new ConfirmApiKeyCreationViewModel { Token = token, DeviceName = apiKeyReq.RequestedDeviceName, TwoFactorRequired = user.TwoFactorEnabled };
                return View(model);
            }

            this.FlashError("Invalid API KEY code");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmApiKeyCreation(ConfirmApiKeyCreationViewModel model)
        {
            if (model.Token == null)
                return RedirectToAction(nameof(HomeController.Index), "Home");
            var apiKeyReq = _context.ApiKeyCreationRequests.SingleOrDefault(r => r.Token == model.Token);
            if (apiKeyReq != null && !apiKeyReq.Completed)
            {
                // check 2fa authentication
                var user = await _userManager.FindByIdAsync(apiKeyReq.ApplicationUserId);
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

                _logger.LogInformation("User confrimed a new apikey with api.");

                apiKeyReq.Completed = true;
                _context.ApiKeyCreationRequests.Update(apiKeyReq);
                _context.SaveChanges();

                this.FlashSuccess($"API KEY ({model.DeviceName}) confirmed");
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            this.FlashError("Invalid API KEY code");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel { User = null });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            model.User = null;
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View(BaseViewModel());
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { User = null, Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _tripwire.RegisterEvent(TripwireEventType.ResetPasswordAttempt);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View(new ResetPasswordViewModel{ User = null });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View(BaseViewModel());
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View(BaseViewModel());
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
