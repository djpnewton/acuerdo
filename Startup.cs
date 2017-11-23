using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql;
using viafront3.Data;
using viafront3.Models;
using viafront3.Services;

namespace viafront3
{
    public class MySqlSettings
    {
        public string Host { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }

    public class AssetSettings
    {
        public int Decimals { get; set; }
    }

    public class MarketSettings
    {
        public string PriceUnit { get; set; }
        public string AmountUnit { get; set; }
        public int PriceDecimals { get; set; }
        public int AmountDecimals { get; set; }
    }

    public class ExchangeSettings
    {
        public MySqlSettings MySql { get; set; } = new MySqlSettings();
        public string AccessHttpHost { get; set; } = "http://localhost:8080";
        public string AccessWsIp { get; set; } = "127.0.0.1";
        public Dictionary<string, AssetSettings> Assets { get; set; } = new Dictionary<string, AssetSettings>();
        public Dictionary<string, MarketSettings> Markets { get; set; } = new Dictionary<string, MarketSettings>();
        public int OrderBookLimit { get; set; } = 99;
        public string OrderBookInterval { get; set; } = "2";
        public string TakerFeeRate { get; set; } = "0.02";
        public string MakerFeeRate { get; set; } = "0.01";
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add ExchangeSettings so it can be injected in controllers
            services.Configure<ExchangeSettings>(options => Configuration.GetSection("Exchange").Bind(options));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            loggerFactory.AddFile("logs/viafront-{Date}.txt");
        }
    }
}
