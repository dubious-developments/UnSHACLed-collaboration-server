#!/usr/bin/env python3
"""A collection of fully automated unit tests for the collaboration server"""

import unittest
import requests
import sys
import common_tests
from libclient import log, start_server, request_token, is_authenticated

domain = 'http://localhost:8080'


class TestAuthentication(unittest.TestCase):
    def test_request_token(self):
        """Tests that a token can be requested."""
        token = request_token(domain)
        assert not is_authenticated(domain, token)


def sign_in_user(domain):
    """Signs in a test user. Returns the user's token."""
    login = 'test-user'
    token = request_token(domain)
    login_response = requests.get('%s/auth/auth/%s/%s' % (domain, token, login))
    login_response.raise_for_status()
    return token


if __name__ == '__main__':
    server = start_server(domain, '--mock-content-tracker')
    try:
        token = sign_in_user(domain)
        common_tests.import_unit_tests(domain, token, globals())
        unittest.main()
    finally:
        server.kill()
        log('Stopped server.')
