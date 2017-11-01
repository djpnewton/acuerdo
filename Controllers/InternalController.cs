using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using viafront3.Models;
using viafront3.Models.TradeViewModels;
using viafront3.Data;
using via_jsonrpc;

namespace viafront3.Controllers
{
    [Route("[controller]/[action]")]
    public class InternalController : Controller
    {
        public InternalController()
        {
        }

        [Produces("application/json")]
        public IActionResult WebsocketAuth()
        {
            StringValues token;
            if (!Request.Headers.TryGetValue("Authorization", out token))
                return BadRequest();
            //TODO: check token and match it up to a user id or fail 
            return Ok(new { code = 0, message = (string)null, data = new { user_id = 1}});
        }
    }
}
