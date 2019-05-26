# Acuerdo
Acuerdo is a frontend for the [viabtc_exchange_server](https://github.com/viabtc/viabtc_exchange_server) and [xchwallet](https://github.com/djpnewton/xchwallet) projects.

It handles user registration, blockchain/fiat wallets and interfacing with the core exchange server (among other things).

# Running locally

Clone the repo including submodules: `git clone --recurse-submodules https://github.com/djpnewton/acuerdo.git`

## Requirements
 - ansible
 - vagrant
 - mysql command line client
 - python
 - dotnet core

## Create backend servers
 - Go to the ansible directory `cd ansible`
 - Use vagrant to provision a local server for the exchange backend and blockchain clients `vagrant up`
 - Use ansible to initialise the exchange backend `./ansible_deploy_viaxch.sh local`
 - Use ansible to initialse the blockchain clients `./ansible_deploy_blockchains.sh local`

## Create Acuerdo database
 - Create database `mysql --host=10.50.1.100 -uviaxch -pnot_production --execute="create database viafront;"`
 - Init schema using Entity Framework Core `dotnet ef database update`

## Create the wallet databases
 - `python init_wallet_dbs.py appsettings.json xchwallet/xchwallet/ .`

## Init roles
 - `dotnet run -- console initroles`

## Run Acuerdo
 - `dotnet run`

## Mail server

A mail server is created, but it will probably have trouble getting its mail delivered.

To use another authorised mail relay (like gmail) change the 'EmailSender' settings in 'appsettings.json':

```
    "EmailSender": {
        "From": "myusername@gmail.com",
        "SmtpHost": "smtp.gmail.com",
        "SmtpUser": "myusername",
        "SmtpPass": "mypassword",
        "SmtpPort": 587,
        "SmtpSsl":  true
    },
```

## Extra

If you dont want to debug the front end on your host pc but want to run it in your local virtual machine you can do that as well (`./ansible_deploy.sh local`)

You can then access the site at `http://10.50.1.100` and you will not have to manually create the database, wallet dbs, roles etc (but changes to 'appsettings.json' will not be replicated into the virtual machine unless comitted to git so your emails probably wont work by default).
