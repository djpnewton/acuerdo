from getgauge.python import step, before_scenario, Messages
from step_impl.utils.driver import Driver
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

@step("Create user <email> <password>")
def create_user(email, password):
    r = req("/api/dev/UserCreate", {"email": email, "password": password, "EmailConfirmed": True, "SendEmail": False})
    r.raise_for_status()

@step("Navigate to <endpoint>")
def navigate_to(endpoint):
    url = BASE_URL + endpoint
    Driver.driver.get(url)

@step("Login with <email> and <password>")
def login_with_email_and_password(email, password):
  driver = Driver.driver
  driver.find_element_by_link_text("Log in").click()
  driver.find_element_by_name("Email").send_keys(email)
  driver.find_element_by_name("Password").send_keys(password)
  driver.find_element_by_css_selector("button[type='submit']").click()
  text = driver.find_element_by_css_selector("div[class='alert alert-success']").text
  assert "Logged in" in text

@step("Check all forms for CSRF protection")
def check_all_forms_for_CSRF_protection():
    driver = Driver.driver
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

# ---------------
# Execution Hooks
# ---------------

@before_scenario()
def before_scenario_hook():
    pass
