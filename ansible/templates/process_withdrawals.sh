#!/bin/bash

set -e

cd /opt/viafront
bin="dotnet bin/Debug/netcoreapp2.1/viafront3.dll"
log=/opt/viafront/withdraws.log
date >> $log

function process_withdrawals() {
    asset=$1
    output=`$bin console show_pending_chain_withdrawals -a $asset`

    while read -r line
    do
        [ -z "$line" ] && continue
        echo ::Processing line::
        echo ::Processing line:: >> $log
        echo $line
        echo $line >> $log
        spendcode=`echo $line | awk -F', ' '{ print $1 }'`
        spendcode=`echo $spendcode | awk -F': ' '{ print $2 }'`

        withdrawal=`$bin console process_chain_withdrawal -a $asset -s $spendcode`
        echo $withdrawal
        echo $withdrawal >> $log
    done <<< "$output"
}

process_withdrawals "btc"
process_withdrawals "waves"
process_withdrawals "zap"
