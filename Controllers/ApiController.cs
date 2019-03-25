using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.ApiViewModels;
using viafront3.Data;
using viafront3.Services;

namespace viafront3.Controllers
{
    [Produces("application/json")]
    [Route("api/v1/[action]")]
    [ApiController]
    public class ApiController : BaseSettingsController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger _logger;
        private readonly ApiSettings _apiSettings;

        public ApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IOptions<ExchangeSettings> settings,
            IOptions<ApiSettings> apiSettings,
            RoleManager<IdentityRole> roleManager) : base(userManager, context, settings)
        {
            _signInManager = signInManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _logger = logger;
            _apiSettings = apiSettings.Value;
        }

        [HttpPost]
        public async Task<ActionResult<ApiToken>> AccountCreate([FromBody] ApiAccountCreate req) 
        {
            var existingUser = await _userManager.FindByEmailAsync(req.Email);
            if (existingUser != null)
                return new ApiToken { Token = Utils.CreateToken() }; // reply with fake token if user already exists (so robot cant test for user emails)
            var date = DateTimeOffset.Now.ToUnixTimeSeconds();
            var token = Utils.CreateToken();
            var secret = Utils.CreateToken(32);
            var accountReq = new AccountCreationRequest { ApplicationUserId = null, Date = date, Token = token, Secret = secret, Completed = false,
                RequestedEmail = req.Email, RequestedDeviceName = req.DeviceName };
            _context.AccountCreationRequests.Add(accountReq);
            var callbackUrl = Url.AccountCreationConfirmationLink(token, Request.Scheme);
            await _emailSender.SendEmailApiAccountCreationRequest(req.Email, _apiSettings.CreationExpiryMinutes, callbackUrl);
            _context.SaveChanges();
            return new ApiToken { Token = token };
        }

        [HttpPost]
        public async Task<ActionResult<ApiDevice>> AccountCreateStatus([FromBody] ApiToken token) 
        {
            var accountReq = _context.AccountCreationRequests.SingleOrDefault(r => r.Token == token.Token);
            if (accountReq == null)
                return new ApiDevice { Completed = false }; // fake reply if token not found (so robot cant test for user emails)
            if (!accountReq.Completed)
                return new ApiDevice { Completed = false };
            var user = await _userManager.FindByEmailAsync(accountReq.RequestedEmail);
            if (user == null)
                return new ApiDevice { Completed = false };
            // check expiry
            if (accountReq.Date + _apiSettings.CreationExpiryMinutes * 60 < DateTimeOffset.Now.ToUnixTimeSeconds())
                return BadRequest("expired");
            // create new device
            var deviceKey = Utils.CreateToken();
            var deviceSecret = Utils.CreateToken(32);
            var device = new Device
            { 
                ApplicationUserId = accountReq.ApplicationUserId,
                CreationRequestId = accountReq.Id,
                Name = accountReq.RequestedDeviceName,
                DeviceKey = deviceKey,
                DeviceSecret = deviceSecret,
                Nonce = 0
            };
            _context.Devices.Add(device);
            // save db and return connection details
            _context.SaveChanges();
            return new ApiDevice { Completed = true, DeviceKey = device.DeviceKey, DeviceSecret = device.DeviceSecret };
        }

        [HttpPost]
        public IActionResult AccountCreateCancel([FromBody] ApiToken token) 
        {
            var accountReq = _context.AccountCreationRequests.SingleOrDefault(r => r.Token == token.Token);
            if (accountReq == null)
                return Ok(); // fake reply if token not found (so robot cant test for user emails)
            _context.AccountCreationRequests.Remove(accountReq);
            _context.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<ApiToken>> DeviceCreate([FromBody] ApiDeviceCreate req) 
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return new ApiToken { Token = Utils.CreateToken() }; // reply with fake token if user already exists (so robot cant test for user emails)
            var date = DateTimeOffset.Now.ToUnixTimeSeconds();
            var token = Utils.CreateToken();
            var secret = Utils.CreateToken(32);
            var accountReq = new DeviceCreationRequest { ApplicationUserId = user.Id, Date = date, Token = token, Secret = secret, Completed = false,
                RequestedDeviceName = req.DeviceName };
            _context.DeviceCreationRequests.Add(accountReq);
            var callbackUrl = Url.DeviceCreationConfirmationLink(token, Request.Scheme);
            await _emailSender.SendEmailApiDeviceCreationRequest(req.Email, _apiSettings.CreationExpiryMinutes, callbackUrl);
            _context.SaveChanges();
            return new ApiToken { Token = token };       
        }

        [HttpPost]
        public async Task<ActionResult<ApiDevice>> DeviceCreateStatus([FromBody] ApiToken token) 
        {
            var deviceReq = _context.DeviceCreationRequests.SingleOrDefault(r => r.Token == token.Token);
            if (deviceReq == null)
                return NotFound();
            if (!deviceReq.Completed)
                return new ApiDevice { Completed = false };
            var device = _context.Devices.SingleOrDefault(d => d.CreationRequestId == deviceReq.Id);
            if (device != null)
                return new ApiDevice { Completed = true };
            // check expiry
            if (deviceReq.Date + _apiSettings.CreationExpiryMinutes * 60 < DateTimeOffset.Now.ToUnixTimeSeconds())
                return BadRequest("expired");
            // get user
            var user = await _userManager.FindByIdAsync(deviceReq.ApplicationUserId);
            if (user == null)
                return BadRequest();
            // create new device
            device = Utils.CreateDevice(user, deviceReq.Id, deviceReq.RequestedDeviceName);
            _context.Devices.Add(device);
            // save db and return connection details
            _context.SaveChanges();
            return new ApiDevice { Completed = true, DeviceKey = device.DeviceKey, DeviceSecret = device.DeviceSecret };      
        }

        [HttpPost]
        public IActionResult DeviceCreateCancel([FromBody] ApiToken token) 
        {
            var deviceReq = _context.DeviceCreationRequests.SingleOrDefault(r => r.Token == token.Token);
            if (deviceReq == null)
                return NotFound(); // TODO: leaks account existence
            _context.DeviceCreationRequests.Remove(deviceReq);
            _context.SaveChanges();
            return Ok();
        }

        string HMacWithSha256(string secret, string message)
        {
            using (var hmac = new HMACSHA256(ASCIIEncoding.ASCII.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(ASCIIEncoding.ASCII.GetBytes(message));
                return Convert.ToBase64String(hash);
            }
        }

        bool CompareDigest(string sig1Base64, string sig2Base64)
        {
            var sig1 = Convert.FromBase64String(sig1Base64);
            var sig2 = Convert.FromBase64String(sig2Base64);
            if (sig1.Length != sig2.Length)
                return false;
            bool ret = true;
            for (int i = 0; i < sig1.Length; i++)
                ret = ret & (sig1[i] == sig2[i]);
            return ret;
        }

        Device AuthDevice(string key, string signature, long nonce, out string error)
        {
            error = "";
            // find device that matches key
            var device = _context.Devices.SingleOrDefault(d => d.DeviceKey == key);
            if (device == null)
            {
                error = "authentication failed";
                return null;
            }
            // check signature
            var message = nonce.ToString() + key;
            var ourSig = HMacWithSha256(device.DeviceSecret, message);
            if (!CompareDigest(ourSig, signature))
            {
                error = "authentication failed";
                return null;
            }
            // check nonce
            if (nonce <= device.Nonce)
            {
                error = "old nonce";
                return null;
            }
            // update nonce in db
            device.Nonce = nonce;
            _context.Devices.Update(device);
            _context.SaveChanges();
            return device;
        }

        [HttpPost]
        public IActionResult DeviceDestroy([FromBody] ApiAuth req) 
        {
            string error;
            var device = AuthDevice(req.Key, req.Signature, req.Nonce, out error);
            if (device == null)
                return BadRequest(error);
            _context.Devices.Remove(device);
            _context.SaveChanges();
            return Ok();          
        }

        [HttpPost]
        public IActionResult DeviceValidate([FromBody] ApiAuth req) 
        {
            string error;
            var device = AuthDevice(req.Key, req.Signature, req.Nonce, out error);
            if (device == null)
                return BadRequest(error);
            return Ok();
        }
    }
}
