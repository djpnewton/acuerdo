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
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using viafront3.Models;
using viafront3.Models.DevApiViewModels;
using viafront3.Data;
using viafront3.Services;
using Newtonsoft.Json;

namespace viafront3.Controllers
{
    [Produces("application/json")]
    [Route("api/dev/[action]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class DevApiController : BaseWalletController
    {
        protected readonly IHostingEnvironment _hostingEnvironment;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApiSettings _apiSettings;
        private readonly ITripwire _tripwire;

        public DevApiController(
            IHostingEnvironment hostingEnvironment,
            ILogger<ApiController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            IOptions<ExchangeSettings> settings,
            IOptions<ApiSettings> apiSettings,
            RoleManager<IdentityRole> roleManager,
            IOptions<KycSettings> kycSettings,
            IWalletProvider walletProvider,
            ITripwire tripwire) : base(logger, userManager, context, settings, walletProvider, kycSettings)
        {
            _hostingEnvironment = hostingEnvironment;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _apiSettings = apiSettings.Value;
            _tripwire = tripwire;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_hostingEnvironment.IsDevelopment())
                context.Result = NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<DevApiUserCreate>> UserCreate([FromBody] DevApiUserCreate req) 
        {
            var existingUser = await _userManager.FindByEmailAsync(req.Email);
            if (existingUser == null)
            {
                (var result, var user) = await CreateUser(_signInManager, _emailSender, req.Email, req.Email, req.Password, req.SendEmail, false);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create broker user");
                    return BadRequest();
                }
                existingUser = user;
            }
            if (!existingUser.EmailConfirmed && req.EmailConfirmed)
            {
                existingUser.EmailConfirmed = true;
                await PostUserEmailConfirmed(_roleManager, _signInManager, _kycSettings, existingUser);
            }
            return req;
        }
    }
}