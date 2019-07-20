#!/bin/bash

set -e

. influxdb.sh
. ssh_users.sh

DEPLOY_TEST=test
DEPLOY_PRODUCTION=production
DEPLOY_LOCAL=local
DEPLOY_USER=$1
DEPLOY_TYPE=$2

display_usage() { 
    echo -e "\nUsage:

    ansible_deploy_blockchains.sh <DEPLOY_USER> <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION | $DEPLOY_LOCAL)> 
    "
} 

# if less than two arguments supplied, display usage 
if [  $# -le 0 ]
then 
    display_usage
    exit 1
fi 

# check whether user had supplied -h or --help . If yes display usage 
if [[ ( $@ == "--help" ) ||  ( $@ == "-h" ) ]] 
then 
    display_usage
    exit 0
fi 

# check whether user has a valid DEPLOY_TYPE
if [[ ( $DEPLOY_TYPE != "$DEPLOY_TEST" ) &&  ( $DEPLOY_TYPE != "$DEPLOY_PRODUCTION" ) && ( $DEPLOY_TYPE != "$DEPLOY_LOCAL" ) ]] 
then 
    display_usage
    echo !!\"$DEPLOY_TYPE\" is not valid
    exit 2
fi 

DOMAIN=example.com #TODO replace example.com with your domain
ADMIN_EMAIL=admin@$DOMAIN
VAGRANT=false
# set deploy variables for production
DEPLOY_HOST=blockchain.$DOMAIN
DEPLOY_HOST_INTERNAL=blockchain-internal.$DOMAIN
FRONTEND_HOST=internal.$DOMAIN
TESTNET=
ADMIN_HOST=123.123.123.123

# set deploy variables for test
if [[ ( $DEPLOY_TYPE == "$DEPLOY_TEST" ) ]]
then
    DEPLOY_HOST=blockchain.test.$DOMAIN
    DEPLOY_HOST_INTERNAL=blockchain-internal.test.$DOMAIN
    FRONTEND_HOST=test-internal.$DOMAIN
    TESTNET=true
fi 

# set deploy variables for local
if [[ ( $DEPLOY_TYPE == "$DEPLOY_LOCAL" ) ]]
then
    DEPLOY_HOST=10.50.1.100
    DEPLOY_HOST_INTERNAL=10.50.1.100
    FRONTEND_HOST=$DEPLOY_HOST
    TESTNET=true
fi 

IF_EXTERNAL=eth0
IF_INTERNAL=eth1

# read influxdb details 
INFLUXDB_DIR=creds/$DEPLOY_TYPE
discover_influxdb $INFLUXDB_DIR INFLUXDB_SERVER INFLUXDB_USER INFLUXDB_PASS

# read ssh users
SSH_USERS_DIR=creds/$DEPLOY_TYPE/ssh_users
discover_ssh_users $SSH_USERS_DIR USE_SSH_USERS SSH_USERS SSH_USER_PUBKEYS

# print variables
echo ":: DEPLOYMENT DETAILS ::"
echo "   - DEPLOY_USER:     $DEPLOY_USER"
echo "   - DEPLOY_HOST:     $DEPLOY_HOST"
echo "   - TESTNET:         $TESTNET"
echo "   - ADMIN_EMAIL:     $ADMIN_EMAIL"
echo "   - ADMIN_HOST:      $ADMIN_HOST"
echo "   - IF_INTERNAL:     $IF_INTERNAL"
echo "   - IF_EXTERNAL:     $IF_EXTERNAL"
echo "   - USE_SSH_USERS:   $USE_SSH_USERS"
echo "   - SSH_USERS:       $SSH_USERS"
echo "   - INFLUXDB_SERVER: $INFLUXDB_SERVER"

# ask user to continue
read -p "Are you sure? " -n 1 -r
echo # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    # do dangerous stuff
    echo ok lets go!!!
    SSH_VARS="{\"use_ssh_users\": $USE_SSH_USERS, \"ssh_users\": $SSH_USERS, \"ssh_user_pubkeys\": $SSH_USER_PUBKEYS}"
    echo "$SSH_VARS" > ssh_vars.json
    ansible-playbook --inventory "$DEPLOY_HOST," --user "$DEPLOY_USER" -v \
        --extra-vars "admin_email=$ADMIN_EMAIL deploy_host=$DEPLOY_HOST smtp_host=$DEPLOY_HOST_INTERNAL smtp_relay_host=$FRONTEND_HOST vagrant=$VAGRANT testnet=$TESTNET admin_host=$ADMIN_HOST deploy_type=$DEPLOY_TYPE if_internal=$IF_INTERNAL if_external=$IF_EXTERNAL" \
        --extra-vars "influxdb_server=$INFLUXDB_SERVER influxdb_user=$INFLUXDB_USER influxdb_pass=$INFLUXDB_PASS" \        
        --extra-vars "@ssh_vars.json" \
        ../xchwallet/ansible/deploy.yml
fi
