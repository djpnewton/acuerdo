using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using viafront3.Models;
using viafront3.Data;

namespace viafront3.Controllers
{
    public class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly ApplicationDbContext _context;

        public BaseController(
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        protected async Task<ApplicationUser> GetUser(bool required = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                if (!required)
                    return null;
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (user.EnsureExchangePresent(_context))
                _context.SaveChanges();

            return user;
        }

        protected BaseViewModel BaseViewModel()
        {
            return new BaseViewModel() { User = GetUser().Result};
        }
    }
}
