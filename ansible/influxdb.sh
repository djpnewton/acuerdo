#!/bin/bash

set -e

discover_influxdb() {
    DIR=$1
    RESULT_SERVER_VAR=$2
    RESULT_USER_VAR=$3
    RESULT_PASS_VAR=$4

    # set file vars
    INFLUXDB_SERVER_FILE=$DIR/influxdb_server
    INFLUXDB_USER_FILE=$DIR/influxdb_user
    INFLUXDB_PASS_FILE=$DIR/influxdb_pass

    # check server file exists
    if [ ! -f "$INFLUXDB_SERVER_FILE" ]; then
        echo influxdb server file \($INFLUXDB_SERVER_FILE\) not found
        return 0
    fi

    # read values
    INFLUXDB_SERVER=$(cat $INFLUXDB_SERVER_FILE)
    INFLUXDB_USER=$(cat $INFLUXDB_USER_FILE)
    INFLUXDB_PASS=$(cat $INFLUXDB_PASS_FILE)

    # set result vars
    printf -v "$RESULT_SERVER_VAR" '%s' "$INFLUXDB_SERVER"
    printf -v "$RESULT_USER_VAR" '%s' "$INFLUXDB_USER"
    printf -v "$RESULT_PASS_VAR" '%s' "$INFLUXDB_PASS"
    return 0
}
