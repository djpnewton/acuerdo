#!/usr/bin/env python3

import argparse
import os
import re
import json

parser = argparse.ArgumentParser()
parser.add_argument("--path", required=True, help="wallet test app path")
parser.add_argument("--host", required=True, help="db host")
parser.add_argument("--user", help="db user")
parser.add_argument("--password", help="db password")
parser.add_argument("--name", required=True, help="db name")

args = parser.parse_args()

# get balance
wallet_balance = None
try:
    if args.user:
        conn = "conn|host=%s;uid=%s;password=%s;database=%s" % (args.host, args.user, args.password, args.name)
    else:
        conn = "conn|host=%s;database=%s" % (args.host, args.name)
    os.chdir(args.path)
    stream = os.popen("dotnet test.dll balance -n \"%s\"" % conn)
    out = stream.read()
    for line in out.split("\n"):
        m = re.search("balance: (\d+)", line)
        if m:
            wallet_balance = int(m.group(1))
except:
    pass

key = args.name + "_balance"
d = {key: wallet_balance}
print(json.dumps(d))
