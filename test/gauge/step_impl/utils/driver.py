from getgauge.python import before_suite, after_suite
from selenium import webdriver

class Driver(object):
    drivers = []

    @before_suite
    def init(*params):
        driver = webdriver.Firefox()
        Driver.drivers.append(driver)

    @after_suite
    def close(*params):
        for driver in Driver.drivers:
            driver.close()

    def add_driver():
        driver = webdriver.Firefox()
        Driver.drivers.append(driver)