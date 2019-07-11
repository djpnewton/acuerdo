#!/bin/bash

set -e

discover_ssh_users() {
    DIR=$1
    RESULT_USE_VAR=$2
    RESULT_NAMES_VAR=$3
    RESULT_KEYS_VAR=$4

    # set use var to false
    printf -v "$RESULT_USE_VAR" '%s' "false"
    printf -v "$RESULT_NAMES_VAR" '%s' "[]"
    printf -v "$RESULT_KEYS_VAR" '%s' "[]"

    # check directory exists
    if [ ! -d "$DIR" ]; then
        echo SSH user dir \($DIR\) not found
        return 0
    fi

    # iterate over filenames in directory
    RESULT_NAMES="["
    RESULT_PUBKEYS="["
    for entry in "$DIR"/*; do
        # set use var to true if there are any items in the directory 
        printf -v "$RESULT_USE_VAR" '%s' "true"

        name=$(basename $entry)
        fullpath=$(realpath $entry)
        RESULT_NAMES="$RESULT_NAMES\"$name\","
        RESULT_PUBKEYS="$RESULT_PUBKEYS{\"name\":\"$name\",\"pubkey\":\"$fullpath\"},"
    done
    if [[ "${RESULT_NAMES: -1}" == "," ]]; then
        RESULT_NAMES=${RESULT_NAMES::-1}
        RESULT_PUBKEYS=${RESULT_PUBKEYS::-1}
    fi
    RESULT_NAMES="$RESULT_NAMES]"
    RESULT_PUBKEYS="$RESULT_PUBKEYS]"

    # set result vars
    printf -v "$RESULT_NAMES_VAR" '%s' "$RESULT_NAMES"
    printf -v "$RESULT_KEYS_VAR" '%s' "$RESULT_PUBKEYS"
    return 0
}
