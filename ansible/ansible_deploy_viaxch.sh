#!/bin/bash

DEPLOY_TEST=test
DEPLOY_PRODUCTION=production
DEPLOY_LOCAL=local
DEPLOY_TYPE=$1

display_usage() { 
    echo -e "\nUsage:

    ansible_deploy.sh <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION | $DEPLOY_LOCAL)> 

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
ALERT_EMAIL=alert@bronze.exchange
VAGRANT=false

# set deploy variables for production
DEPLOY_HOST=backend.bronze.exchange
FRONTEND_HOST=internal.bronze.exchange
KAFKA_ADVERTISED_LISTENER=backend-internal.bronze.exchange
DEPLOY_USER=root
TESTNET=
# set deploy variables for test
if [[ ( $DEPLOY_TYPE == "$DEPLOY_TEST" ) ]]
then 
    DEPLOY_HOST=backend.test.bronze.exchange
    FRONTEND_HOST=test-internal.bronze.exchange
    KAFKA_ADVERTISED_LISTENER=backend-internal.test.bronze.exchange
    DEPLOY_USER=root
    TESTNET=true
fi 
ADMIN_HOST=`dig +short $FRONTEND_HOST`
# set deploy variables for local
if [[ ( $DEPLOY_TYPE == "$DEPLOY_LOCAL" ) ]]
then 
    DEPLOY_HOST=10.50.1.100
    FRONTEND_HOST=10.0.2.2 # https://gist.github.com/lsloan/6f4307a2cab2aaa16feb323adf8d7959
    KAFKA_ADVERTISED_LISTENER=10.50.1.100
    DEPLOY_USER=root
    TESTNET=true
    ADMIN_HOST=$FRONTEND_HOST
fi 

AUTH_URL=http://$FRONTEND_HOST:5000/Internal/WebsocketAuth

MYSQL_HOST=127.0.0.1
REDIS_HOST=127.0.0.1
KAFKA_HOST=127.0.0.1
MATCH_HOST=127.0.0.1
PRICE_HOST=127.0.0.1
DATA_HOST=127.0.0.1
HTTP_HOST=127.0.0.1
WS_HOST=127.0.0.1
ALERT_HOST=127.0.0.1

ROOT_DIR=/opt/viabtc
CONF_DIR=/opt/viabtc_conf
MYSQL_USER=viaxch
MYSQL_PASS=not_production
REDIS_PASS=

IF_EXTERNAL=eth0
IF_INTERNAL=eth1

# create archive
(cd ../viabtc_exchange_server; git archive --format=zip HEAD > viabtc_xch.zip)

# print variables
echo ":: DEPLOYMENT DETAILS ::"
echo "   - TESTNET: $TESTNET"
echo "   - ADMIN_EMAIL: $ADMIN_EMAIL"
echo "   - ADMIN_HOST: $ADMIN_HOST"
echo "   - DEPLOY_HOST: $DEPLOY_HOST"
echo "   - DEPLOY_USER: $DEPLOY_USER"
echo "   - CODE ARCHIVE: viabtc_xch.zip"

# ask user to continue
read -p "Are you sure? " -n 1 -r
echo # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    # do dangerous stuff
    echo ok lets go!!!
    ansible-playbook --inventory "$DEPLOY_HOST," --user "$DEPLOY_USER" -v \
        --extra-vars "admin_email=$ADMIN_EMAIL deploy_type=$DEPLOY_TYPE deploy_host=$DEPLOY_HOST vagrant=$VAGRANT testnet=$TESTNET admin_host=$ADMIN_HOST mysql_host=$MYSQL_HOST redis_host=$REDIS_HOST kafka_host=$KAFKA_HOST match_host=$MATCH_HOST price_host=$PRICE_HOST data_host=$DATA_HOST http_host=$HTTP_HOST ws_host=$WS_HOST alert_host=$ALERT_HOST root_dir=$ROOT_DIR conf_dir=$CONF_DIR mysql_user=$MYSQL_USER mysql_pass=$MYSQL_PASS mysql_user_match_host=$MATCH_HOST mysql_user_data_host=$DATA_HOST redis_pass=$REDIS_PASS auth_url=$AUTH_URL kafka_advertised_listener=$KAFKA_ADVERTISED_LISTENER alert_email=$ALERT_EMAIL if_external=$IF_EXTERNAL if_internal=$IF_INTERNAL" \
        ../viabtc_exchange_server/provisioning/deploy.yml
fi
