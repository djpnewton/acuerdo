#!/bin/bash

set -e

. influxdb.sh
. ssh_users.sh
. service_details.sh

DEPLOY_TEST=test
DEPLOY_PRODUCTION=production
DEPLOY_LOCAL=local
DEPLOY_USER=$1
DEPLOY_TYPE=$2
DEPLOY_LEVEL_VIAFRONT_ONLY=viafront_only
DEPLOY_LEVEL=$3

display_usage() { 
    echo -e "\nUsage:

    ansible_deploy.sh <DEPLOY_USER> <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION | $DEPLOY_LOCAL)> 

    ansible_deploy.sh <DEPLOY_USER> <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION | $DEPLOY_LOCAL)> <DEPLOY_LEVEL ($DEPLOY_LEVEL_VIAFRONT_ONLY)>

        This is a lesser deploy scenario:

        DEPLOY_LEVEL=$DEPLOY_LEVEL_VIAFRONT_ONLY: only update the viafront service
    "
} 

# if less than two arguments supplied, display usage 
if [ $# -le 1 ]
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
if [ $# -ge 3 ]
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
TESTNET=
LOCAL=
ADMIN_HOST=123.123.123.123

clr=$'\e[0m'
# red background for production deploy
color=$'\e[48;2;255;0;0m'

# set deploy variables for test
if [[ ( $DEPLOY_TYPE == "$DEPLOY_TEST" ) ]]
then 
    # yellow background for production deploy
    color=$'\e[48;2;255;255;0m'
    DEPLOY_HOST=test.$DOMAIN
    BACKEND_HOST=backend-internal.test.$DOMAIN
    BLOCKCHAIN_HOST=blockchain-internal.test.$DOMAIN
    INTERNAL_HOST=test-internal.$DOMAIN
    TESTNET=true
fi 
# set deploy variables for local
if [[ ( $DEPLOY_TYPE == "$DEPLOY_LOCAL" ) ]]
then 
    color=$white
    DEPLOY_HOST=10.50.1.100
    BACKEND_HOST=10.50.1.100
    BLOCKCHAIN_HOST=10.50.1.100
    INTERNAL_HOST=10.50.1.100
    TESTNET=true
    LOCAL=true

    INTERNAL_IP=$INTERNAL_HOST
    BACKEND_IP=$BACKEND_HOST
    BLOCKCHAIN_IP=$BLOCKCHAIN_HOST
else
    INTERNAL_IP=`dig +short $INTERNAL_HOST`
    BACKEND_IP=`dig +short $BACKEND_HOST`
    BLOCKCHAIN_IP=`dig +short $BLOCKCHAIN_HOST`
fi 

# read mysql user/pass from local file
MYSQL_USER_FILE=creds/$DEPLOY_TYPE/mysql_user
MYSQL_PASS_FILE=creds/$DEPLOY_TYPE/mysql_pass
MYSQL_USER=$(cat $MYSQL_USER_FILE)
MYSQL_PASS=$(cat $MYSQL_PASS_FILE)

# read influxdb details 
INFLUXDB_DIR=creds/$DEPLOY_TYPE
discover_influxdb $INFLUXDB_DIR INFLUXDB_SERVER INFLUXDB_USER INFLUXDB_PASS

# read ssh users
SSH_USERS_DIR=creds/$DEPLOY_TYPE/ssh_users
discover_ssh_users $SSH_USERS_DIR USE_SSH_USERS SSH_USERS SSH_USER_PUBKEYS

# read kyc server details from local file
KYC_DIR=creds/$DEPLOY_TYPE
discover_service $KYC_DIR kyc KYC_URL KYC_API_KEY KYC_API_SECRET

# read fiat payment server details from local file
FIAT_SERVER_DIR=creds/$DEPLOY_TYPE
discover_service $FIAT_SERVER_DIR fiat_server FIAT_SERVER_URL FIAT_SERVER_API_KEY FIAT_SERVER_API_SECRET

# create git hash
GIT_BRANCH=$(git name-rev --name-only HEAD)
GIT_REMOTE=$(git config branch.$GIT_BRANCH.remote)
GIT_REPO=$(git config --get remote.$GIT_REMOTE.url)
GIT_HASH=$(git rev-parse HEAD)


# print variables
echo $color":: DEPLOYMENT DETAILS ::"$clr
echo "   - DEPLOY_USER:        $DEPLOY_USER"
echo "   - DEPLOY_HOST:        $DEPLOY_HOST"
echo "   - DEPLOY_LEVEL:       $DEPLOY_LEVEL"
echo "   - TESTNET:            $TESTNET"
echo "   - ADMIN_EMAIL:        $ADMIN_EMAIL"
echo "   - ADMIN_HOST:         $ADMIN_HOST"
echo "   - BACKEND_HOST/IP:    $BACKEND_HOST/$BACKEND_IP"
echo "   - BLOCKCHAIN_HOST/IP: $BLOCKCHAIN_HOST/$BLOCKCHAIN_IP"
echo "   - INTERNAL_IP:        $INTERNAL_IP"
echo "   - MYSQL_USER:         $MYSQL_USER"
echo "   - USE_SSH_USERS:      $USE_SSH_USERS"
echo "   - SSH_USERS:          $SSH_USERS"
echo "   - INFLUXDB_SERVER:    $INFLUXDB_SERVER"
echo "   - KYC_URL:            $KYC_URL"
echo "   - FIAT_SERVER_URL:    $FIAT_SERVER_URL"
echo "   - GIT Details:        Branch: $GIT_BRANCH, Remote: $GIT_REMOTE, Commit: $GIT_HASH"
echo "   - GIT REPO:           $GIT_REPO"

# ask user to continue
read -p "Are you sure? " -n 1 -r
echo # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    # do dangerous stuff
    echo ok lets go!!!
    INVENTORY_HOST=$DEPLOY_HOST
    if [[ ( $DEPLOY_TYPE == "$DEPLOY_LOCAL" ) ]]; then 
        DEPLOY_HOST=acuerdo.local
    fi 
    SSH_VARS="{\"use_ssh_users\": $USE_SSH_USERS, \"ssh_users\": $SSH_USERS, \"ssh_user_pubkeys\": $SSH_USER_PUBKEYS}"
    echo "$SSH_VARS" > ssh_vars.json
    ansible-playbook --inventory "$INVENTORY_HOST," --user "$DEPLOY_USER" -v \
        --extra-vars "admin_email=$ADMIN_EMAIL deploy_type=$DEPLOY_TYPE local=$LOCAL deploy_host=$DEPLOY_HOST backend_host=$BACKEND_HOST backend_ip=$BACKEND_IP blockchain_host=$BLOCKCHAIN_HOST blockchain_ip=$BLOCKCHAIN_IP internal_ip=$INTERNAL_IP full_deploy=$FULL_DEPLOY vagrant=$VAGRANT testnet=$TESTNET admin_host=$ADMIN_HOST DEPLOY_TYPE=$DEPLOY_TYPE git_repo=$GIT_REPO git_hash=$GIT_HASH" \
        --extra-vars "mysql_user=$MYSQL_USER mysql_pass=$MYSQL_PASS" \
        --extra-vars "influxdb_server=$INFLUXDB_SERVER influxdb_user=$INFLUXDB_USER influxdb_pass=$INFLUXDB_PASS" \
        --extra-vars "@ssh_vars.json" \
        --extra-vars "kyc_url=$KYC_URL kyc_api_key=$KYC_API_KEY kyc_api_secret=$KYC_API_SECRET" \
        --extra-vars "fiat_server_url=$FIAT_SERVER_URL fiat_server_api_key=$FIAT_SERVER_API_KEY fiat_server_api_secret=$FIAT_SERVER_API_SECRET" \
        deploy.yml
fi
