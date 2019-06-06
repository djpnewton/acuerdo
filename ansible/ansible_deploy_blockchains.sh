#!/bin/bash

DEPLOY_TEST=test
DEPLOY_PRODUCTION=production
DEPLOY_LOCAL=local
DEPLOY_TYPE=$1

display_usage() { 
    echo -e "\nUsage:

    ansible_deploy_blockchains.sh <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION | $DEPLOY_LOCAL)> 

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

ADMIN_EMAIL=admin@bronze.exchange
VAGRANT=false
# set deploy variables for production
DEPLOY_HOST=blockchain.bronze.exchange
DEPLOY_USER=root
TESTNET=
ADMIN_HOST=123.123.123.123

# set deploy variables for test
if [[ ( $DEPLOY_TYPE == "$DEPLOY_TEST" ) ]]
then
    DEPLOY_HOST=blockchain.test.bronze.exchange
    DEPLOY_USER=root
    TESTNET=true
fi 

# set deploy variables for local
if [[ ( $DEPLOY_TYPE == "$DEPLOY_LOCAL" ) ]]
then
    DEPLOY_HOST=10.50.1.100
    DEPLOY_USER=root
    TESTNET=true
fi 

IF_EXTERNAL=eth0
IF_INTERNAL=eth1

# print variables
echo ":: DEPLOYMENT DETAILS ::"
echo "   - TESTNET: $TESTNET"
echo "   - ADMIN_EMAIL: $ADMIN_EMAIL"
echo "   - ADMIN_HOST: $ADMIN_HOST"
echo "   - DEPLOY_HOST: $DEPLOY_HOST"
echo "   - DEPLOY_USER: $DEPLOY_USER"
echo "   - IF_INTERNAL: $IF_INTERNAL"
echo "   - IF_EXTERNAL: $IF_EXTERNAL"


# ask user to continue
read -p "Are you sure? " -n 1 -r
echo # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    # do dangerous stuff
    echo ok lets go!!!
    ansible-playbook --inventory "$DEPLOY_HOST," --user "$DEPLOY_USER" -v \
        --extra-vars "admin_email=$ADMIN_EMAIL deploy_host=$DEPLOY_HOST vagrant=$VAGRANT testnet=$TESTNET admin_host=$ADMIN_HOST DEPLOY_TYPE=$DEPLOY_TYPE if_internal=$IF_INTERNAL if_external=$IF_EXTERNAL" \
        ../xchwallet/ansible/deploy.yml
fi
