import threading
from getgauge.python import step, before_scenario, Messages
from step_impl.utils.driver import Driver
from selenium.webdriver.support.select import Select
from selenium.webdriver.support.wait import WebDriverWait
import requests
import json

# --------------------------
# Gauge step implementations
# --------------------------

BASE_URL = "http://localhost:5000"

def req(endpoint, params=None):
    url = BASE_URL + endpoint
    if params:
        headers = {"Content-type": "application/json"}
        body = json.dumps(params)
        print("   POST - " + url)
        r = requests.post(url, headers=headers, data=body)
    else:
        print("   GET - " + url)
        r = requests.get(url)
    return r

@step("DEV: Create user <email> <password>")
def user_create(email, password):
    r = req("/api/dev/UserCreate", {"email": email, "password": password, "EmailConfirmed": True, "SendEmail": False})
    r.raise_for_status()

@step("DEV: Create api key for <email> <key> <secret>")
def api_key_create(email, key, secret):
    r = req("/api/dev/UserApiKeyCreate", {"email": email, "key": key, "secret": secret})
    r.raise_for_status()

@step("DEV: Fund user <email> give <amount> <asset>")
def user_fund(email, amount, asset):
    r = req("/api/dev/UserFundGive", {"email": email, "asset": asset, "amount": amount})
    r.raise_for_status()

@step("DEV: Fund user <email> set <asset> to <amount>")
def user_fund_set(email, asset, amount):
    r = req("/api/dev/UserFundSet", {"email": email, "asset": asset, "amount": amount})
    r.raise_for_status()

def user_fund_get(email, asset):
    r = req("/api/dev/UserFundGet", {"email": email, "asset": asset})
    r.raise_for_status()
    return r.json()["amount"]

@step("DEV: Check <asset> funds of user <email> are <amount>")
def user_fund_check(asset, email, amount):
    r = req("/api/dev/UserFundCheck", {"email": email, "asset": asset, "amount": amount})
    r.raise_for_status()

@step("DEV: Limit buy for <email> in market <market>, <amount> units at price <price>")
def user_limit_buy_order(email, market, amount, price):
    r = req("/api/dev/UserLimitOrder", {"email": email, "market": market, "side": "buy", "amount": amount, "price": price})
    r.raise_for_status()
    return r.json()

@step("DEV: Limit sell for <email> in market <market>, <amount> units at price <price>")
def user_limit_sell_order(email, market, amount, price):
    r = req("/api/dev/UserLimitOrder", {"email": email, "market": market, "side": "sell", "amount": amount, "price": price})
    r.raise_for_status()
    return r.json()

@step("DEV: Market buy for <email> in market <market>, <amount> units")
def user_market_buy_order(email, market, amount):
    r = req("/api/dev/UserMarketOrder", {"email": email, "market": market, "side": "buy", "amount": amount})
    r.raise_for_status()
    return r.json()

@step("DEV: Market sell for <email> in market <market>, <amount> units")
def user_market_sell_order(email, market, amount):
    r = req("/api/dev/UserMarketOrder", {"email": email, "market": market, "side": "sell", "amount": amount})
    r.raise_for_status()
    return r.json()

@step("DEV: Clear all orders on <market>")
def clear_all_orders(market):
    r = req("/api/dev/ClearAllOrders", {"market": market})
    r.raise_for_status() 

@step("DEV: Set maker (<maker>) and taker (<taker>) fee rates")
def taker_fee_set(maker, taker):
    r = req("/api/dev/FeeRatesSet", {"maker": maker, "taker": taker})
    r.raise_for_status() 

@step("DEV: Reset tripwire")
def reset_tripwire():
    r = req("/api/dev/ResetTripwire", {"method_post": True})
    r.raise_for_status()

@step("DEV: Reset withdrawal limit for <email>")
def reset_withdrawal_limit(email):
    r = req("/api/dev/ResetWithdrawalLimit", {"email": email})
    r.raise_for_status() 

@step("Navigate to <endpoint>")
def navigate_to(endpoint):
    for driver in Driver.drivers:
        url = BASE_URL + endpoint
        driver.get(url)

@step("Login with <email> and <password>")
def login_with_email_and_password(email, password):
    for driver in Driver.drivers:
        driver.find_element_by_link_text("Log in").click()
        driver.find_element_by_name("Email").send_keys(email)
        driver.find_element_by_name("Password").send_keys(password)
        driver.find_element_by_css_selector("button[type='submit']").click()
        text = driver.find_element_by_css_selector("div[class='alert alert-success']").text
        assert "Logged in" in text

@step("Check all forms for CSRF protection")
def check_all_forms_for_CSRF_protection():
    driver = Driver.drivers[0]
    driver.set_page_load_timeout(10)
    base_url = driver.current_url
    urls = []
    urls_visited = []

    while True:
        elems = driver.find_elements_by_xpath("//a[@href]")
        for elem in elems:
            url = elem.get_attribute("href")
            if url.startswith(base_url) and (not url in urls) and (not url in urls_visited):
                urls.append(url)
        
        if urls:
            url = urls.pop(0)
            driver.get(url)
            import time
            time.sleep(0.1)
            urls_visited.append(url)

            # check for forms
            forms = driver.find_elements_by_css_selector("form")
            #if forms:
            #    print(url)
            for form in forms:
                # check that form has CSRF protection
                #print("%s - %s - %s" % (form.tag_name, form.id, form.get_attribute("action")))
                anti_crsf_input = form.find_element_by_css_selector("input[name='__RequestVerificationToken']")
                assert anti_crsf_input
                #print("%s - %s" % (anti_crsf_input.tag_name, anti_crsf_input.get_attribute("value")))
        else:
            break

@step("Add driver")
def add_driver():
    Driver.add_driver()

def withdraw(driver, address, amount):
    addr = driver.find_element_by_id("WithdrawalAddress")
    addr.clear()
    addr.send_keys(address)
    amt = driver.find_element_by_id("Amount")
    amt.clear()
    amt.send_keys(amount)
    driver.find_element_by_id("withdraw-form").submit()
    element = WebDriverWait(driver, 10).until(
        lambda x: x.find_element_by_css_selector("div[class='alert alert-success'], div[class='alert alert-danger']"))
    assert "Created withdrawal" in element.text or "Balance not enough" in element.text

def market_sell(driver, market, amount):
    select = Select(driver.find_element_by_id("form-market-type"))
    select.select_by_index(1)
    amt = driver.find_element_by_id("form-market-amount")
    amt.clear()
    amt.send_keys(amount)
    driver.find_element_by_id("market-order-form").submit()
    element = WebDriverWait(driver, 10).until(
        lambda x: x.find_element_by_css_selector("div[class='alert alert-success'], div[class='alert alert-danger']"))
    assert "Market Order Created" in element.text or "Market Order Failed (balance too small)" in element.text

@step("Limit sell and withdraw <amount> of <asset> (to <address>) at the same time on <market> market <times> times (while funding <email>)")
def limit_sell_and_withdraw_at_the_same_time(amount, asset, address, market, times, email):
    if len(Driver.drivers) == 1:
        Driver.add_driver()
    driver1 = Driver.drivers[0]
    driver2 = Driver.drivers[1]

    url_withdraw = BASE_URL + "/Wallet/Withdraw?asset=" + asset
    url_trade = BASE_URL + "/Trade/Trade?market=" + market

    for i in range(int(times)):
        print(i)
        # goto pages
        driver1.get(url_withdraw)
        driver2.get(url_trade)
        # reset tripwire
        reset_tripwire()
        # reset withdrawals
        reset_withdrawal_limit(email)
        # set balance to 1 WAVES
        user_fund_set(email, asset, amount)
        user_fund_check(asset, email, amount)
        # do potential race condition
        t1 = threading.Thread(target=withdraw, args=(driver1, address, amount))
        t2 = threading.Thread(target=market_sell, args=(driver2, market, amount))
        t1.start()
        t2.start()
        t1.join()
        t2.join()
        # check balance
        #import time
        #time.sleep(1)
        amt = user_fund_get(email, asset)
        assert amt == 0, "Users funds are not 0"

@step("Withdraw <amount> of <asset> (to <address>) <times> times (while funding <email>)")
def withdraw_lots(amount, asset, address, times, email):
    driver = Driver.drivers[0]

    url_withdraw = BASE_URL + "/Wallet/Withdraw?asset=" + asset

    for i in range(int(times)):
        print(i)
        # goto withdrawal page
        driver.get(url_withdraw)
        # reset tripwire
        reset_tripwire()
        # reset withdrawals
        reset_withdrawal_limit(email)
        # set balance to 1 WAVES
        user_fund_set(email, asset, amount)
        user_fund_check(asset, email, amount)
        # do potential race condition
        withdraw(driver, address, amount)
        # check balance
        amt = user_fund_get(email, asset)
        assert amt == 0, "Users funds are not 0"

@step("API Limit sell and withdraw <amount> unit at the same time on <market> market <times> times")
def api_limit_sell_and_withdraw_at_the_same_time(amount, market, times):
    assert False, "add implementation code"

# ---------------
# Execution Hooks
# ---------------

@before_scenario()
def before_scenario_hook():
    pass
