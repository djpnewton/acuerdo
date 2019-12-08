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
            // only allow from localhost
            var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
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