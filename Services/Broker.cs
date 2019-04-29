using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using viafront3.Data;
using viafront3.Models;
using xchwallet;
using via_jsonrpc;
using Newtonsoft.Json;

namespace viafront3.Services
{
    public interface IBroker
    {
        void ProcessOrders();
    }

    public class Broker : IBroker
    {
        static Object lockObj = new Object();

        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWalletProvider _walletProvider;
        private readonly ApiSettings _apiSettings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ExchangeSettings _settings;

        public Broker(ILogger<Broker> logger,
            ApplicationDbContext context,
            IWalletProvider walletProvider,
            IOptions<ApiSettings> apiSettings,
            UserManager<ApplicationUser> userManager,
            IOptions<ExchangeSettings> settings)
        {
            _logger = logger;
            _context = context;
            _walletProvider = walletProvider;
            _apiSettings = apiSettings.Value;
            _userManager = userManager;
            _settings = settings.Value;
        }

        bool DepositAndCreateTrade(ApplicationUser brokerUser, IWallet wallet, BrokerOrder order, WalletTx tx)
        {
            // check broker exchange id
            if (brokerUser.Exchange == null)
            {
                _logger.LogError("Failed to get broker exchange id");
                return false;
            }
            // create and test backend connection
            var via = new ViaJsonRpc(_settings.AccessHttpUrl); //TODO: move this to a ViaRpcProvider in /Services (like IWalletProvider)
            via.BalanceQuery(1);
            // register new deposit with the exchange backend
            var amount = wallet.AmountToString(tx.ChainTx.Amount);
            var source = new Dictionary<string, object>();
            source["txid"] = tx.ChainTx.TxId;
            var businessId = tx.Meta.Id;
            via.BalanceUpdateQuery(brokerUser.Exchange.Id, order.AssetSend, "deposit", businessId, amount, source);
            // make trade
            string tradeAmount;
            if (order.Side == OrderSide.Bid)
                tradeAmount = order.AmountReceive.ToString();
            else if (order.Side == OrderSide.Ask)
                tradeAmount = order.AmountSend.ToString();
            else
                throw new Exception("invalid order side");
            via.OrderMarketQuery(brokerUser.Exchange.Id, order.Market, order.Side, tradeAmount, "0", _apiSettings.Broker.BrokerTag);
            return true;
        }

        void ProcessOrderChain(Dictionary<string, IWallet> wallets, BrokerOrder order)
        {
            // get broker user
            var task = _userManager.FindByNameAsync(_apiSettings.Broker.BrokerTag);
            task.Wait();
            var brokerUser = task.Result;
            if (brokerUser == null)
            {
                _logger.LogError("Failed to find broker user");
                return;
            }
            // get wallet and only update from the blockchain one time
            IWallet wallet;
            if (wallets.ContainsKey(order.AssetSend))
                wallet = wallets[order.AssetSend];
            else
            {
                wallet = _walletProvider.GetChain(order.AssetSend);
                wallet.UpdateFromBlockchain();
                wallet.Save();
                wallets[order.AssetSend] = wallet;
            }

            var txs = wallet.GetAddrUnacknowledgedTransactions(order.PaymentAddress);
            var ackTxs = new List<WalletTx>();
            foreach (var tx in txs)
            {
                // get invoice id
                string invoiceId = null;
                if (tx.ChainTx.Attachment != null)
                {
                    var att = System.Text.Encoding.UTF8.GetString(tx.ChainTx.Attachment.Data);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(att);
                    if (dict.ContainsKey("InvoiceId"))
                        invoiceId = dict["InvoiceId"];
                }
                // check tx is incomming to our wallet
                if (tx.Direction == WalletDirection.Incomming)
                {
                    // check invoice id matches (if asset uses account model)
                    if (wallet.GetLedgerModel() == xchwallet.LedgerModel.Account && invoiceId == order.InvoiceId ||
                        wallet.GetLedgerModel() == xchwallet.LedgerModel.UTXO)
                    {
                        // check amount matches
                        var amount = wallet.AmountToString(tx.ChainTx.Amount);
                        if (order.AmountSend <= decimal.Parse(amount))
                        {
                            // bingo!
                            if (order.Status == BrokerOrderStatus.Ready.ToString())
                            {
                                order.Status = BrokerOrderStatus.Incomming.ToString();
                                order.TxIdPayment = tx.ChainTx.TxId;
                                _context.BrokerOrders.Update(order);
                                _logger.LogInformation($"Payment detected for order {order.Token}, {tx}");
                            }
                            else if (order.Status == BrokerOrderStatus.Incomming.ToString() &&
                                tx.ChainTx.Confirmations >= _walletProvider.ChainAssetSettings(order.AssetSend).MinConf)
                            {
                                order.Status = BrokerOrderStatus.Confirmed.ToString();
                                _context.BrokerOrders.Update(order);
                                ackTxs.Add(tx);
                                _logger.LogInformation($"Payment confirmed for order {order.Token}, {tx}");
                                DepositAndCreateTrade(brokerUser, wallet, order, tx);
                            }
                        }
                    }
                }
            }
            wallet.AcknowledgeTransactions(brokerUser.Id, ackTxs);
            wallet.Save();
        }

        void ProcessOrderFiat(BrokerOrder order)
        {
            //TODO: not yet implemented
            _logger.LogError("receiving fiat not yet implemented");
        }

        public void ProcessOrders()
        {
            // get lock - ensure that this function ends before it is started again
            lock(lockObj)
            {
                _logger.LogInformation("Process Orders - Broker");
                var wallets = new Dictionary<string, IWallet>();
                var date = DateTimeOffset.Now.ToUnixTimeSeconds();
                // process created orders
                var orders = _context.BrokerOrders.Where(o => o.Status == BrokerOrderStatus.Ready.ToString() || o.Status == BrokerOrderStatus.Incomming.ToString());
                foreach (var order in orders)
                {
                    if (_walletProvider.IsChain(order.AssetSend))
                        ProcessOrderChain(wallets, order);
                    else
                        ProcessOrderFiat(order);
                }
                _context.SaveChanges();
                // expire orders
                orders = _context.BrokerOrders.Where(o => o.Status == BrokerOrderStatus.Ready.ToString());
                foreach (var order in orders)
                {
                    if (date > order.Expiry + _apiSettings.Broker.TimeLimitGracePeriod)
                    {
                        order.Status = BrokerOrderStatus.Expired.ToString();
                        _context.BrokerOrders.Update(order);
                    }
                }
                _context.SaveChanges();
            }
        }
    }
}
