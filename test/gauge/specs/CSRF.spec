# Forms have CSRF protection

We log in and interate through all the pages. Any forms are checked to make sure they have CSRF protection

## Ensure user exists

* Create user "test@example.com" "Not_prod_123!"

## Login

* Navigate to "/"
* Login with "test@example.com" and "Not_prod_123!"

## Check all forms

* Navigate to "/"
* Check all forms for CSRF protection
