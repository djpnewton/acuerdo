#!/usr/bin/python3

import sys
import argparse
import requests
import time
import hmac
import hashlib
import base64

URL_BASE = "http://localhost:5000/api/v1/"

EXIT_NO_COMMAND = 1

def construct_parser():
    # construct argument parser
    parser = argparse.ArgumentParser()

    subparsers = parser.add_subparsers(dest="command")

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

    return parser

def req(endpoint, params):
    url = URL_BASE + endpoint
    r = requests.post(url, json=params)
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

def create_sig(args):
    nonce = int(time.time())
    message = str(nonce) + args.device_key
    _hmac = hmac.new(args.device_secret.encode('latin-1'), msg=message.encode('latin-1'), digestmod=hashlib.sha256)
    signature = _hmac.digest()
    signature = base64.b64encode(signature).decode("utf-8")
    return signature, nonce

def device_destroy(args):
    signature, nonce = create_sig(args)
    print(":: calling device destroy..")
    r = req("DeviceDestroy", {"key": args.device_key, "signature": signature, "nonce": nonce})
    check_request_status(r)
    print("ok")

def device_validate(args):
    signature, nonce = create_sig(args)
    print(":: calling device validate..")
    r = req("DeviceValidate", {"key": args.device_key, "signature": signature, "nonce": nonce})
    check_request_status(r)
    print("ok")

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
    else:
        parser.print_help()
        sys.exit(EXIT_NO_COMMAND)

    if function:
        function(args)
