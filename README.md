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
 - Use ansible to initialise the exchange backend `./ansible_deploy_viaxch.sh`
 - Use ansible to initialse the blockchain clients `./ansible_deploy_blockchains.sh`

## Create Acuerdo database
 - Create database `mysql --host=10.50.1.100 -uviaxch -pnot_production --execute="create database viafront;"`
 - Init schema using Entity Framework Core `dotnet ef database update`

## Create the wallet databases
 - `python init_wallet_dbs.py appsettings.json xchwallet/xchwallet/ .`

## Run Acuerdo
 - `dotnet run`

## Mail server
 - a mail server is created, but it will probably have trouble getting its mail delivered
