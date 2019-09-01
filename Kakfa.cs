using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using viafront3.Models;
using viafront3.Services;
using viafront3.Data;
using via_jsonrpc;
using Newtonsoft.Json;
using Confluent.Kafka;

namespace viafront3
{
    public static class Kafka
    {
        public enum OrderEvent
        {
            ORDER_EVENT_PUT     = 1,
            ORDER_EVENT_UPDATE  = 2,
            ORDER_EVENT_FINISH  = 3
        }

        public const string OrdersTopic = "orders";
        public const string DealsTopic = "deals";

        public static void Run(IServiceProvider serviceProvider)
        {
            // get exchange settings
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeSettings>>().Value;

            // get the user manager & email sender
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var emailSender = serviceProvider.GetRequiredService<IEmailSender>();

            var conf = new ConsumerConfig
            { 
                GroupId = "order-updates-group",
                BootstrapServers = settings.KafkaHost,
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            using (var c = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                c.Subscribe(new string[] {OrdersTopic, DealsTopic});

                CancellationTokenSource cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };

                try
                {
                    while (true)
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            Console.WriteLine($"Consumed message at: '{cr.TopicPartitionOffset}'.");
                            switch (cr.Topic)
                            {
                                case OrdersTopic:
                                    ProcessOrder(context, userManager, settings, emailSender, cr.Value);
                                    break;
                                case DealsTopic:
                                    ProcessDeal(context, userManager, emailSender, cr.Value);
                                    break;
                                default:
                                    break;
                            }
                            // commit queue offsets
                            c.Commit(cr, cts.Token);
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    c.Close();
                }
            }
        }

        static void ProcessOrder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ExchangeSettings settings, IEmailSender emailSender, string json)
        {
            // parse order event
            var parts = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var evt = (OrderEvent)(long)parts["event"];
            var stock = (string)parts["stock"];
            var money = (string)parts["money"];
            var order = JsonConvert.DeserializeObject<Order>(((Newtonsoft.Json.Linq.JObject)parts["order"]).ToString());
            Console.WriteLine($":: order event :: {evt} - {stock}/{money} (user id: {order.user}, id: {order.id}, type: {order.type}, side: {order.side}, amount: {order.amount}, price: {order.price}, left: {order.left}");
            // find user who owns the order
            var user = ApplicationUser.GetUserFromExchangeId(context, userManager, order.user);
            if (user == null)
            {
                Console.WriteLine($":: ERROR :: user for exchange id {order.user} not found");
                return;
            }
            // send email to user
            if (user.Email != null)
            {
                try
                {
                    switch (evt)
                    {
                        case OrderEvent.ORDER_EVENT_PUT:
                            if (order.type == OrderType.Limit)
                                emailSender.SendEmailLimitOrderCreatedAsync(user.Email, order.market, order.side.ToString(), order.amount, stock, order.price, money);
                            else
                                emailSender.SendEmailMarketOrderCreatedAsync(user.Email, order.market, order.side.ToString(), order.amount, stock);
                            break;
                        case OrderEvent.ORDER_EVENT_UPDATE:
                            if (order.type == OrderType.Limit)
                                emailSender.SendEmailLimitOrderUpdatedAsync(user.Email, order.market, order.side.ToString(), order.amount, stock, order.price, money, order.left);
                            else
                                emailSender.SendEmailMarketOrderUpdatedAsync(user.Email, order.market, order.side.ToString(), order.amount, stock, order.left);
                            break;
                        case OrderEvent.ORDER_EVENT_FINISH:
                            var amountInterval = decimal.Parse(settings.Markets[order.market].AmountInterval);
                            if (order.type == OrderType.Limit)
                                emailSender.SendEmailLimitOrderFinishedAsync(user.Email, order.market, order.side.ToString(), order.amount, stock, order.price, money, order.left, amountInterval);
                            else
                                emailSender.SendEmailMarketOrderFinishedAsync(user.Email, order.market, order.side.ToString(), order.amount, stock, order.left, amountInterval);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($":: ERROR sending email - '{ex.Message}'");
                }
                Console.WriteLine($":: email sent to {user.Email}");
            }
        } 

        static void ProcessDeal(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender, string json)
        {
            var parts = JsonConvert.DeserializeObject<List<object>>(json);
            var date = (double)parts[0];
            var market = (string)parts[1];
            var ask_id = (long)parts[2];
            var bid_id = (long)parts[3];
            var ask_user_id = (long)parts[4];
            var bid_user_id = (long)parts[5];
            var price = (string)parts[6];
            var amount = (string)parts[7];
            var ask_fee = (string)parts[8];
            var bid_fee = (string)parts[9];
            var side = (long)parts[10];
            var id = (long)parts[11];
            var stock = (string)parts[12];
            var money = (string)parts[13];

            Console.WriteLine($":: deal :: {stock}/{money} (ask user id: {ask_user_id}, bid user id: {bid_user_id}, id: {id}, amount: {amount}, price: {price}");
        }   
    }
}
