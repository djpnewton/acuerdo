# Attempt to exploit race condition in trading and wallet updates
Tags: race, racecondition

We create two users and fund their accounts.. User A creates a large limit order. User B attempts to exploit race conditions to withdraw more funds then he should be able to. 

## Ensure users exist

* DEV: Create user "a@example.com" "Not_prod_123!"
* DEV: Create user "b@example.com" "Not_prod_123!"

## Fund user A and create limit order

* DEV: Clear all orders on "WAVESNZD"
* DEV: Fund user "a@example.com" give "1000" "NZD"
* DEV: Limit buy for "a@example.com" in market "WAVESNZD", "200" units at price "5"


## Attempt to exploit race conditions using web interface

* Add driver
* Navigate to "/"
* Login with "b@example.com" and "Not_prod_123!"
* Limit sell and withdraw "1" of "WAVES" (to "3MwR2FzFtbHeds3NzvkbiaYMVNhbtqcfRPU") at the same time on "WAVESNZD" market "10" times (while funding "b@example.com")

## Attempt to exploit race conditions using API

* DEV: Create api key for "b@example.com" "USER_B_KEY" "SECRET"
* API Limit sell and withdraw "1" unit at the same time on "WAVESNZD" market "10" times
