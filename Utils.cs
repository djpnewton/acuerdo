using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using viafront3.Models;

namespace viafront3
{
    public static class Utils
    {
        public static async Task CreateRoles(IServiceProvider serviceProvider)
        {
            // create roles if they dont exist
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { "admin" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        public static async Task GiveUserRole(IServiceProvider serviceProvider, string email, string role)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var adminRole = await roleManager.FindByNameAsync(role);
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            if (!await userManager.IsInRoleAsync(user, adminRole.Name))
                await userManager.AddToRoleAsync(user, adminRole.Name);
        }
    }
}
