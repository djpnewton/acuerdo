#!/usr/bin/python3

import os
import sys
import argparse
import requests
import json
import decimal

URL_BASE = "http://localhost:5000/api/v1/"

EXIT_NO_COMMAND = 1

def construct_parser():
    # construct argument parser
    parser = argparse.ArgumentParser()

    subparsers = parser.add_subparsers(dest="command")

    ## Market

    parser_market_depth = subparsers.add_parser("market_depth", help="Get the depth of a market")
    parser_market_depth.add_argument("market", metavar="MARKET", type=str, help="the market to query")
    parser_market_depth.add_argument("merge", metavar="MERGE", type=str, help="the smallest unit to merge (0.1 0.01, 0.001, etc")
    parser_market_depth.add_argument("limit", metavar="LIMIT", type=int, nargs="?", default=20, help="the maximum records to return")

    return parser

def req(endpoint, params=None):
    body_without_auth = ""
    if params:
        body_without_auth = json.dumps(params)
    url = URL_BASE + endpoint
    if params:
        headers = {"Content-type": "application/json"}
        body = json.dumps(params)
        ##print("   POST - " + url + " - " + body_without_auth)
        r = requests.post(url, headers=headers, data=body)
    else:
        ##print("   GET - " + url)
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

def market_depth(args):
    ##print(":: calling market depth..")
    params = {"market": args.market, "merge": args.merge}
    if args.limit:
        params["limit"] = args.limit
    r = req("MarketDepth", params)
    check_request_status(r)
    asks = r.json()["asks"]
    bids = r.json()["bids"]
    asks_total = decimal.Decimal(0)
    bids_total = decimal.Decimal(0)
    for ask in asks:
        asks_total += decimal.Decimal(ask[1])
    for bid in bids:
        bids_total += decimal.Decimal(bid[1])
    print(json.dumps(dict(asks_total=float(asks_total), bids_total=float(bids_total))))

if __name__ == "__main__":
    # parse arguments
    parser = construct_parser()
    args = parser.parse_args()

    # set appropriate function
    function = None
    if args.command == "market_depth":
        function = market_depth
    else:
        parser.print_help()
        sys.exit(EXIT_NO_COMMAND)

    if function:
        function(args)
