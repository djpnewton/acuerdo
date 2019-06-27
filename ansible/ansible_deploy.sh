#!/bin/bash

set -e

DEPLOY_TEST=test
DEPLOY_PRODUCTION=production
DEPLOY_LOCAL=local
DEPLOY_TYPE=$1
DEPLOY_LEVEL_VIAFRONT_ONLY=viafront_only
DEPLOY_LEVEL=$2

display_usage() { 
    echo -e "\nUsage:

    ansible_deploy.sh <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION | $DEPLOY_LOCAL)> 

    ansible_deploy.sh <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION | $DEPLOY_LOCAL)> <DEPLOY_LEVEL ($DEPLOY_LEVEL_VIAFRONT_ONLY)>

        This is a lesser deploy scenario:

        DEPLOY_LEVEL=$DEPLOY_LEVEL_VIAFRONT_ONLY: only update the viafront service
    "
} 

# if less than two arguments supplied, display usage 
if [ $# -le 0 ]
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

# check whether we have a valid deploy level
if [ $# -ge 2 ]
then 
    # a lesser deployment
    if [[ ( "$DEPLOY_LEVEL" != "$DEPLOY_LEVEL_VIAFRONT_ONLY" ) ]]
    then
        display_usage
        echo !!\"$DEPLOY_LEVEL\" is not valid
        exit 2
    fi
    FULL_DEPLOY=
else
    DEPLOY_LEVEL=full
    FULL_DEPLOY=true
fi 

DOMAIN=example.com #TODO replace example.com with your domain
ADMIN_EMAIL=admin@$DOMAIN
VAGRANT=
# set deploy variables for production
DEPLOY_HOST=$DOMAIN
BACKEND_HOST=backend-internal.$DOMAIN
BLOCKCHAIN_HOST=blockchain-internal.$DOMAIN
INTERNAL_HOST=internal.$DOMAIN
DEPLOY_USER=root
TESTNET=
LOCAL=
ADMIN_HOST=123.123.123.123

# set deploy variables for test
if [[ ( $DEPLOY_TYPE == "$DEPLOY_TEST" ) ]]
then 
    DEPLOY_HOST=test.$DOMAIN
    BACKEND_HOST=backend-internal.test.$DOMAIN
    BLOCKCHAIN_HOST=blockchain-internal.test.$DOMAIN
    INTERNAL_HOST=test-internal.$DOMAIN
    DEPLOY_USER=root
    TESTNET=true
fi 
INTERNAL_IP=`dig +short $INTERNAL_HOST`
BACKEND_IP=`dig +short $BACKEND_HOST`
BLOCKCHAIN_IP=`dig +short $BLOCKCHAIN_HOST`
# set deploy variables for local
if [[ ( $DEPLOY_TYPE == "$DEPLOY_LOCAL" ) ]]
then 
    DEPLOY_HOST=10.50.1.100
    BACKEND_HOST=10.50.1.100
    BLOCKCHAIN_HOST=10.50.1.100
    INTERNAL_HOST=10.50.1.100
    DEPLOY_USER=root
    TESTNET=true
    LOCAL=true

    INTERNAL_IP=$INTERNAL_HOST
    BACKEND_IP=$BACKEND_HOST
    BLOCKCHAIN_IP=$BLOCKCHAIN_HOST
fi 

# read mysql user/pass from local file
MYSQL_USER_FILE=creds/$DEPLOY_TYPE/mysql_user
MYSQL_PASS_FILE=creds/$DEPLOY_TYPE/mysql_pass
MYSQL_USER=$(cat $MYSQL_USER_FILE)
MYSQL_PASS=$(cat $MYSQL_PASS_FILE)

# read kyc server details from local file
KYC_URL_FILE=creds/$DEPLOY_TYPE/kyc_url
KYC_API_KEY_FILE=creds/$DEPLOY_TYPE/kyc_api_key
KYC_API_SECRET_FILE=creds/$DEPLOY_TYPE/kyc_api_secret
KYC_URL=$(cat $KYC_URL_FILE)
KYC_API_KEY=$(cat $KYC_API_KEY_FILE)
KYC_API_SECRET=$(cat $KYC_API_SECRET_FILE)

# create archive
(cd ../; ./git-archive-all.sh --format zip --tree-ish HEAD)

# print variables
echo ":: DEPLOYMENT DETAILS ::"
echo "   - DEPLOY_HOST:     $DEPLOY_HOST"
echo "   - DEPLOY_LEVEL:    $DEPLOY_LEVEL"
echo "   - TESTNET:         $TESTNET"
echo "   - ADMIN_EMAIL:     $ADMIN_EMAIL"
echo "   - ADMIN_HOST:      $ADMIN_HOST"
echo "   - DEPLOY_USER:     $DEPLOY_USER"
echo "   - BACKEND_HOST:    $BACKEND_HOST"
echo "   - BACKEND_IP:      $BACKEND_IP"
echo "   - BLOCKCHAIN_HOST: $BLOCKCHAIN_HOST"
echo "   - BLOCKCHAIN_IP:   $BLOCKCHAIN_IP"
echo "   - INTERNAL_IP:     $INTERNAL_IP"
echo "   - MYSQL_USER:      $MYSQL_USER"
echo "   - MYSQL_PASS:      *${#MYSQL_PASS} chars*"
echo "   - KYC_URL:         $KYC_URL"
echo "   - CODE ARCHIVE:    acuerdo.zip"

# ask user to continue
read -p "Are you sure? " -n 1 -r
echo # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    # do dangerous stuff
    echo ok lets go!!!
    INVENTORY_HOST=$DEPLOY_HOST
    if [[ ( $DEPLOY_TYPE == "$DEPLOY_LOCAL" ) ]]
    then 
        DEPLOY_HOST=acuerdo.local
    fi 
    ansible-playbook --inventory "$INVENTORY_HOST," --user "$DEPLOY_USER" -v \
        --extra-vars "admin_email=$ADMIN_EMAIL deploy_type=$DEPLOY_TYPE local=$LOCAL deploy_host=$DEPLOY_HOST backend_host=$BACKEND_HOST backend_ip=$BACKEND_IP blockchain_host=$BLOCKCHAIN_HOST blockchain_ip=$BLOCKCHAIN_IP internal_ip=$INTERNAL_IP full_deploy=$FULL_DEPLOY vagrant=$VAGRANT testnet=$TESTNET admin_host=$ADMIN_HOST DEPLOY_TYPE=$DEPLOY_TYPE mysql_user=$MYSQL_USER mysql_pass=$MYSQL_PASS kyc_url=$KYC_URL kyc_api_key=$KYC_API_KEY kyc_api_secret=$KYC_API_SECRET" \
        deploy.yml
fi
