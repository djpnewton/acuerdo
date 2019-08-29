#!/bin/bash

set -e

discover_service() {
    DIR=$1
    NAME=$2
    RESULT_URL_VAR=$3
    RESULT_API_KEY=$4
    RESULT_API_SECRET_VAR=$5

    # set file vars
    URL_FILE=$DIR/${NAME}_url
    API_KEY=$DIR/${NAME}_api_key
    API_SECRET_FILE=$DIR/${NAME}_api_secret

    # check file exists
    if [ ! -f "$URL_FILE" ]; then
        echo service file \($URL_FILE\) not found
        return 0
    fi

    # read values
    URL=$(cat $URL_FILE)
    API_KEY=$(cat $API_KEY)
    API_SECRET=$(cat $API_SECRET_FILE)

    # set result vars
    printf -v "$RESULT_URL_VAR" '%s' "$URL"
    printf -v "$RESULT_API_KEY" '%s' "$API_KEY"
    printf -v "$RESULT_API_SECRET_VAR" '%s' "$API_SECRET"
    return 0
}
