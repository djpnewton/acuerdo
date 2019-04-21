using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using viafront3.Data;
using viafront3.Models;
using xchwallet;
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

        public Broker(ILogger<Broker> logger, ApplicationDbContext context, IWalletProvider walletProvider, IOptions<ApiSettings> apiSettings)
        {
            _logger = logger;
            _context = context;
            _walletProvider = walletProvider;
            _apiSettings = apiSettings.Value;
        }

        public void ProcessOrders()
        {
            // get lock - ensure that this function ends before it is started again
            lock(lockObj)
            {
                _logger.LogInformation("Process Orders - Broker");
                var wallets = new Dictionary<string, IWallet>();
                var date = DateTimeOffset.Now.ToUnixTimeSeconds();
                var orders = _context.BrokerOrders.Where(o => o.Status == BrokerOrderStatus.Created.ToString());
                foreach (var order in orders)
                {
                    if (date > order.Expiry + _apiSettings.Broker.TimeLimitGracePeriod)
                    {
                        order.Status = BrokerOrderStatus.Expired.ToString();
                        _context.BrokerOrders.Update(order);
                    }
                    else
                    {
                        if (_walletProvider.IsChain(order.AssetSend))
                        {
                            // get wallet and only update from the blockchain one time
                            IWallet wallet = null;
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
                                            order.Status = BrokerOrderStatus.Incomming.ToString();
                                            order.TxIdPayment = tx.ChainTx.TxId;
                                            _context.BrokerOrders.Update(order);
                                            _logger.LogInformation($"Payment detected for order {order.Token}, {tx}");
                                        }

                                    }
                                }
                            }
                        }
                        else
                        {
                            //TODO: not yet implemented
                            _logger.LogError("receiving fiat not yet implemented");
                        }
                    }
                }
                _context.SaveChanges();
            }
        }
    }
}
