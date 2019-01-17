#!/bin/bash

set -e

DEPLOY_TEST=test
DEPLOY_PRODUCTION=production
DEPLOY_TYPE=$1

display_usage() { 
    echo -e "\nUsage:

    ansible_deploy.sh <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION)> 

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
if [[ ( $DEPLOY_TYPE != "test" ) &&  ( $DEPLOY_TYPE != "production" ) ]] 
then 
    display_usage
    echo !!\"$DEPLOY_TYPE\" is not valid
    exit 2
fi 

ADMIN_EMAIL=admin@bronze.exchange
VAGRANT=false
# set deploy variables for production
DEPLOY_HOST=bronze.exchange
BACKEND_HOST=backend-internal.bronze.exchange
BLOCKCHAIN_HOST=blockchain-internal.bronze.exchange
DEPLOY_USER=root
TESTNET=
ADMIN_HOST=123.123.123.123

# set deploy variables for test
if [[ ( $DEPLOY_TYPE == "test" ) ]]
then 
    DEPLOY_HOST=test.bronze.exchange
    BACKEND_HOST=backend-internal.test.bronze.exchange
    BLOCKCHAIN_HOST=blockchain-internal.test.bronze.exchange
    DEPLOY_USER=root
    TESTNET=true
fi 

# print variables
echo ":: DEPLOYMENT DETAILS ::"
echo "   - TESTNET: $TESTNET"
echo "   - ADMIN_EMAIL: $ADMIN_EMAIL"
echo "   - ADMIN_HOST: $ADMIN_HOST"
echo "   - DEPLOY_HOST: $DEPLOY_HOST"
echo "   - DEPLOY_USER: $DEPLOY_USER"
echo "   - BACKEND_HOST: $BACKEND_HOST"
echo "   - BLOCKCHAIN_HOST: $BLOCKCHAIN_HOST"

# ask user to continue
read -p "Are you sure? " -n 1 -r
echo # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    # do dangerous stuff
    echo ok lets go!!!
    ansible-playbook --inventory "$DEPLOY_HOST," --user "$DEPLOY_USER" -v \
        --extra-vars "admin_email=$ADMIN_EMAIL deploy_host=$DEPLOY_HOST backend_host=$BACKEND_HOST blockchain_host=$BLOCKCHAIN_HOST vagrant=$VAGRANT testnet=$TESTNET admin_host=$ADMIN_HOST DEPLOY_TYPE=$DEPLOY_TYPE" \
        deploy.yml
fi
