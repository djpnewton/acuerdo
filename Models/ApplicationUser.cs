using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using viafront3.Data;

namespace viafront3.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public bool EnsureExchangePresent(ApplicationDbContext context)
        {
            context.Entry(this).Reference(u => u.Exchange).Load();
            if (Exchange == null)
            {
                var exch = new Exchange{ ApplicationUserId=Id };
                context.Add(exch);
                return true;
            }
            return false;
        }

        public Exchange Exchange{ get; set; }
    }

    public class Exchange
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
    }
}
