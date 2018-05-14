#!/usr/bin/env python3
"""A collection of fully automated unit tests for the collaboration server"""

import unittest
import requests
import sys
from libclient import log, start_server, request_token, is_authenticated

domain = 'http://localhost:8080'


class TestAuthentication(unittest.TestCase):
    def test_request_token(self):
        """Tests that a token can be requested."""
        token = request_token(domain)
        assert is_authenticated(domain, token)


if __name__ == '__main__':
    app_name, client_id, client_secret = sys.argv
    server = start_server(domain, client_id, client_secret)
    try:
        unittest.main(argv=[app_name])
    finally:
        server.kill()
        log('Stopped server.')
