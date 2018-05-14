#!/usr/bin/env python3
"""A collection of semi-automated unit tests for the collaboration server"""

import unittest
import requests
import sys
import libtest

domain = 'http://193.190.127.184:8042'


def user_sign_in():
    """Requests that the user sign in."""
    # Request a token.
    token = libtest.request_token(domain)
    # Get it authenticated by the user.
    print('You need to sign in before the tests can run. '
          'Sign in using your browser and hit enter when you\'re done')
    print('Clicky link: %s/auth/auth/%s' % (domain, token))
    # Read an empty string.
    input()
    # Ensure that the user is actually authenticated.
    if libtest.is_authenticated(domain, token):
        print('Authentication successful!')
    else:
        print('Authentication unsuccessful. :<')
        sys.exit(1)
    return token


class TestUser(unittest.TestCase):
    def test_email(self):
        """Tests that a user's email address can be retrieved."""
        # The 'test' is that this doesn't throw.
        libtest.get_email(domain, token)

    def test_login(self):
        """Tests that a user's login can be retrieved
           and is non-empty."""
        assert libtest.get_login(domain, token).strip() != ''

    def test_name(self):
        """Tests that a user's name can be retrieved
           and is non-empty."""
        assert libtest.get_name(domain, token).strip() != ''

    def test_repo_names(self):
        """Tests that a list of repository names can be retrieved."""
        # The 'test' is that this doesn't throw.
        libtest.get_repo_names(domain, token)


class TestWorkspace(unittest.TestCase):
    def test_round_trip(self):
        """Tests that a workspace can be round-tripped."""
        # Get the old workspace so we can restore it afterward.
        old_workspace = libtest.get_workspace(domain, token)

        # Create a dummy workspace string, test that round-tripping works.
        new_workspace = 'Test workspace!'
        libtest.set_workspace(domain, token, new_workspace)
        assert libtest.get_workspace(domain, token) == new_workspace

        # Restore the old workspace.
        libtest.set_workspace(domain, token, old_workspace)
        assert libtest.get_workspace(domain, token) == old_workspace


if __name__ == '__main__':
    global token
    token = user_sign_in()
    unittest.main()
