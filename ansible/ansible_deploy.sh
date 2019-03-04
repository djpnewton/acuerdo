#!/bin/bash

set -e

DEPLOY_TEST=test
DEPLOY_PRODUCTION=production
DEPLOY_TYPE=$1
DEPLOY_LEVEL_VIAFRONT_ONLY=viafront_only
DEPLOY_LEVEL=$2

display_usage() { 
    echo -e "\nUsage:

    ansible_deploy.sh <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION)> 

    ansible_deploy.sh <DEPLOY_TYPE ($DEPLOY_TEST | $DEPLOY_PRODUCTION)> <DEPLOY_LEVEL ($DEPLOY_LEVEL_VIAFRONT_ONLY)>

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
if [[ ( $DEPLOY_TYPE != "test" ) &&  ( $DEPLOY_TYPE != "production" ) ]] 
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

ADMIN_EMAIL=admin@bronze.exchange
VAGRANT=
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
echo "   - BLOCKCHAIN_HOST: $BLOCKCHAIN_HOST"
echo "   - CODE ARCHIVE:    viafront3.zip"

# ask user to continue
read -p "Are you sure? " -n 1 -r
echo # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    # do dangerous stuff
    echo ok lets go!!!
    ansible-playbook --inventory "$DEPLOY_HOST," --user "$DEPLOY_USER" -v \
        --extra-vars "admin_email=$ADMIN_EMAIL deploy_host=$DEPLOY_HOST backend_host=$BACKEND_HOST blockchain_host=$BLOCKCHAIN_HOST full_deploy=$FULL_DEPLOY vagrant=$VAGRANT testnet=$TESTNET admin_host=$ADMIN_HOST DEPLOY_TYPE=$DEPLOY_TYPE" \
        deploy.yml
fi
