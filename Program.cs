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

        [Verb("change_user_email", HelpText = "Mark 'sent' a broker order")]
        class ChangeUserEmail
        {
            [Option('o', "oldemail", Required = true, HelpText = "Old (current) email address")]
            public string OldEmail { get; set; }

            [Option('n', "newemail", Required = true, HelpText = "New email address")]
            public string NewEmail { get; set; }
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

        [Verb("__temp_completed_fiat_withdraw_for_broker_order", HelpText = "[TEMP] Add a completed fiat withdrawal for broker order")]
        class CompletedFiatWithdrawForBrokerOrder
        {
            [Option('t', "token", Required = true, HelpText = "Broker order token")]
            public string Token { get; set; }
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

        [Verb("kafka_order_updates", HelpText = "Run a kafka order message consumer (and email users whose orders have been updated)")]
        class KafkaOrderUpdates
        { }

        static int RunInitRoles(InitRoles opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.CreateRoles(sp).GetAwaiter().GetResult();
            return 0;
        }

        static int RunAddRole(AddRole opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.GiveUserRole(sp, opts.Email, opts.Role).GetAwaiter().GetResult();
            return 0;
        }

        static int RunChangeUserEmail(ChangeUserEmail opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.ChangeUserEmail(sp, opts.OldEmail, opts.NewEmail).GetAwaiter().GetResult();
            return 0;
        }

        static int RunConsolidate(ConsolidateWallet opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.ConsolidateWallet(sp, opts.Asset, opts.Emails, opts.All).GetAwaiter().GetResult();
            return 0;
        }

        static int RunProcessFiatDeposit(ProcessFiatDeposit opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.ProcessFiatDeposit(sp, opts.Asset, opts.DepositCode, opts.Date, opts.Amount, opts.BankMetadata).GetAwaiter().GetResult();
            return 0;
        }

        static int RunProcessFiatWithdrawal(ProcessFiatWithdrawal opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.ProcessFiatWithdrawal(sp, opts.Asset, opts.DepositCode, opts.Date, opts.Amount, opts.BankMetadata).GetAwaiter().GetResult();
            return 0;
        }

        static int RunCompletedFiatWithdrawForBrokerOrder(CompletedFiatWithdrawForBrokerOrder opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.CompletedFiatWithdrawForBrokerOrder(sp, opts.Token).GetAwaiter().GetResult();
            return 0;
        }

        static int RunProcessChainWithdrawal(ProcessChainWithdrawal opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.ProcessChainWithdrawal(sp, opts.Asset, opts.SpendCode);
            return 0;
        }

        static int RunShowPendingChainWithdrawals(ShowPendingChainWithdrawals opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.ShowPendingChainWithdrawals(sp, opts.Asset);
            return 0;
        }

        static int RunCheckChainDeposits(CheckChainDeposits opts)
        {
            var sp = GetServiceProvider();
            ConsoleTasks.CheckChainDeposits(sp, opts.Asset);
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
                return CommandLine.Parser.Default.ParseArguments<InitRoles, AddRole, ChangeUserEmail,
                        ConsolidateWallet, ProcessFiatDeposit, ProcessFiatWithdrawal, CompletedFiatWithdrawForBrokerOrder, ProcessChainWithdrawal, ShowPendingChainWithdrawals, CheckChainDeposits,
                        KafkaOrderUpdates>(argsList)
                    .MapResult(
                    (InitRoles opts) => RunInitRoles(opts),
                    (AddRole opts) => RunAddRole(opts),
                    (ChangeUserEmail opts) => RunChangeUserEmail(opts),
                    (ConsolidateWallet opts) => RunConsolidate(opts),
                    (ProcessFiatDeposit opts) => RunProcessFiatDeposit(opts),
                    (ProcessFiatWithdrawal opts) => RunProcessFiatWithdrawal(opts),
                    (CompletedFiatWithdrawForBrokerOrder opts) => RunCompletedFiatWithdrawForBrokerOrder(opts),
                    (ProcessChainWithdrawal opts) => RunProcessChainWithdrawal(opts),
                    (ShowPendingChainWithdrawals opts) => RunShowPendingChainWithdrawals(opts),
                    (CheckChainDeposits opts) => RunCheckChainDeposits(opts),
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
            sc.AddLogging(builder => {
                builder.AddConsole().AddFilter(level => level >= LogLevel.Debug);
            });
            startup.ConfigureServices(sc);
            return sc.BuildServiceProvider();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
