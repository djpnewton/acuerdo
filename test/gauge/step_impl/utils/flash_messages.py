import os
from step_impl.utils.driver import Driver
from getgauge.python import step

@step("Show a message <message> <link_text>")
def show_a_message(message, link_text):
    driver = Driver.driver
    driver.find_element_by_link_text(link_text)
    flash_notice_element = driver.find_element_by_xpath("//div[@id = 'flash_notice' and text() = '" + message + "']")
    assert True, flash_notice_element.is_displayed()
