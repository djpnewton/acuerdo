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
using CommandLine;
using CommandLine.Text;

namespace viafront3
{
    public class Program
    {
        [Verb("server", HelpText = "Run the server")]
        class Server
        {
        }

        [Verb("initroles", HelpText = "Init roles in the database")]
        class InitRoles
        {
        }

        [Verb("addrole", HelpText = "Add a role to a user")]
        class AddRole
        { 
            [Option('e', "email", Required = true, HelpText = "User email")]
            public string Email { get; set; }
            [Option('r', "role", Required = true, HelpText = "Role")]
            public string Role { get; set; }
        }

        [Verb("consolidate_wallet", HelpText = "Consolidate funds in a wallet")]
        class ConsolidateWallet
        { 
            [Option('a', "asset", Required = true, HelpText = "Asset")]
            public string Asset { get; set; }
            [Option('e', "emails", Required = true, Separator = ',',  HelpText = "List of user emails (separated by commas)")]
            public IEnumerable<string> Emails { get; set; }
        }

        [Verb("process_fiat_deposit", HelpText = "Process a fiat deposit")]
        class ProcessFiatDeposit
        { 
            [Option('a', "asset", Required = true, HelpText = "Asset")]
            public string Asset { get; set; }
            [Option('d', "depositcode", Required = true, HelpText = "The deposit code")]
            public string DepositCode { get; set; }
            [Option('D', "date", Required = true, HelpText = "The date of the deposit")]
            public long Date { get; set ;}
            [Option('A', "amount", Required = true, HelpText = "The *decimal* amount")]
            public decimal Amount { get; set; }
            [Option('b', "bankmetadata", Required = true, HelpText = "The metadata from the bank")]
            public string BankMetadata { get; set; }
        }

        [Verb("process_fiat_withdrawal", HelpText = "Process a fiat withdrawal")]
        class ProcessFiatWithdrawal
        { 
            [Option('a', "asset", Required = true, HelpText = "Asset")]
            public string Asset { get; set; }
            [Option('d', "depositcode", Required = true,  HelpText = "The deposit code")]
            public string DepositCode { get; set; }
            [Option('D', "date", Required = true, HelpText = "The date of the deposit")]
            public long Date { get; set ;}
            [Option('A', "amount", Required = true,  HelpText = "The *decimal* amount")]
            public decimal Amount { get; set; }
            [Option('b', "bankmetadata", Required = true, HelpText = "The metadata from the bank")]
            public string BankMetadata { get; set; }
        }

        static int RunInitRolesAndReturnExitCode(InitRoles opts)
        {
            var sp = GetServiceProvider();
            Utils.CreateRoles(sp).Wait();
            return 0;
        }

        static int RunAddRoleAndReturnExitCode(AddRole opts)
        {
            var sp = GetServiceProvider();
            Utils.GiveUserRole(sp, opts.Email, opts.Role).Wait();
            return 0;
        }

        static int RunConsolidateAndReturnExitCode(ConsolidateWallet opts)
        {
            var sp = GetServiceProvider();
            Utils.ConsolidateWallet(sp, opts.Asset, opts.Emails).Wait();
            return 0;
        }

        static int RunProcessFiatDepositAndReturnExitCode(ProcessFiatDeposit opts)
        {
            var sp = GetServiceProvider();
            Utils.ProcessFiatDeposit(sp, opts.Asset, opts.DepositCode, opts.Date, opts.Amount, opts.BankMetadata);
            return 0;
        }

        static int RunProcessFiatWithdrawalAndReturnExitCode(ProcessFiatWithdrawal opts)
        {
            var sp = GetServiceProvider();
            Utils.ProcessFiatWithdrawal(sp, opts.Asset, opts.DepositCode, opts.Date, opts.Amount, opts.BankMetadata);
            return 0;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "console")
            {
                var argsList = args.ToList();
                argsList.RemoveAt(0);
                return CommandLine.Parser.Default.ParseArguments<InitRoles, AddRole, ConsolidateWallet, ProcessFiatDeposit, ProcessFiatWithdrawal>(argsList)
                    .MapResult(
                    (InitRoles opts) => RunInitRolesAndReturnExitCode(opts),
                    (AddRole opts) => RunAddRoleAndReturnExitCode(opts),
                    (ConsolidateWallet opts) => RunConsolidateAndReturnExitCode(opts),
                    (ProcessFiatDeposit opts) => RunProcessFiatDepositAndReturnExitCode(opts),
                    (ProcessFiatWithdrawal opts) => RunProcessFiatWithdrawalAndReturnExitCode(opts),
                    errs => 1);
            }

            BuildWebHost(args).Run();
            return 0;
        }

        public static ServiceProvider GetServiceProvider()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var configuration = builder.Build();
            var startup = new Startup(configuration);
            var sc = new ServiceCollection();
            startup.ConfigureServices(sc);
            return sc.BuildServiceProvider();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
