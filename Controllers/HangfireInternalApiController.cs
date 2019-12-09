using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Hangfire;

namespace viafront3.Controllers
{
    [Produces("application/json")]
    [Route("api/hangfire/[action]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class HangfireInternalApiController : Controller
    {

        public HangfireInternalApiController() : base()
        {}

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // get remote ip
            var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
            // check if request was from proxy server and get real ip
            if (context.HttpContext.Connection.LocalPort != 80 && context.HttpContext.Connection.LocalPort != 443)
            {
                if (context.HttpContext.Request.Headers.TryGetValue("X-Real-IP", out var realIpStr))
                    if (IPAddress.TryParse(realIpStr, out var realIp))
                        remoteIp = realIp;
            }
            // only allow from localhost
            if (!IPAddress.IsLoopback(remoteIp))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            base.OnActionExecuting(context);
        }

        [HttpGet]
        public ActionResult<Hangfire.Storage.Monitoring.StatisticsDto> Stats()
        {
            return JobStorage.Current.GetMonitoringApi().GetStatistics();
        }
    }
}