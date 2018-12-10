using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace viafront3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "console")
                BuildConsoleHost(args);
            else
                BuildWebHost(args).Run();
        }

        public static void BuildConsoleHost(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var configuration = builder.Build();
            var startup = new Startup(configuration);
            var sc = new ServiceCollection();
            startup.ConfigureServices(sc);
            var serviceProvider = sc.BuildServiceProvider();

            if (args.Length > 1 && args[1] == "initroles")
                Utils.CreateRoles(serviceProvider).Wait();
            else if (args.Length > 3 && args[1] == "addrole")
                Utils.GiveUserRole(serviceProvider, args[2], args[3]).Wait();
            else if (args.Length > 3 && args[1] == "consolidate_wallet")
                Utils.ConsolidateWallet(serviceProvider, args[2], args[3]).Wait();
            else
                Console.WriteLine("ERROR: no matching command");
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
