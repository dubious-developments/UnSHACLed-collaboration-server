#!/usr/bin/env python3
"""A collection of semi-automated unit tests for the collaboration server"""

import unittest
import requests
import sys
import random
import libclient
import common_tests

domain = 'http://193.190.127.184:8042'

def user_sign_in():
    """Requests that the user sign in."""
    # Request a token.
    token = libclient.request_token(domain)
    # Get it authenticated by the user.
    libclient.log('You need to sign in before the tests can run. '
                  'Sign in using your browser and hit enter when you\'re done')
    print('Clicky link: %s/auth/auth/%s' % (domain, token))
    # Read an empty string.
    input('>')
    # Ensure that the user is actually authenticated.
    if libclient.is_authenticated(domain, token):
        libclient.log('Authentication successful!')
    else:
        libclient.log('Authentication unsuccessful. :<')
        sys.exit(1)
    return token

if __name__ == '__main__':
    global token
    token = user_sign_in()
    common_tests.import_unit_tests(domain, token, globals())
    unittest.main()
