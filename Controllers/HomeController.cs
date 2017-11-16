using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using viafront3.Data;
using viafront3.Models;

namespace viafront3.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context) : base(userManager, context)
        {}

        public IActionResult Index()
        {
            return View(BaseViewModel());
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View(BaseViewModel());
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View(BaseViewModel());
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { User = GetUser().Result, RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
