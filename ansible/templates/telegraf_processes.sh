#!/bin/bash

### The idea is to run the systemctl and filter the result.
### output is json format that telgraf would send to influxdb.

status=$(systemctl status $1 | awk '/Active:/ {print $2}')

if [ $status == "active" ]; then
        service=$1
        status='1'
        echo "["
        echo $service $status | awk '{if (NR!=1) {printf ",\n"};printf "  { \""$1"\": "$2" }"}'
        echo
        echo "]"
else
        service=$1
        status='0'
        echo "["
        echo $service $status | awk '{if (NR!=1) {printf ",\n"};printf "  { \""$1"\": "$2" }"}'
        echo
        echo "]"
fi

