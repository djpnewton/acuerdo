#!/usr/bin/env python3

import argparse
import os
import re
import json

def str2bool(v):
    if isinstance(v, bool):
       return v
    if v.lower() in ('yes', 'true', 't', 'y', '1'):
        return True
    elif v.lower() in ('no', 'false', 'f', 'n', '0'):
        return False
    else:
        raise argparse.ArgumentTypeError('Boolean value expected.')

def find_balance_in_stream(stream):
    out = stream.read()
    for line in out.split("\n"):
        m = re.search("balance\s*: (\d+)", line)
        if m:
            return int(m.group(1))

parser = argparse.ArgumentParser()
parser.add_argument("--path", required=True, help="wallet test app path")
parser.add_argument("--host", required=True, help="db host")
parser.add_argument("--user", help="db user")
parser.add_argument("--password", help="db password")
parser.add_argument("--node", required=True, help="blockchain node")
parser.add_argument("--mainnet", required=True, type=str2bool, help="mainnet or testnet")
parser.add_argument("--name", required=True, help="db name")

args = parser.parse_args()

# get balance
wallet_balance = None
wallet_balance_consolidated = None
try:
    if args.user:
        conn = "conn|host=%s;uid=%s;password=%s;database=%s" % (args.host, args.user, args.password, args.name)
    else:
        conn = "conn|host=%s;database=%s" % (args.host, args.name)
    os.chdir(args.path)
    cmd = "dotnet test.dll balance -n \"%s\" --node %s --mainnet %s" % (conn, args.node, args.mainnet)
    stream = os.popen(cmd)
    wallet_balance = find_balance_in_stream(stream)
    cmd = "dotnet test.dll balance -n \"%s\" -t %s --node %s --mainnet %s" % (conn, "Consolidate", args.node, args.mainnet)
    stream = os.popen(cmd)
    wallet_balance_consolidated = find_balance_in_stream(stream)
except:
    pass

d = {args.name + "_balance": wallet_balance, args.name + "_balance_consolidated": wallet_balance_consolidated}
print(json.dumps(d))
