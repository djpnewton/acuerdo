#!/usr/bin/env python3

import argparse
import os
import re
import json

parser = argparse.ArgumentParser()
parser.add_argument("--path", required=True, help="the file to read/delete")

args = parser.parse_args()

if os.path.exists(args.path):
    with open(args.path, "r") as f:
        data = f.read()
    os.remove(args.path)
    print(data)
