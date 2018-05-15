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
        assert not is_authenticated(domain, token)


if __name__ == '__main__':
    server = start_server(domain, '--mock-content-tracker')
    try:
        unittest.main()
    finally:
        server.kill()
        log('Stopped server.')
