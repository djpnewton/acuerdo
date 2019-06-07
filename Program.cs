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
            [Option('e', "emails", Default = null, Separator = ',',  HelpText = "List of user emails (separated by commas) to consolidate from")]
            public IEnumerable<string> Emails { get; set; }
            [Option('A', "all", Default = false, HelpText = "Consolidate from all users")]
            public bool All { get; set; }
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

        [Verb("__process_chain_withdrawal", HelpText = "Process a blockchain withdrawal")]
        class ProcessChainWithdrawal
        { 
            [Option('a', "asset", Required = true, HelpText = "Asset")]
            public string Asset { get; set; }
            [Option('s', "spendcode", Required = true,  HelpText = "The spend code")]
            public string SpendCode { get; set; }
        }

        [Verb("show_pending_chain_withdrawals", HelpText = "Show pending blockchain withdrawals")]
        class ShowPendingChainWithdrawals
        { 
            [Option('a', "asset", Required = true, HelpText = "Asset")]
            public string Asset { get; set; }
        }

        [Verb("__check_chain_deposits", HelpText = "Check all users for deposits")]
        class CheckChainDeposits
        { 
            [Option('a', "asset", Required = true, HelpText = "Asset")]
            public string Asset { get; set; }
        }

        [Verb("process_broker_order", HelpText = "Mark 'sent' a broker order")]
        class ProcessBrokerOrder
        {
            [Option('t', "token", Required = true, HelpText = "Token")]
            public string Token { get; set; }

            [Option('a', "amountsent", Required = true, HelpText = "Amount (decimal) sent")]
            public decimal AmountSent { get; set; }
        }

        [Verb("kafka_order_updates", HelpText = "Run a kafka order message consumer (and email users whose orders have been updated)")]
        class KafkaOrderUpdates
        { }

        static int RunInitRoles(InitRoles opts)
        {
            var sp = GetServiceProvider();
            Utils.CreateRoles(sp).Wait();
            return 0;
        }

        static int RunAddRole(AddRole opts)
        {
            var sp = GetServiceProvider();
            Utils.GiveUserRole(sp, opts.Email, opts.Role).Wait();
            return 0;
        }

        static int RunConsolidate(ConsolidateWallet opts)
        {
            var sp = GetServiceProvider();
            Utils.ConsolidateWallet(sp, opts.Asset, opts.Emails, opts.All).Wait();
            return 0;
        }

        static int RunProcessFiatDeposit(ProcessFiatDeposit opts)
        {
            var sp = GetServiceProvider();
            Utils.ProcessFiatDeposit(sp, opts.Asset, opts.DepositCode, opts.Date, opts.Amount, opts.BankMetadata).Wait();
            return 0;
        }

        static int RunProcessFiatWithdrawal(ProcessFiatWithdrawal opts)
        {
            var sp = GetServiceProvider();
            Utils.ProcessFiatWithdrawal(sp, opts.Asset, opts.DepositCode, opts.Date, opts.Amount, opts.BankMetadata).Wait();
            return 0;
        }

        static int RunProcessChainWithdrawal(ProcessChainWithdrawal opts)
        {
            var sp = GetServiceProvider();
            Utils.ProcessChainWithdrawal(sp, opts.Asset, opts.SpendCode);
            return 0;
        }

        static int RunShowPendingChainWithdrawals(ShowPendingChainWithdrawals opts)
        {
            var sp = GetServiceProvider();
            Utils.ShowPendingChainWithdrawals(sp, opts.Asset);
            return 0;
        }

        static int RunCheckChainDeposits(CheckChainDeposits opts)
        {
            var sp = GetServiceProvider();
            Utils.CheckChainDeposits(sp, opts.Asset);
            return 0;
        }

        static int RunProcessBrokerOrder(ProcessBrokerOrder opts)
        {
            var sp = GetServiceProvider();
            Utils.ProcessBrokerOrder(sp, opts.Token, opts.AmountSent);
            return 0;
        }

        static int RunKafkaOrderUpdates(KafkaOrderUpdates opts)
        {
            var sp = GetServiceProvider();
            Kafka.Run(sp);
            return 0;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "console")
            {
                var argsList = args.ToList();
                argsList.RemoveAt(0);
                return CommandLine.Parser.Default.ParseArguments<InitRoles, AddRole,
                        ConsolidateWallet, ProcessFiatDeposit, ProcessFiatWithdrawal, ProcessChainWithdrawal, ShowPendingChainWithdrawals, CheckChainDeposits, ProcessBrokerOrder,
                        KafkaOrderUpdates>(argsList)
                    .MapResult(
                    (InitRoles opts) => RunInitRoles(opts),
                    (AddRole opts) => RunAddRole(opts),
                    (ConsolidateWallet opts) => RunConsolidate(opts),
                    (ProcessFiatDeposit opts) => RunProcessFiatDeposit(opts),
                    (ProcessFiatWithdrawal opts) => RunProcessFiatWithdrawal(opts),
                    (ProcessChainWithdrawal opts) => RunProcessChainWithdrawal(opts),
                    (ShowPendingChainWithdrawals opts) => RunShowPendingChainWithdrawals(opts),
                    (CheckChainDeposits opts) => RunCheckChainDeposits(opts),
                    (ProcessBrokerOrder opts) => RunProcessBrokerOrder(opts),
                    (KafkaOrderUpdates opts) => RunKafkaOrderUpdates(opts),
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
