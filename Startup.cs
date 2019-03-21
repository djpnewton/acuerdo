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
        public string PriceInterval { get; set; }
        public string AmountInterval { get; set; }
    }

    public class ExchangeSettings
    {
        public MySqlSettings MySql { get; set; } = new MySqlSettings();
        public string AccessHttpUrl { get; set; } = "http://localhost:8080";
        public string AccessWsUrl { get; set; } = "ws://localhost:8090";
        public string AccessWsIp { get; set; } = "127.0.0.1";
        public string KafkaHost { get; set; } = "127.0.0.1:9092";
        public Dictionary<string, AssetSettings> Assets { get; set; } = new Dictionary<string, AssetSettings>();
        public Dictionary<string, MarketSettings> Markets { get; set; } = new Dictionary<string, MarketSettings>();
        public int OrderBookLimit { get; set; } = 99;
        public string TakerFeeRate { get; set; } = "0.02";
        public string MakerFeeRate { get; set; } = "0.01";
        public bool MarketOrderBidAmountMoney { get; set; } = false;
    }

    public class ChainAssetSettings
    {
        public string NodeUrl { get; set; }
        public long FeeUnit { get; set; }
        public long FeeMax { get; set; }
        public int MinConf { get; set; }
    }

    public class WalletSettings
    {
        public bool Mainnet { get; set; } = false;
        public string ConsolidatedFundsTag { get; set; } = "Consolidate";
        public Dictionary<string, string> DbFiles { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, ChainAssetSettings> ChainAssetSettings { get; set; } = new Dictionary<string, ChainAssetSettings>();
        public Dictionary<string, xchwallet.BankAccount> BankAccounts { get; set; } = new Dictionary<string, xchwallet.BankAccount>();
    }

    public class EmailSenderSettings
    {
        public string SmtpHost { get; set; }
        public string From { get; set; }
    }

    public class ApiSettings
    {
        public int CreationExpiryMinutes { get; set; }
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
            services.Configure<WalletSettings>(options => Configuration.GetSection("Wallet").Bind(options));
            services.Configure<EmailSenderSettings>(options => Configuration.GetSection("EmailSender").Bind(options));
            services.Configure<ApiSettings>(options => Configuration.GetSection("Api").Bind(options));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseLazyLoadingProxies()
                       .UseMySql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddSingleton<IWebsocketTokens, WebsocketTokens>();
            services.AddSingleton<IWalletProvider, WalletProvider>();

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
