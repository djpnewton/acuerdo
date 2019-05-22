from getgauge.python import before_suite, after_suite
from selenium import webdriver

class Driver(object):
    driver = None

    @before_suite
    def init(*params):
        Driver.driver = webdriver.Firefox()

    @after_suite
    def close(*params):
        Driver.driver.close()
