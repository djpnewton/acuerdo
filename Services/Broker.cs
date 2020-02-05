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
using RestSharp;

namespace viafront3.Services
{
    public interface IBroker
    {
        void ProcessOrders();
        bool FiatWithdrawToCustomer(ApplicationUser brokerUser, BrokerOrder order);
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
        private readonly ITripwire _tripwire;
        private readonly FiatProcessorSettings _fiatSettings;
        private readonly IEmailSender _emailSender;

        public Broker(ILogger<Broker> logger,
            ApplicationDbContext context,
            IWalletProvider walletProvider,
            IOptions<ApiSettings> apiSettings,
            UserManager<ApplicationUser> userManager,
            IOptions<ExchangeSettings> settings,
            ITripwire tripwire,
            IOptions<FiatProcessorSettings> fiatSettings,
            IEmailSender emailSender)
        {
            _logger = logger;
            _context = context;
            _walletProvider = walletProvider;
            _apiSettings = apiSettings.Value;
            _userManager = userManager;
            _settings = settings.Value;
            _tripwire = tripwire;
            _fiatSettings = fiatSettings.Value;
            _emailSender = emailSender;
        }

        bool DepositAndCreateTrade(ApplicationUser brokerUser, BrokerOrder order)
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
            var amount = order.AmountSend.ToString();
            var source = new Dictionary<string, object>();
            source["BrokerOrderToken"] = order.Token;
            var businessId = order.Id;
            try
            {
                via.BalanceUpdateQuery(brokerUser.Exchange.Id, order.AssetSend, "deposit", businessId, amount, source);
            }
            catch (ViaJsonException ex)
            {
                if (ex.Err == ViaError.BALANCE_UPDATE__REPEAT_UPDATE)
                    _logger.LogError(ex, $"broker already made this exchange update - exch id: {brokerUser.Exchange.Id}, business id: {businessId}");
                else
                    throw;
            }
            // make trade
            string tradeAmount;
            if (order.Side == OrderSide.Bid)
                tradeAmount = order.AmountReceive.ToString();
            else if (order.Side == OrderSide.Ask)
                tradeAmount = order.AmountSend.ToString();
            else
                throw new Exception("invalid order side");
            if (!_settings.MarketOrderBidAmountMoney)
                via.OrderMarketQuery(brokerUser.Exchange.Id, order.Market, order.Side, tradeAmount, "0", _apiSettings.Broker.BrokerTag, _settings.MarketOrderBidAmountMoney);
            else
                via.OrderMarketQuery(brokerUser.Exchange.Id, order.Market, order.Side, tradeAmount, "0", _apiSettings.Broker.BrokerTag);
            return true;
        }

        void CheckTxs(ApplicationUser brokerUser, ApplicationUser user, BrokerOrder order)
        {
            // get wallet and only update from the blockchain one time
            IWallet wallet = _walletProvider.GetChain(order.AssetSend);

            var txs = wallet.GetAddrUnacknowledgedTransactions(order.PaymentAddress);
            var ackTxs = new List<WalletTx>();
            foreach (var tx in txs)
            {
                // get invoice id
                string invoiceId = null;
                if (tx.ChainTx.Attachment != null)
                {
                    var att = System.Text.Encoding.UTF8.GetString(tx.ChainTx.Attachment.Data);
                    try
                    {
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(att);
                        invoiceId = dict.FirstOrDefault(x => String.Equals(x.Key, "InvoiceId", StringComparison.OrdinalIgnoreCase)).Value;
                    }
                    catch {}
                }
                // check tx is incomming to our wallet
                if (tx.Direction == WalletDirection.Incomming)
                {
                    // check invoice id matches (if asset uses account model)
                    if (wallet.GetLedgerModel() == xchwallet.LedgerModel.Account && invoiceId == order.InvoiceId ||
                        wallet.GetLedgerModel() == xchwallet.LedgerModel.UTXO)
                    {
                        // check amount matches
                        var amount = wallet.AmountToString(tx.AmountOutputs());
                        if (order.AmountSend <= decimal.Parse(amount))
                        {
                            // bingo!
                            if (order.Status == BrokerOrderStatus.Ready.ToString())
                            {
                                order.Status = BrokerOrderStatus.Incomming.ToString();
                                order.TxIdPayment = tx.ChainTx.TxId;
                                _context.BrokerOrders.Update(order);
                                _logger.LogInformation($"Payment detected for order {order.Token}, {tx}");

                                // send email
                                _emailSender.SendEmailBrokerSeenIncomingFunds(user.Email, order.AssetSend, wallet.AmountToString(order.AmountSend), order.InvoiceId).GetAwaiter().GetResult();
                                _logger.LogInformation($"Sent email to {user.Email}");
                            }
                            else if (order.Status == BrokerOrderStatus.Incomming.ToString() &&
                                tx.ChainTx.Confirmations >= _walletProvider.ChainAssetSettings(order.AssetSend).MinConf)
                            {
                                order.Status = BrokerOrderStatus.Confirmed.ToString();
                                _context.BrokerOrders.Update(order);
                                ackTxs.Add(tx);
                                _logger.LogInformation($"Payment confirmed for order {order.Token}, {tx}");
                                DepositAndCreateTrade(brokerUser, order);
                            }
                        }
                    }
                }
            }
            wallet.AcknowledgeTransactions(ackTxs);
            wallet.Save();
        }

        bool ChainWithdrawToCustomer(ApplicationUser brokerUser, BrokerOrder order)
        {
            var asset = order.AssetReceive;
            var amount = order.AmountReceive;

            var wallet = _walletProvider.GetChain(asset);
            if (wallet == null)
            {
                _logger.LogError($"No chain wallet for {asset}");
                return false;
            }
            var brokerWalletTag = wallet.GetTag(brokerUser.Id);
            if (brokerWalletTag == null)
            {
                _logger.LogError($"No tag for broker {brokerUser.Id}");
                return false;
            }

            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(brokerUser.Exchange.Id, asset);

            // validate amount
            var amountInt = wallet.StringToAmount(amount.ToString());
            var availableInt = wallet.StringToAmount(balance.Available);
            if (amountInt > availableInt)
            {
                _logger.LogError("broker available balance is too small");
                return false;
            }
            if (amountInt <= 0)
            {
                _logger.LogError("amount must be greather then or equal to 0");
                return false;
            }

            var consolidatedFundsTag = _walletProvider.ConsolidatedFundsTag();

            using (var dbtx = wallet.BeginDbTransaction())
            {
                // ensure tag exists
                if (!wallet.HasTag(consolidatedFundsTag))
                {
                    wallet.NewTag(consolidatedFundsTag);
                    wallet.Save();
                }

                // register withdrawal with wallet
                var tag = wallet.GetTag(brokerUser.Id);
                if (tag == null)
                    tag = wallet.NewTag(brokerUser.Id);
                var spend = wallet.RegisterPendingSpend(consolidatedFundsTag, consolidatedFundsTag,
                    order.Recipient, amountInt, tag);
                wallet.Save();
                var businessId = spend.Id;

                try
                {
                    // link pending withdrawal to broker order
                    var bow = new BrokerOrderChainWithdrawal { BrokerOrderId = order.Id, SpendCode = spend.SpendCode };
                    _context.BrokerOrderChainWithdrawals.Add(bow);
                    // we save changes here so that we a broker order cannot be processed twice(BrokerOrderChainWithdrawal.BrokerOrderId is unique)
                    _context.SaveChanges();
                }
                catch
                {
                    _logger.LogError($"unable to create BrokerOrderChainWithdrawal object ({order.Id}, {spend.SpendCode}");
                    throw;
                }

                // register withdrawal with the exchange backend
                var negativeAmount = -amount;
                try
                {
                    via.BalanceUpdateQuery(brokerUser.Exchange.Id, asset, "withdraw", businessId, negativeAmount.ToString(), null);
                }
                catch (ViaJsonException ex)
                {
                    _logger.LogError(ex, "Failed to update (withdraw) user balance (xch id: {0}, asset: {1}, businessId: {2}, amount {3}",
                        brokerUser.Exchange.Id, asset, businessId, negativeAmount);
                    if (ex.Err == ViaError.BALANCE_UPDATE__BALANCE_NOT_ENOUGH)
                    {
                        dbtx.Rollback();
                        _logger.LogError("balance not enough");
                        return false;
                    }
                    throw;
                }

                dbtx.Commit();
            }

            return true;
        }

        public bool FiatWithdrawToCustomer(ApplicationUser brokerUser, BrokerOrder order)
        {
            var asset = order.AssetReceive;
            var amount = order.AmountReceive;

            var wallet = _walletProvider.GetFiat(asset);
            if (wallet == null)
            {
                _logger.LogError($"No fiat wallet for {asset}");
                return false;
            }

            var via = new ViaJsonRpc(_settings.AccessHttpUrl);
            var balance = via.BalanceQuery(brokerUser.Exchange.Id, asset);

            // validate amount
            var amountInt = wallet.AmountToLong(amount);
            var availableInt = wallet.StringToAmount(balance.Available);
            if (amountInt > availableInt)
            {
                _logger.LogError("broker available balance is too small");
                return false;
            }
            if (amountInt <= 0)
            {
                _logger.LogError("amount must be greather then or equal to 0");
                return false;
            }

            using (var dbtx = wallet.BeginDbTransaction())
            {
                // register withdrawal with wallet
                var acct = new BankAccount { AccountNumber = order.Recipient };
                var tx = wallet.RegisterPendingWithdrawal(brokerUser.Id, amountInt, acct);
                if (tx == null)
                {
                    _logger.LogError($"Failed to create fiat withdrawal ('{order.Token}')");
                    return false;
                }
                wallet.Save();
                var businessId = tx.Id;

                try
                {
                    // link pending withdrawal to broker order
                    var bow = new BrokerOrderFiatWithdrawal { BrokerOrderId = order.Id, DepositCode = tx.DepositCode };
                    _context.BrokerOrderFiatWithdrawals.Add(bow);
                    // we save changes here so that we a broker order cannot be processed twice(BrokerOrderChainWithdrawal.BrokerOrderId is unique)
                    _context.SaveChanges();
                }
                catch
                {
                    _logger.LogError($"unable to create BrokerOrderChainWithdrawal object ({order.Id}, {tx.DepositCode}");
                    throw;
                }

                // register withdrawal with the exchange backend
                var negativeAmount = -amount;
                try
                {
                    via.BalanceUpdateQuery(brokerUser.Exchange.Id, asset, "withdraw", businessId, negativeAmount.ToString(), null);
                }
                catch (ViaJsonException ex)
                {
                    _logger.LogError(ex, "Failed to update (withdraw) user balance (xch id: {0}, asset: {1}, businessId: {2}, amount {3}",
                        brokerUser.Exchange.Id, asset, businessId, negativeAmount);
                    if (ex.Err == ViaError.BALANCE_UPDATE__BALANCE_NOT_ENOUGH)
                    {
                        dbtx.Rollback();
                        _logger.LogError("balance not enough");
                        return false;
                    }
                    throw;
                }

                dbtx.Commit();
            }

            return true;
        }

        void ProcessOrderChain(BrokerOrder order)
        {
            // check wallet has been updated
            var time = _walletProvider.LastBlockchainWalletUpdate(order.AssetSend);
            if (time < DateTimeOffset.Now.AddMinutes(-(_apiSettings.Broker.TimeLimitGracePeriod/2)))
            {
                _logger.LogWarning($"Not processing broker order ({order.Token}) as the wallet ({order.AssetSend}) was not updated since {time}");
                return;
            }

            // get broker user
            var brokerUser = _userManager.FindByNameAsync(_apiSettings.Broker.BrokerTag).GetAwaiter().GetResult();
            if (brokerUser == null)
            {
                _logger.LogError("Failed to find broker user");
                return;
            }

            // get order user
            var user = _userManager.FindByIdAsync(order.ApplicationUserId).GetAwaiter().GetResult();
            if (user == null)
            {
                _logger.LogError($"Failed to find order user ('{order.ApplicationUserId}')");
                return;
            }

            if (order.Status == BrokerOrderStatus.Ready.ToString() || order.Status == BrokerOrderStatus.Incomming.ToString())
                CheckTxs(brokerUser, user, order);
            else if (order.Status == BrokerOrderStatus.Confirmed.ToString())
            {
                if (FiatWithdrawToCustomer(brokerUser, order))
                {
                    order.Status = BrokerOrderStatus.PayoutWait.ToString();
                    _context.BrokerOrders.Update(order);
                    _logger.LogError($"Sent fiat for order ('{order.Token}')");
                }
                else
                    _logger.LogError($"failed to send fiat for order ({order.Token})");
            }
            else if (order.Status == BrokerOrderStatus.PayoutWait.ToString())
            {
                var bow = _context.BrokerOrderFiatWithdrawals.SingleOrDefault(o => o.BrokerOrderId == order.Id);
                if (bow == null)
                {
                    _logger.LogWarning($"broker order withdrawal not found ({order.Token})");
                    return;
                }

                // check fiat withdrawal is completed
                var wallet = _walletProvider.GetFiat(order.AssetReceive);
                var fiatTx = wallet.GetTx(bow.DepositCode);
                if (fiatTx != null && fiatTx.BankTx != null)
                {
                    order.Status = BrokerOrderStatus.Sent.ToString();
                    _context.BrokerOrders.Update(order);
                    _logger.LogInformation($"Payout confirmed for order {order.Token}");

                    // send email
                    var sendWallet = _walletProvider.GetChain(order.AssetSend);
                    var receiveWallet = _walletProvider.GetFiat(order.AssetReceive);
                    _emailSender.SendEmailBrokerSentOutgoingFunds(user.Email, order.AssetSend, sendWallet.AmountToString(order.AmountSend), order.AssetReceive,
                        receiveWallet.AmountToString(order.AmountReceive), order.InvoiceId).GetAwaiter().GetResult();
                    _logger.LogInformation($"Sent email to {user.Email}");
                }
            }
        }

        void ProcessOrderFiat(BrokerOrder order)
        {
            if (!_fiatSettings.PaymentsEnabled)
            {
                _logger.LogError("receiving fiat not enabled");
                return;
            }
            if (!_fiatSettings.PaymentsAssets.Contains(order.AssetSend))
            {
                _logger.LogError($"receiving fiat currency '${order.AssetSend}' not supported");
                return;
            }
            // get broker user
            var brokerUser = _userManager.FindByNameAsync(_apiSettings.Broker.BrokerTag).GetAwaiter().GetResult();
            if (brokerUser == null)
            {
                _logger.LogError("Failed to find broker user");
                return;
            }

            // get order user
            var user = _userManager.FindByIdAsync(order.ApplicationUserId).GetAwaiter().GetResult();
            if (user == null)
            {
                _logger.LogError($"Failed to find order user ('{order.ApplicationUserId}')");
                return;
            }

            var paymentReq = RestUtils.GetFiatPaymentRequest(_fiatSettings, order.Token);
            if (paymentReq == null)
            {
                _logger.LogError($"Failed to get fiat payment request (token: {order.Token})");
                return;
            }
            if (order.Status == BrokerOrderStatus.Ready.ToString() || order.Status == BrokerOrderStatus.Incomming.ToString())
            {
                // bingo!
                if (paymentReq.Status.ToLower() == viafront3.Models.ApiViewModels.ApiRequestStatus.Completed.ToString().ToLower())
                {
                    order.Status = BrokerOrderStatus.Confirmed.ToString();
                    _context.BrokerOrders.Update(order);
                    _logger.LogInformation($"Payment confirmed for order {order.Token}");
                    DepositAndCreateTrade(brokerUser, order);

                    // send email
                    var wallet = _walletProvider.GetFiat(order.AssetSend);
                    if (wallet == null)
                    {
                        _logger.LogError($"Failed to get fiat wallet for order (token: {order.Token})");
                        return;
                    }
                    _emailSender.SendEmailBrokerSeenIncomingFunds(user.Email, order.AssetSend, wallet.AmountToString(order.AmountSend), order.InvoiceId).GetAwaiter().GetResult();
                    _logger.LogInformation($"Sent email to {user.Email}");
                }
            }
            else if (order.Status == BrokerOrderStatus.Confirmed.ToString())
            {
                if (ChainWithdrawToCustomer(brokerUser, order))
                {
                    order.Status = BrokerOrderStatus.PayoutWait.ToString();
                    _context.BrokerOrders.Update(order);
                    _logger.LogInformation($"Sent funds for order {order.Token}");
                }
                else
                    _logger.LogError($"failed to send funds for order ({order.Token})");
            }
            else if (order.Status == BrokerOrderStatus.PayoutWait.ToString())
            {
                var bow = _context.BrokerOrderChainWithdrawals.SingleOrDefault(o => o.BrokerOrderId == order.Id);
                if (bow == null)
                {
                    _logger.LogWarning($"broker order withdrawal not found ({order.Token})");
                    return;
                }

                var asset = order.AssetReceive;
                var wallet = _walletProvider.GetChain(asset);
                if (wallet == null)
                {
                    _logger.LogError($"No chain wallet for {asset}");
                    return;
                }

                var spend = wallet.PendingSpendsGet().SingleOrDefault(s => s.SpendCode == bow.SpendCode);
                if (spend == null)
                {
                    _logger.LogError($"No pending spend for broker {bow.SpendCode}");
                    return;
                }

                if (spend.State == PendingSpendState.Complete)
                {
                    order.Status = BrokerOrderStatus.Sent.ToString();
                    _context.BrokerOrders.Update(order);
                    _logger.LogInformation($"Payout confirmed for order {order.Token}");

                    // send email
                    var sendWallet = _walletProvider.GetFiat(order.AssetSend);
                    var receiveWallet = _walletProvider.GetChain(order.AssetReceive);
                    _emailSender.SendEmailBrokerSentOutgoingFunds(user.Email, order.AssetSend, sendWallet.AmountToString(order.AmountSend), order.AssetReceive,
                        receiveWallet.AmountToString(order.AmountReceive), order.InvoiceId).GetAwaiter().GetResult();
                    _logger.LogInformation($"Sent email to {user.Email}");
                }
            }
        }

        public void ProcessOrders()
        {
            // if tripwire tripped cancel
            if (!_tripwire.TradingEnabled() || !_tripwire.WithdrawalsEnabled())
            {
                _logger.LogError("Tripwire tripped, exiting ProcessOrders()");
                return;
            }
            // get lock - ensure that this function ends before it is started again
            lock(lockObj)
            {
                _logger.LogInformation("Process Orders - Broker");
                var date = DateTimeOffset.Now.ToUnixTimeSeconds();
                // process created orders
                var orders = _context.BrokerOrders.Where(o => o.Status == BrokerOrderStatus.Ready.ToString() || o.Status == BrokerOrderStatus.Incomming.ToString() ||
                    o.Status == BrokerOrderStatus.Confirmed.ToString() || o.Status == BrokerOrderStatus.PayoutWait.ToString()).ToList();
                foreach (var order in orders)
                {
                    if (_walletProvider.IsChain(order.AssetSend))
                        ProcessOrderChain(order);
                    else
                        ProcessOrderFiat(order);

                    // we should save changes here because calls to the exchange backend could potentially throw exceptions
                    // which this would leave the backend and frontend in conflicting state (if we save after the loop finishes)
                    // I just eagerly load 'orders' using ToList() at the end of the linq statement to make sure that doesnt get invalidated or anything 
                    _context.SaveChanges();
                }
                // expire orders
                var ordersToExpire = _context.BrokerOrders.Where(o => o.Status == BrokerOrderStatus.Created.ToString() || o.Status == BrokerOrderStatus.Ready.ToString());
                foreach (var order in ordersToExpire)
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
