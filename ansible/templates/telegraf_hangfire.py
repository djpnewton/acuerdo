#!/usr/bin/env python3

import requests

r = requests.get("http://localhost:5000/api/hangfire/Stats")
r.raise_for_status()
print(r.text)
