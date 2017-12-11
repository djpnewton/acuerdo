using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using viafront3.Models;
using viafront3.Models.InternalViewModels;
using viafront3.Data;
using viafront3.Services;
using via_jsonrpc;

namespace viafront3.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("[controller]/[action]")]
    public class InternalController : BaseSettingsController
    {
        private readonly IWebsocketTokens _websocketTokens;

        public InternalController(UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<ExchangeSettings> settings,
            IWebsocketTokens websocketTokens) : base(userManager, context, settings)
        {
            _websocketTokens = websocketTokens;
        }

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public IActionResult Users()
        {
            var user = GetUser(required: true).Result;

            var users = (from u in _context.Users
                let query = (from ur in _context.Set<IdentityUserRole<string>>()
                    where ur.UserId.Equals(u.Id)
                    join r in _context.Roles on ur.RoleId equals r.Id select r.Name)
                    select new UserRoleInfo() {User = u, Roles = query.ToList<string>()})
                    .ToList();

            var model = new UsersViewModel
            {
                User = user,
                UserRoleInfos = users
            };
            return View(model);
        }

        [AllowAnonymous]
        [Produces("application/json")]
        public IActionResult WebsocketAuth()
        {
            var ip = GetRequestIP();
            if (ip != _settings.AccessWsIp)
                return Unauthorized();
            StringValues token;
            if (!Request.Headers.TryGetValue("Authorization", out token))
                return BadRequest();
            var wsToken = _websocketTokens.Remove(token);
            if (wsToken == null)
                return Unauthorized();
            return Ok(new { code = 0, message = (string)null, data = new { user_id = wsToken.ExchangeUserId}});
        }

        public IActionResult TestWebsocket()
        {
            var user = GetUser(required: true).Result;

            var model = new TestWebsocketViewModel
            {
                User = user,
                WebsocketToken = _websocketTokens.NewToken(user.Exchange.Id),
                WebsocketUrl = _settings.AccessWsUrl
            };

            return View(model);
        }
    }
}
