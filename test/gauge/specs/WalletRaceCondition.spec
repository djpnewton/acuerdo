# Attempt to exploit race condition in withdrawals
Tags: race2, racecondition

## Ensure user exists

* DEV: Create user "a@example.com" "Not_prod_123!"

## Attempt to exploit race conditions using web interface

* Navigate to "/"
* Login with "b@example.com" and "Not_prod_123!"
* Withdraw "1" of "WAVES" (to "3MwR2FzFtbHeds3NzvkbiaYMVNhbtqcfRPU") "10" times (while funding "b@example.com")