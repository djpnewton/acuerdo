# Test that market orders execute correctly
Tags: marketorders

We create user A and user B, we select a market, clear all orders and then construct various market order scenarios and check that the results agree with what we expect

## Ensure users exists, set fee rates, clear orders

* DEV: Create user "a@example.com" "Not_prod_123!"
* DEV: Create user "b@example.com" "Not_prod_123!"
* DEV: Set maker ("0.1") and taker ("0.1") fee rates
* DEV: Clear all orders on "WAVESNZD"

## Fund user A and create limit buy order

* DEV: Fund user "a@example.com" set "NZD" to "1000"
* DEV: Fund user "a@example.com" set "WAVES" to "0"
* DEV: Limit buy for "a@example.com" in market "WAVESNZD", "200" units at price "5"

## Fund user B and create market sell order

* DEV: Fund user "b@example.com" set "NZD" to "0"
* DEV: Fund user "b@example.com" set "WAVES" to "200"
* DEV: Market sell for "b@example.com" in market "WAVESNZD", "10" units
* DEV: Clear all orders on "WAVESNZD"
* DEV: Check "WAVES" funds of user "a@example.com" are "9"
* DEV: Check "NZD" funds of user "a@example.com" are "950"
* DEV: Check "WAVES" funds of user "b@example.com" are "190"
* DEV: Check "NZD" funds of user "b@example.com" are "45"

## User A limit sell order

* DEV: Clear all orders on "WAVESNZD"
* DEV: Limit sell for "a@example.com" in market "WAVESNZD", "9" units at price "5"

## User B market buy order

* DEV: Market buy for "b@example.com" in market "WAVESNZD", "9" units
* DEV: Clear all orders on "WAVESNZD"
* DEV: Check "WAVES" funds of user "a@example.com" are "0"
* DEV: Check "NZD" funds of user "a@example.com" are "990.5"
* DEV: Check "WAVES" funds of user "b@example.com" are "198.1"
* DEV: Check "NZD" funds of user "b@example.com" are "0"

## Market orders between user A and B

* DEV: Perform market orders on "WAVESNZD" market with users "a@example.com" and "b@example.com" (asset "WAVES" and priced in "NZD"