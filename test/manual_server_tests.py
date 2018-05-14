#!/usr/bin/env python3
"""A collection of semi-automated unit tests for the collaboration server"""

import unittest
import requests
import sys
from libtest import log, start_server, request_token, is_authenticated, get_workspace, set_workspace

domain = 'http://193.190.127.184:8042'


def user_sign_in():
    """Requests that the user sign in."""
    # Request a token.
    token = request_token(domain)
    # Get it authenticated by the user.
    print('You need to sign in before the tests can run. '
          'Sign in using your browser and hit enter when you\'re done')
    print('Clicky link: %s/auth/auth/%s' % (domain, token))
    # Read an empty string.
    input()
    # Ensure that the user is actually authenticated.
    if is_authenticated(domain, token):
        print('Authentication successful!')
    else:
        print('Authentication unsuccessful. :<')
        sys.exit(1)
    return token


class TestWorkspace(unittest.TestCase):
    def test_round_trip(self):
        """Tests that a workspace can be round-tripped."""
        # Get the old workspace so we can restore it afterward.
        old_workspace = get_workspace(domain, token)

        # Create a dummy workspace string, test that round-tripping works.
        new_workspace = 'Test workspace!'
        set_workspace(domain, token, new_workspace)
        assert get_workspace(domain, token) == new_workspace

        # Restore the old workspace.
        set_workspace(domain, token, old_workspace)
        assert get_workspace(domain, token) == old_workspace


if __name__ == '__main__':
    global token
    token = user_sign_in()
    unittest.main()
