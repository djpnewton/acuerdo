using System;
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
        private readonly ILogger _logger;
        private readonly UrlEncoder _urlEncoder;
        private readonly WalletSettings _walletSettings;

        private const string AuthenicatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public ManageController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          IEmailSender emailSender,
          ILogger<ManageController> logger,
          UrlEncoder urlEncoder,
          ApplicationDbContext context,
          IOptions<ExchangeSettings> settings,
          IOptions<WalletSettings> walletSettings) : base(userManager, context, settings)
        {
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _urlEncoder = urlEncoder;
            _walletSettings = walletSettings.Value;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Balance()
        {
            var user = await GetUser(required: true);

            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balances = via.BalanceQuery(user.Exchange.Id);

            var model = new BalanceViewModel
            {
                User = user,
                AssetSettings = _settings.Assets,
                Balances = balances
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Deposits()
        {
            var user = await GetUser(required: true);
            var model = new BaseViewModel
            {
                User = user
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Deposit(string asset)
        {
            var user = await GetUser(required: true);

            // we can only deposit waves for now
            if (asset != "WAVES")
                throw new Exception("Only 'WAVES' support atm");

            var wallet = new WavWallet(_logger, _walletSettings.WavesSeedHex, _walletSettings.WavesWalletFile,
                _walletSettings.Mainnet, new Uri(_walletSettings.WavesNodeUrl));
            var addrs = wallet.GetAddresses(user.Id);
            IAddress addr = null;
            if (addrs.Any())
                addr = addrs.First();
            else
            {
                addr = wallet.NewAddress(user.Id);
                wallet.Save(_walletSettings.WavesWalletFile);
            }

            var model = new DepositViewModel
            {
                User = user,
                Asset = asset,
                DepositAddress = addr.Address,
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> TransactionCheck(string asset, string address)
        {
            var user = await GetUser(required: true);

            // we can only deposit waves for now
            if (asset != "WAVES")
                throw new Exception("Only 'WAVES' support atm");

            // get wallet transactions
            var wallet = new WavWallet(_logger, _walletSettings.WavesSeedHex, _walletSettings.WavesWalletFile,
                _walletSettings.Mainnet, new Uri(_walletSettings.WavesNodeUrl));
            var addrs = wallet.GetAddresses(user.Id);
            IAddress addr = null;
            if (addrs.Any())
                addr = addrs.First();
            else
                addr = wallet.NewAddress(user.Id);
            var txs = wallet.GetAddrTransactions(addr.Address);
            var unackedTxs = wallet.GetAddrUnacknowledgedTransactions(addr.Address);
            BigInteger newDeposits = 0;
            foreach (var tx in unackedTxs)
                if (tx.Direction == WalletDirection.Incomming)
                    newDeposits += tx.Amount;
            var newDepositsHuman = wallet.AmountToString(newDeposits);

            // ack txs and save wallet
            wallet.AcknowledgeTransactions(user.Id, unackedTxs);
            wallet.Save(_walletSettings.WavesWalletFile);

            // register new deposits with the exchange backend
            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            foreach (var tx in unackedTxs)
            {
                if (tx.Direction == WalletDirection.Incomming)
                {
                    var amount = wallet.AmountToString(tx.Amount);
                    var source = new Dictionary<string, object>();
                    source["txid"] = tx.Id;
                    var businessId = wallet.GetNextTxWalletId(user.Id);
                    wallet.SetTxWalletId(user.Id, tx.Id, businessId);
                    wallet.Save(_walletSettings.WavesWalletFile);
                    via.BalanceUpdateQuery(user.Exchange.Id, asset, "deposit", businessId, amount, source);
                }
            } 

            var model = new TransactionCheckViewModel
            {
                User = user,
                Asset = asset,
                AssetSettings = _settings.Assets,
                DepositAddress = addr.Address,
                Wallet = wallet,
                Transactions = txs,
                NewTransactions = unackedTxs,
                NewDeposits = newDeposits,
                NewDepositsHuman = newDepositsHuman,
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Withdrawals()
        {
            var user = await GetUser(required: true);
            var model = new BaseViewModel
            {
                User = user
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Withdraw(string asset)
        {
            var user = await GetUser(required: true);

            // we can only withdraw waves for now
            if (asset != "WAVES")
                throw new Exception("Only 'WAVES' support atm");

            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(user.Exchange.Id, asset);

            var model = new WithdrawViewModel
            {
                User = user,
                Asset = asset,
                BalanceAvailable = balance.Available,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(WithdrawViewModel model)
        {
            var user = await GetUser(required: true);

            // we can only withdraw waves for now
            if (model.Asset != "WAVES")
                throw new Exception("Only 'WAVES' support atm");

            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(user.Exchange.Id, model.Asset);
            model.BalanceAvailable = balance.Available;

            if (ModelState.IsValid)
            {
                var wallet = new WavWallet(_logger, _walletSettings.WavesSeedHex, _walletSettings.WavesWalletFile,
                    _walletSettings.Mainnet, new Uri(_walletSettings.WavesNodeUrl));

                // validate amount
                var amountInt = wallet.StringToAmount(model.Amount.ToString());
                var availableInt = wallet.StringToAmount(balance.Available);
                if (amountInt > availableInt)
                {
                    this.FlashError("Amount must be less then or equal to available balance");
                    return View(model);
                }
                if (amountInt <= 0)
                {
                    this.FlashError("Amount must be greather then or equal to 0");
                    return View(model);
                }

                // validate address
                if (!wallet.ValidateAddress(model.WithdrawalAddress))
                {
                    this.FlashError("Withdrawal address is not valid");
                    return View(model);
                }

                var businessId = wallet.GetNextTxWalletId(_walletSettings.ConsolidatedFundsTag);

                // register withdrawal with the exchange backend
                var negativeAmount = -model.Amount;
                try
                {
                    via.BalanceUpdateQuery(user.Exchange.Id, model.Asset, "withdraw", businessId, negativeAmount.ToString(), null);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Failed to update (withdraw) user balance (xch id: {0}, asset: {1}, businessId: {2}, amount {3}",
                        user.Exchange.Id, model.Asset, businessId, negativeAmount);
                    throw;
                }
                
                // send funds and save wallet
                IEnumerable<string> txids = null;
                var res = wallet.Spend(_walletSettings.ConsolidatedFundsTag, _walletSettings.ConsolidatedFundsTag,
                    model.WithdrawalAddress, amountInt, _walletSettings.WavesFeeMax, _walletSettings.WavesFeeUnit, out txids);
                if (res != WalletError.Success)
                {
                    _logger.LogError("Failed to withdraw funds (wallet error: {0}, asset: {1}, address: {2}, amount: {3}, businessId: {4}",
                        res, model.Asset, model.WithdrawalAddress, amountInt, businessId);
                    this.FlashError(string.Format("Failed to withdraw funds ({0})", res));
                }
                else
                    this.FlashSuccess(string.Format("{0} {1} withdrawn to {2}", model.Amount, model.Asset, model.WithdrawalAddress));
                wallet.SetTxWalletId(_walletSettings.ConsolidatedFundsTag, txids, businessId);
                wallet.Save(_walletSettings.WavesWalletFile);

                return View(model);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetUser(required: true);

            var model = new IndexViewModel
            {
                User = user,
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IndexViewModel model)
        {
            var user = await GetUser(required: true);

            if (!ModelState.IsValid)
            {
                model.User = user;
                return View(model);
            }

            var email = user.Email;
            if (model.Email != email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    throw new ApplicationException($"Unexpected error occurred setting email for user with ID '{user.Id}'.");
                }
            }

            var phoneNumber = user.PhoneNumber;
            if (model.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    throw new ApplicationException($"Unexpected error occurred setting phone number for user with ID '{user.Id}'.");
                }
            }

            StatusMessage = "Your profile has been updated";
            return RedirectToAction(nameof(Index));
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

            return View(nameof(Disable2fa));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2fa()
        {
            var user = await GetUser(required: true);

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
            }

            _logger.LogInformation("User with ID {UserId} has disabled 2fa.", user.Id);
            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        [HttpGet]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await GetUser(required: true);

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
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            _logger.LogInformation("User with ID {UserId} has enabled 2FA with an authenticator app.", user.Id);
            return RedirectToAction(nameof(GenerateRecoveryCodes));
        }

        [HttpGet]
        public IActionResult ResetAuthenticatorWarning()
        {
            return View(nameof(ResetAuthenticator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await GetUser(required: true);

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
                _urlEncoder.Encode("viafront3"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

        #endregion
    }
}
