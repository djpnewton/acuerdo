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

    ## Account / Device creation

    parser_acct_create = subparsers.add_parser("account_create", help="Create an account")
    parser_acct_create.add_argument("email", metavar="EMAIL", type=str, help="the email")
    parser_acct_create.add_argument("device_name", metavar="DEVICE_NAME", type=str, help="the name for the created device")

    parser_acct_create_status = subparsers.add_parser("account_create_status", help="Check an account creation request")
    parser_acct_create_status.add_argument("token", metavar="TOKEN", type=str, help="the token")

    parser_acct_create_cancel = subparsers.add_parser("account_create_cancel", help="Cancel an account creation request")
    parser_acct_create_cancel.add_argument("token", metavar="TOKEN", type=str, help="the token")

    parser_device_create = subparsers.add_parser("device_create", help="Create an device")
    parser_device_create.add_argument("email", metavar="EMAIL", type=str, help="the email of the account")
    parser_device_create.add_argument("device_name", metavar="DEVICE_NAME", type=str, help="the name for the created device")

    parser_device_create_status = subparsers.add_parser("device_create_status", help="Check an device creation request")
    parser_device_create_status.add_argument("token", metavar="TOKEN", type=str, help="the token")

    parser_device_create_cancel = subparsers.add_parser("device_create_cancel", help="Cancel an device creation request")
    parser_device_create_cancel.add_argument("token", metavar="TOKEN", type=str, help="the token")

    parser_dev_destroy = subparsers.add_parser("device_destroy", help="Destroy device")
    parser_dev_destroy.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_dev_destroy.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")

    parser_dev_validate = subparsers.add_parser("device_validate", help="Validate device authentication")
    parser_dev_validate.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_dev_validate.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")

    ## Account

    parser_account_balance = subparsers.add_parser("account_balance", help="Show account balance")
    parser_account_balance.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_account_balance.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
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

    ## Trade
    parser_order_limit = subparsers.add_parser("order_limit", help="Create a limit order")
    parser_order_limit.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_order_limit.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
    parser_order_limit.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_order_limit.add_argument("side", metavar="SIDE", type=str, help="The side to trade ('buy' or 'sell'")
    parser_order_limit.add_argument("amount", metavar="AMOUNT", type=str, help="The amount of the order")
    parser_order_limit.add_argument("price", metavar="PRICE", type=str, help="The price of the order")

    parser_order_market = subparsers.add_parser("order_market", help="Create a market order")
    parser_order_market.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_order_market.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
    parser_order_market.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_order_market.add_argument("side", metavar="SIDE", type=str, help="The side to trade ('buy' or 'sell'")
    parser_order_market.add_argument("amount", metavar="AMOUNT", type=str, help="The amount of the order")

    parser_orders_pending = subparsers.add_parser("orders_pending", help="View pending orders")
    parser_orders_pending.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_orders_pending.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
    parser_orders_pending.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_orders_pending.add_argument("offset", metavar="OFFSET", type=int, help="The offset")
    parser_orders_pending.add_argument("limit", metavar="LIMIT", type=int, help="The limit")

    parser_orders_executed = subparsers.add_parser("orders_executed", help="View executed orders")
    parser_orders_executed.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_orders_executed.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
    parser_orders_executed.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_orders_executed.add_argument("offset", metavar="OFFSET", type=int, help="The offset")
    parser_orders_executed.add_argument("limit", metavar="LIMIT", type=int, help="The limit")

    parser_order_pending_status = subparsers.add_parser("order_pending_status", help="View order pending status")
    parser_order_pending_status.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_order_pending_status.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
    parser_order_pending_status.add_argument("market", metavar="MARKET", type=str, help="The market trade in")
    parser_order_pending_status.add_argument("id", metavar="ID", type=int, help="Order ID")

    parser_order_executed_status = subparsers.add_parser("order_executed_status", help="View order executed status")
    parser_order_executed_status.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_order_executed_status.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
    parser_order_executed_status.add_argument("id", metavar="ID", type=int, help="Order ID")


    parser_order_cancel = subparsers.add_parser("order_cancel", help="Cancel order")
    parser_order_cancel.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_order_cancel.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
    parser_order_cancel.add_argument("id", metavar="ID", type=int, help="Order ID")

    parser_trades_executed = subparsers.add_parser("trades_executed", help="View executed trades")
    parser_trades_executed.add_argument("device_key", metavar="DEVICE_KEY", type=str, help="the device key")
    parser_trades_executed.add_argument("device_secret", metavar="DEVICE_SECRET", type=str, help="the device secret")
    parser_trades_executed.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_trades_executed.add_argument("offset", metavar="OFFSET", type=int, help="The offset")
    parser_trades_executed.add_argument("limit", metavar="LIMIT", type=int, help="The limit")

    return parser

def create_sig(device_key, device_secret, message):
    _hmac = hmac.new(device_secret.encode('latin-1'), msg=message.encode('latin-1'), digestmod=hashlib.sha256)
    signature = _hmac.digest()
    signature = base64.b64encode(signature).decode("utf-8")
    return signature

def req(endpoint, params=None, device_key=None, device_secret=None):
    if device_key:
        if not params:
            params = {}
        params["nonce"] = int(time.time())
        params["key"] = device_key
    url = URL_BASE + endpoint
    if params:
        headers = {"Content-type": "application/json"}
        body = json.dumps(params)
        if device_key:
            headers["X-Signature"] = create_sig(device_key, device_secret, body)
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
    api_device = r.json()
    print(api_device)

def account_create_cancel(args):
    print(":: calling account creation cancel..")
    r = req("AccountCreateCancel", {"token": args.token})
    check_request_status(r)
    print("ok")

def device_create(args):
    print(":: calling device create..")
    r = req("DeviceCreate", {"email": args.email, "deviceName": args.device_name})
    check_request_status(r)
    token = r.json()["token"]
    print("token: %s" % token)

def device_create_status(args):
    print(":: calling device creation status..")
    r = req("DeviceCreateStatus", {"token": args.token})
    check_request_status(r)
    api_device = r.json()
    print(api_device)

def device_create_cancel(args):
    print(":: calling device creation cancel..")
    r = req("DeviceCreateCancel", {"token": args.token})
    check_request_status(r)
    print("ok")

def device_destroy(args):
    print(":: calling device destroy..")
    r = req("DeviceDestroy", None, args.device_key, args.device_secret)
    check_request_status(r)
    print("ok")

def device_validate(args):
    print(":: calling device validate..")
    r = req("DeviceValidate", None, args.device_key, args.device_secret)
    check_request_status(r)
    print("ok")

def account_balance(args):
    print(":: calling account balance..")
    r = req("AccountBalance", None, args.device_key, args.device_secret)
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

def order_limit(args):
    print(":: calling order limit..")
    if args.side not in ("buy", "sell"):
        print("ERROR: invalid 'side' parameter")
        sys.exit(EXIT_INVALID_SIDE)
    params = {"market": args.market, "side": args.side, "amount": args.amount, "price": args.price}
    r = req("OrderLimit", params, args.device_key, args.device_secret)
    check_request_status(r)
    print(r.text)

def order_market(args):
    print(":: calling order market..")
    if args.side not in ("buy", "sell"):
        print("ERROR: invalid 'side' parameter")
        sys.exit(EXIT_INVALID_SIDE)
    params = {"market": args.market, "side": args.side, "amount": args.amount}
    r = req("OrderMarket", params, args.device_key, args.device_secret)
    check_request_status(r)
    print(r.text)

def orders_pending(args):
    print(":: calling orders pending..")
    params = {"market": args.market, "offset": args.offset, "limit": args.limit}
    r = req("OrdersPending", params, args.device_key, args.device_secret)
    check_request_status(r)
    print(r.text)

def orders_executed(args):
    print(":: calling orders executed..")
    params = {"market": args.market, "offset": args.offset, "limit": args.limit}
    r = req("OrdersExecuted", params, args.device_key, args.device_secret)
    check_request_status(r)
    print(r.text)

def order_pending_status(args):
    print(":: calling order pending status..")
    params = {"market": args.market, "id": args.id}
    r = req("OrderPendingStatus", params, args.device_key, args.device_secret)
    check_request_status(r)
    print(r.text)

def order_executed_status(args):
    print(":: calling order executed status..")
    params = {"id": args.id}
    r = req("OrderExecutedStatus", params, args.device_key, args.device_secret)
    check_request_status(r)
    print(r.text)

def order_cancel(args):
    print(":: calling order cancel..")
    params = {"id": args.id}
    r = req("OrderCancel", params, args.device_key, args.device_secret)
    check_request_status(r)
    print(r.text)

def trades_executed(args):
    print(":: calling trades executed..")
    params = {"market": args.market, "offset": args.offset, "limit": args.limit}
    r = req("TradesExecuted", params, args.device_key, args.device_secret)
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
    elif args.command == "device_create":
        function = device_create
    elif args.command == "device_create_status":
        function = device_create_status
    elif args.command == "device_create_cancel":
        function = device_create_cancel
    elif args.command == "device_destroy":
        function = device_destroy
    elif args.command == "device_validate":
        function = device_validate
    elif args.command == "account_balance":
        function = account_balance
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
    elif args.command == "order_cancel":
        function = order_cancel
    elif args.command == "trades_executed":
        function = trades_executed
    else:
        parser.print_help()
        sys.exit(EXIT_NO_COMMAND)

    if function:
        function(args)
