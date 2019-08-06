#!/usr/bin/env python3

import argparse
import os
import time
import json

parser = argparse.ArgumentParser()
parser.add_argument("--output", required=True, help="output file to add entry to")

args = parser.parse_args()

user = os.environ["PAM_USER"]
ip = os.environ["PAM_RHOST"]
service = os.environ["PAM_SERVICE"]
tty = os.environ["PAM_TTY"]
date = time.time()

# create dir
directory = os.path.dirname(args.output)
os.system("mkdir -p %s" % directory)
os.system("chown telegraf:telegraf %s" % directory)
# read data
data = None
try:
    data = open(args.output, "r").read()
except:
    pass
# init entries
entries = []
# load entries from file data
if data:
    entries = json.loads(data)
# add new entry
entry = {"user": user, "ip": ip, "service": service, "tty": tty, "date": date}
entries.append(entry)
# write new data
data = json.dumps(entries)
open(args.output, "w").write(data)
# set ownership to telegraf
os.system("chown telegraf:telegraf %s" % args.output)
