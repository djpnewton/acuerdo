#!/usr/bin/python3

import sys
import argparse
import requests
import time
import hmac
import hashlib
import base64
import json

URL_BASE = "http://localhost:5000/api/v1/"

EXIT_NO_COMMAND = 1
EXIT_INVALID_SIDE = 2

def construct_parser():
    # construct argument parser
    parser = argparse.ArgumentParser()

    subparsers = parser.add_subparsers(dest="command")

    ## Account / API KEY creation

    parser_acct_create = subparsers.add_parser("account_create", help="Create an account")
    parser_acct_create.add_argument("email", metavar="EMAIL", type=str, help="the email")
    parser_acct_create.add_argument("device_name", metavar="DEVICE_NAME", type=str, help="the name for the created api key")

    parser_acct_create_status = subparsers.add_parser("account_create_status", help="Check an account creation request")
    parser_acct_create_status.add_argument("token", metavar="TOKEN", type=str, help="the token")

    parser_acct_create_cancel = subparsers.add_parser("account_create_cancel", help="Cancel an account creation request")
    parser_acct_create_cancel.add_argument("token", metavar="TOKEN", type=str, help="the token")

    parser_apikey_create = subparsers.add_parser("apikey_create", help="Create an apikey")
    parser_apikey_create.add_argument("email", metavar="EMAIL", type=str, help="the email of the account")
    parser_apikey_create.add_argument("device_name", metavar="DEVICE_NAME", type=str, help="the name for the created apikey")

    parser_apikey_create_status = subparsers.add_parser("apikey_create_status", help="Check an apikey creation request")
    parser_apikey_create_status.add_argument("token", metavar="TOKEN", type=str, help="the token")

    parser_apikey_create_cancel = subparsers.add_parser("apikey_create_cancel", help="Cancel an apikey creation request")
    parser_apikey_create_cancel.add_argument("token", metavar="TOKEN", type=str, help="the token")

    parser_apikey_destroy = subparsers.add_parser("apikey_destroy", help="Destroy apikey")
    parser_apikey_destroy.add_argument("key", metavar="KEY", type=str, help="the key")
    parser_apikey_destroy.add_argument("secret", metavar="SECRET", type=str, help="the secret")

    parser_apikey_validate = subparsers.add_parser("apikey_validate", help="Validate apikey authentication")
    parser_apikey_validate.add_argument("key", metavar="KEY", type=str, help="the apikey key")
    parser_apikey_validate.add_argument("secret", metavar="SECRET", type=str, help="the apikey secret")

    ## Account

    parser_account_balance = subparsers.add_parser("account_balance", help="Show account balance")
    parser_account_balance.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_account_balance.add_argument("secret", metavar="SECRET", type=str, help="the api secret")

    parser_account_kyc = subparsers.add_parser("account_kyc", help="Show account kyc")
    parser_account_kyc.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_account_kyc.add_argument("secret", metavar="SECRET", type=str, help="the api secret")

    parser_account_kyc_upgrade = subparsers.add_parser("account_kyc_upgrade", help="Request to upgrade account kyc")
    parser_account_kyc_upgrade.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_account_kyc_upgrade.add_argument("secret", metavar="SECRET", type=str, help="the api secret")

    parser_account_kyc_upgrade_status = subparsers.add_parser("account_kyc_upgrade_status", help="Get status of request to upgrade account kyc")
    parser_account_kyc_upgrade_status.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_account_kyc_upgrade_status.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_account_kyc_upgrade_status.add_argument("token", metavar="TOKEN", type=str, help="the kyc upgrade request token")

    ## Market

    parser_market_list = subparsers.add_parser("market_list", help="Get the list of markets")

    parser_market_status = subparsers.add_parser("market_status", help="Get the status of a market")
    parser_market_status.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_market_status.add_argument("period", metavar="PERIOD", type=int, nargs="?", default=86400, help="the time period to query in seconds")

    parser_market_detail = subparsers.add_parser("market_detail", help="Get the details of a market")
    parser_market_detail.add_argument("market", metavar="MARKET", type=str, help="the market to query")

    parser_market_depth = subparsers.add_parser("market_depth", help="Get the depth of a market")
    parser_market_depth.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_market_depth.add_argument("merge", metavar="MERGE", type=str, help="the smallest unit to merge (0.1 0.01, 0.001, etc")
    parser_market_depth.add_argument("limit", metavar="LIMIT", type=int, nargs="?", default=20, help="the maximum records to return")

    parser_market_history = subparsers.add_parser("market_history", help="Get the history of a market")
    parser_market_history.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_market_history.add_argument("limit", metavar="LIMIT", type=int, nargs="?", default=20, help="the maximum records to return")

    parser_market_chart = subparsers.add_parser("market_chart", help="Get the candlestick chart data of a market")
    parser_market_chart.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_market_chart.add_argument("start", metavar="START", type=int, help="the start date (unix timestamp)")
    parser_market_chart.add_argument("end", metavar="END", type=int, help="the end date (unix timestamp)")
    parser_market_chart.add_argument("interval", metavar="INTERVAL", help="the interval (time period of each candle) in seconds (must be a multiple of 3600)")

    ## Trade

    parser_order_limit = subparsers.add_parser("order_limit", help="Create a limit order")
    parser_order_limit.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_order_limit.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_order_limit.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_order_limit.add_argument("side", metavar="SIDE", type=str, help="The side to trade ('buy' or 'sell'")
    parser_order_limit.add_argument("amount", metavar="AMOUNT", type=str, help="The amount of the order")
    parser_order_limit.add_argument("price", metavar="PRICE", type=str, help="The price of the order")

    parser_order_market = subparsers.add_parser("order_market", help="Create a market order")
    parser_order_market.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_order_market.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_order_market.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_order_market.add_argument("side", metavar="SIDE", type=str, help="The side to trade ('buy' or 'sell'")
    parser_order_market.add_argument("amount", metavar="AMOUNT", type=str, help="The amount of the order")

    parser_orders_pending = subparsers.add_parser("orders_pending", help="View pending orders")
    parser_orders_pending.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_orders_pending.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_orders_pending.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_orders_pending.add_argument("offset", metavar="OFFSET", type=int, help="The offset")
    parser_orders_pending.add_argument("limit", metavar="LIMIT", type=int, help="The limit")

    parser_orders_executed = subparsers.add_parser("orders_executed", help="View executed orders")
    parser_orders_executed.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_orders_executed.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_orders_executed.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_orders_executed.add_argument("offset", metavar="OFFSET", type=int, help="The offset")
    parser_orders_executed.add_argument("limit", metavar="LIMIT", type=int, help="The limit")

    parser_order_pending_status = subparsers.add_parser("order_pending_status", help="View order pending status")
    parser_order_pending_status.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_order_pending_status.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_order_pending_status.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_order_pending_status.add_argument("id", metavar="ID", type=int, help="Order ID")

    parser_order_executed_status = subparsers.add_parser("order_executed_status", help="View order executed status")
    parser_order_executed_status.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_order_executed_status.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_order_executed_status.add_argument("id", metavar="ID", type=int, help="Order ID")

    parser_order_status = subparsers.add_parser("order_status", help="View order status (pending or executed)")
    parser_order_status.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_order_status.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_order_status.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_order_status.add_argument("id", metavar="ID", type=int, help="Order ID")

    parser_order_cancel = subparsers.add_parser("order_cancel", help="Cancel order")
    parser_order_cancel.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_order_cancel.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_order_cancel.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_order_cancel.add_argument("id", metavar="ID", type=int, help="Order ID")

    parser_trades_executed = subparsers.add_parser("trades_executed", help="View executed trades")
    parser_trades_executed.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_trades_executed.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_trades_executed.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_trades_executed.add_argument("offset", metavar="OFFSET", type=int, help="The offset")
    parser_trades_executed.add_argument("limit", metavar="LIMIT", type=int, help="The limit")

    ## Broker

    parser_broker_markets = subparsers.add_parser("broker_markets", help="Get the list of open broker markets")

    parser_broker_quote = subparsers.add_parser("broker_quote", help="Get a brokerage quote")
    parser_broker_quote.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_broker_quote.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_broker_quote.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_broker_quote.add_argument("side", metavar="SIDE", type=str, help="'buy' or 'sell'")
    parser_broker_quote.add_argument("amount", metavar="AMOUNT", type=str, help="The amount of the asset")
    parser_broker_quote.add_argument("amount_as_quote_currency", metavar="AMOUNT_AS_QUOTE_CURRENCY", type=bool, nargs="?", default=False, help="Denominate the amount parameter as the 'quote currency' of the market ticker")

    parser_broker_create = subparsers.add_parser("broker_create", help="Create a brokerage order")
    parser_broker_create.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_broker_create.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_broker_create.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_broker_create.add_argument("side", metavar="SIDE", type=str, help="'buy' or 'sell'")
    parser_broker_create.add_argument("amount", metavar="AMOUNT", type=str, help="The amount of the asset")
    parser_broker_create.add_argument("amount_as_quote_currency", metavar="AMOUNT_AS_QUOTE_CURRENCY", type=bool, nargs="?", default=False, help="Denominate the amount parameter as the 'quote currency' of the market ticker")
    parser_broker_create.add_argument("recipient", metavar="RECIPIENT", type=str, help="The recipient (cryptocurrency address or bank account number")

    parser_broker_accept = subparsers.add_parser("broker_accept", help="Accept a brokerage order")
    parser_broker_accept.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_broker_accept.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_broker_accept.add_argument("token", metavar="TOKEN", type=str, help="the brokerage order token")

    parser_broker_status = subparsers.add_parser("broker_status", help="Check a brokerage order")
    parser_broker_status.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_broker_status.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_broker_status.add_argument("token", metavar="TOKEN", type=str, help="the brokerage order token")

    parser_broker_orders = subparsers.add_parser("broker_orders", help="Get you list of brokerage orders")
    parser_broker_orders.add_argument("key", metavar="KEY", type=str, help="the api key")
    parser_broker_orders.add_argument("secret", metavar="SECRET", type=str, help="the api secret")
    parser_broker_orders.add_argument("offset", metavar="OFFSET", type=int, help="the offset")
    parser_broker_orders.add_argument("limit", metavar="LIMIT", type=int, help="the limit")
    parser_broker_orders.add_argument("status", metavar="STATUS", type=str, nargs="?", default="", help="the status to filter by")

    ## Debug
    parser_funds_add = subparsers.add_parser("funds_give", help="DEBUG: give funds to a user")
    parser_funds_add.add_argument("email", metavar="EMAIL", type=str, help="the users email address")
    parser_funds_add.add_argument("asset", metavar="ASSET", type=str, help="the asset to give")
    parser_funds_add.add_argument("amount", metavar="AMOunT", type=str, help="the amount to give")

    return parser

def create_sig(api_key, api_secret, message):
    _hmac = hmac.new(api_secret.encode('latin-1'), msg=message.encode('latin-1'), digestmod=hashlib.sha256)
    signature = _hmac.digest()
    signature = base64.b64encode(signature).decode("utf-8")
    return signature

def req(endpoint, params=None, api_key=None, api_secret=None):
    if api_key:
        if not params:
            params = {}
        params["nonce"] = int(time.time())
        params["key"] = api_key
    url = URL_BASE + endpoint
    if params:
        headers = {"Content-type": "application/json"}
        body = json.dumps(params)
        if api_key:
            headers["X-Signature"] = create_sig(api_key, api_secret, body)
        print("   POST - " + url)
        r = requests.post(url, headers=headers, data=body)
    else:
        print("   GET - " + url)
        r = requests.get(url)
    return r

def check_request_status(r):
    try:
        r.raise_for_status()
    except Exception as e:
        print("::ERROR::")
        print(str(r.status_code) + " - " + r.url)
        print(r.text)
        raise e

def account_create(args):
    print(":: calling account create..")
    r = req("AccountCreate", {"email": args.email, "deviceName": args.device_name})
    check_request_status(r)
    token = r.json()["token"]
    print("token: %s" % token)

def account_create_status(args):
    print(":: calling account creation status..")
    r = req("AccountCreateStatus", {"token": args.token})
    check_request_status(r)
    api_key = r.json()
    print(api_key)

def account_create_cancel(args):
    print(":: calling account creation cancel..")
    r = req("AccountCreateCancel", {"token": args.token})
    check_request_status(r)
    print("ok")

def apikey_create(args):
    print(":: calling api key create..")
    r = req("ApiKeyCreate", {"email": args.email, "deviceName": args.device_name})
    check_request_status(r)
    token = r.json()["token"]
    print("token: %s" % token)

def apikey_create_status(args):
    print(":: calling api key creation status..")
    r = req("ApiKeyCreateStatus", {"token": args.token})
    check_request_status(r)
    api_key = r.json()
    print(api_key)

def apikey_create_cancel(args):
    print(":: calling api key creation cancel..")
    r = req("ApiKeyCreateCancel", {"token": args.token})
    check_request_status(r)
    print("ok")

def apikey_destroy(args):
    print(":: calling api key destroy..")
    r = req("ApiKeyDestroy", None, args.key, args.secret)
    check_request_status(r)
    print("ok")

def apikey_validate(args):
    print(":: calling api key validate..")
    r = req("ApiKeyValidate", None, args.key, args.secret)
    check_request_status(r)
    print("ok")

def account_balance(args):
    print(":: calling account balance..")
    r = req("AccountBalance", None, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def account_kyc(args):
    print(":: calling account kyc..")
    r = req("AccountKyc", None, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def account_kyc_upgrade(args):
    print(":: calling account kyc upgrade..")
    r = req("AccountKycUpgrade", None, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def account_kyc_upgrade_status(args):
    print(":: calling account kyc upgrade status..")
    params = {"token": args.token}
    r = req("AccountKycUpgradeStatus", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def market_list(args):
    print(":: calling market list..")
    r = req("MarketList")
    check_request_status(r)
    print(r.text)

def market_status(args):
    print(":: calling market status..")
    params = {"market": args.market}
    if args.period:
        params["period"] = args.period
    r = req("MarketStatus", params)
    check_request_status(r)
    print(r.text)

def market_detail(args):
    print(":: calling market detail..")
    r = req("MarketDetail", {"market": args.market})
    check_request_status(r)
    print(r.text)

def market_depth(args):
    print(":: calling market depth..")
    params = {"market": args.market, "merge": args.merge}
    if args.limit:
        params["limit"] = args.limit
    r = req("MarketDepth", params)
    check_request_status(r)
    print(r.text)

def market_history(args):
    print(":: calling market history..")
    params = {"market": args.market}
    if args.limit:
        params["limit"] = args.limit
    r = req("MarketHistory", params)
    check_request_status(r)
    print(r.text)

def market_chart(args):
    print(":: calling market chart..")
    params = {"market": args.market}
    params["start"] = args.start
    params["end"] = args.end
    params["interval"] = args.interval
    r = req("MarketChart", params)
    check_request_status(r)
    print(r.text)

def order_limit(args):
    print(":: calling order limit..")
    if args.side not in ("buy", "sell"):
        print("ERROR: invalid 'side' parameter")
        sys.exit(EXIT_INVALID_SIDE)
    params = {"market": args.market, "side": args.side, "amount": args.amount, "price": args.price}
    r = req("OrderLimit", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def order_market(args):
    print(":: calling order market..")
    if args.side not in ("buy", "sell"):
        print("ERROR: invalid 'side' parameter")
        sys.exit(EXIT_INVALID_SIDE)
    params = {"market": args.market, "side": args.side, "amount": args.amount}
    r = req("OrderMarket", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def orders_pending(args):
    print(":: calling orders pending..")
    params = {"market": args.market, "offset": args.offset, "limit": args.limit}
    r = req("OrdersPending", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def orders_executed(args):
    print(":: calling orders executed..")
    params = {"market": args.market, "offset": args.offset, "limit": args.limit}
    r = req("OrdersExecuted", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def order_pending_status(args):
    print(":: calling order pending status..")
    params = {"market": args.market, "id": args.id}
    r = req("OrderPendingStatus", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def order_executed_status(args):
    print(":: calling order executed status..")
    params = {"id": args.id}
    r = req("OrderExecutedStatus", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def order_status(args):
    print(":: calling order status..")
    params = {"market": args.market, "id": args.id}
    r = req("OrderStatus", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def order_cancel(args):
    print(":: calling order cancel..")
    params = {"market": args.market, "id": args.id}
    r = req("OrderCancel", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def trades_executed(args):
    print(":: calling trades executed..")
    params = {"market": args.market, "offset": args.offset, "limit": args.limit}
    r = req("TradesExecuted", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def broker_markets(args):
    print(":: calling broker markets..")
    r = req("BrokerMarkets", None)
    check_request_status(r)
    print(r.text)

def broker_quote(args):
    print(":: calling broker quote..")
    params = {"market": args.market, "side": args.side, "amount": args.amount, "amount_as_quote_currency": args.amount_as_quote_currency}
    r = req("BrokerQuote", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def broker_create(args):
    print(":: calling broker create..")
    params = {"market": args.market, "side": args.side, "amount": args.amount, "amount_as_quote_currency": args.amount_as_quote_currency, "recipient": args.recipient}
    r = req("BrokerCreate", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def broker_accept(args):
    print(":: calling broker accept..")
    params = {"token": args.token}
    r = req("BrokerAccept", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def broker_status(args):
    print(":: calling broker status..")
    params = {"token": args.token}
    r = req("BrokerStatus", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)
    print(" - payment address: %s" % r.json()["paymentAddress"])
    print(" - amount: %s" % r.json()["amountSend"])
    print(" - attachment: {\"InvoiceId\":\"%s\"}" % r.json()["invoiceId"])
    addr = r.json()["paymentAddress"]
    asset_id = "CgUrFtinLXEbJwJVjwwcppk4Vpz1nMmR3H5cQaDcUcfe"
    import decimal
    amount = int(decimal.Decimal(r.json()["amountSend"]) * 100)
    attachment = "{\"InvoiceId\":\"%s\"}" % r.json()["invoiceId"]
    uri = "waves://%s?asset=%s&amount=%d&attachment=%s" % (addr, asset_id, amount, attachment)
    print(" - uri: %s" % uri)

def broker_orders(args):
    print(":: calling broker orders..")
    params = {"offset": args.offset, "limit": args.limit, "status": args.status}
    r = req("BrokerOrders", params, args.key, args.secret)
    check_request_status(r)
    print(r.text)

def funds_give(args):
    global URL_BASE
    URL_BASE = "http://localhost:5000/api/dev/"

    print(":: calling funds give..")
    params = {"email": args.email, "asset": args.asset, "amount": args.amount}
    r = req("UserFundGive", params)
    check_request_status(r)
    print(r.text)

if __name__ == "__main__":
    # parse arguments
    parser = construct_parser()
    args = parser.parse_args()

    # set appropriate function
    function = None
    if args.command == "account_create":
        function = account_create
    elif args.command == "account_create_status":
        function = account_create_status
    elif args.command == "account_create_cancel":
        function = account_create_cancel
    elif args.command == "apikey_create":
        function = apikey_create
    elif args.command == "apikey_create_status":
        function = apikey_create_status
    elif args.command == "apikey_create_cancel":
        function = apikey_create_cancel
    elif args.command == "apikey_destroy":
        function = apikey_destroy
    elif args.command == "apikey_validate":
        function = apikey_validate
    elif args.command == "account_balance":
        function = account_balance
    elif args.command == "account_kyc":
        function = account_kyc
    elif args.command == "account_kyc_upgrade":
        function = account_kyc_upgrade
    elif args.command == "account_kyc_upgrade_status":
        function = account_kyc_upgrade_status
    elif args.command == "market_list":
        function = market_list
    elif args.command == "market_status":
        function = market_status
    elif args.command == "market_detail":
        function = market_detail
    elif args.command == "market_depth":
        function = market_depth
    elif args.command == "market_history":
        function = market_history
    elif args.command == "market_chart":
        function = market_chart
    elif args.command == "order_limit":
        function = order_limit
    elif args.command == "order_market":
        function = order_market
    elif args.command == "orders_pending":
        function = orders_pending
    elif args.command == "orders_executed":
        function = orders_executed
    elif args.command == "order_pending_status":
        function = order_pending_status
    elif args.command == "order_executed_status":
        function = order_executed_status
    elif args.command == "order_status":
        function = order_status
    elif args.command == "order_cancel":
        function = order_cancel
    elif args.command == "trades_executed":
        function = trades_executed
    elif args.command == "broker_markets":
        function = broker_markets
    elif args.command == "broker_quote":
        function = broker_quote
    elif args.command == "broker_create":
        function = broker_create
    elif args.command == "broker_accept":
        function = broker_accept
    elif args.command == "broker_status":
        function = broker_status
    elif args.command == "broker_orders":
        function = broker_orders
    elif args.command == "funds_give":
        function = funds_give
    else:
        parser.print_help()
        sys.exit(EXIT_NO_COMMAND)

    if function:
        function(args)
