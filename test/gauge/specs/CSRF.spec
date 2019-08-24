# Forms have CSRF protection
Tags: csrf

We log in and interate through all the pages. Any forms are checked to make sure they have CSRF protection

## Ensure user exists

* DEV: Create user "test@example.com" "Not_prod_123!"

## Login

* Add driver
* Navigate to "/"
* Login with "test@example.com" and "Not_prod_123!"

## Check all forms

* Navigate to "/"
* Check all forms for CSRF protection
