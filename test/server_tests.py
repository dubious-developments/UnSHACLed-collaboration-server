#!/usr/bin/env python3
"""A collection of unit tests for our electronic checkbook system"""

import unittest
import requests

domain = 'http://localhost:8080'

class TestAuthentication(unittest.TestCase):
    def test_request_token(self):
        """Tests that a token can be requested."""
        response = requests.post(domain + '/auth/request-token')
        assert response.text == 'hi'

if __name__ == '__main__':
    unittest.main()
