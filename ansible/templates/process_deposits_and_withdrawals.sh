#!/bin/bash

set -e

cd /opt/viafront
bin="dotnet bin/Debug/netcoreapp2.1/viafront3.dll"
log_deposits=/opt/viafront/deposits.log
log_withdrawals=/opt/viafront/withdraws.log
date >> $log_deposits
date >> $log_withdrawals

function process_deposits() {
    asset=$1
    output=`$bin console check_chain_deposits -a $asset`
    if [ ! -z "$output" ]
    then
        echo $output
        echo $output >> $log_deposits
    fi
}

function process_withdrawals() {
    asset=$1
    output=`$bin console show_pending_chain_withdrawals -a $asset`

    while read -r line
    do
        [ -z "$line" ] && continue
        echo ::Processing line::
        echo ::Processing line:: >> $log_withdrawals
        echo $line
        echo $line >> $log_withdrawals
        spendcode=`echo $line | awk -F', ' '{ print $1 }'`
        spendcode=`echo $spendcode | awk -F': ' '{ print $2 }'`

        withdrawal=`$bin console process_chain_withdrawal -a $asset -s $spendcode`
        echo $withdrawal
        echo $withdrawal >> $log_withdrawals
    done <<< "$output"
}

process_deposits "btc"
process_deposits "waves"
process_deposits "zap"

process_withdrawals "btc"
process_withdrawals "waves"
process_withdrawals "zap"
