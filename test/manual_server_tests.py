#!/usr/bin/env python3
"""A collection of semi-automated unit tests for the collaboration server"""

import unittest
import requests
import sys
import random
import libclient

domain = 'http://193.190.127.184:8042'
test_repo_name = 'dubious-developments/editor-test'


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


class TestUser(unittest.TestCase):
    def test_email(self):
        """Tests that a user's email address can be retrieved."""
        # The 'test' is that this doesn't throw.
        libclient.get_email(domain, token)

    def test_login(self):
        """Tests that a user's login can be retrieved
           and is non-empty."""
        assert libclient.get_login(domain, token).strip() != ''

    def test_name(self):
        """Tests that a user's name can be retrieved
           and is non-empty."""
        assert libclient.get_name(domain, token).strip() != ''

    def test_repo_names(self):
        """Tests that a list of repository names can be retrieved."""
        assert test_repo_name in libclient.get_repo_names(domain, token)


class TestWorkspace(unittest.TestCase):
    def test_round_trip(self):
        """Tests that a workspace can be round-tripped."""
        # Get the old workspace so we can restore it afterward.
        old_workspace = libclient.get_workspace(domain, token)

        # Create a dummy workspace string, test that round-tripping works.
        new_workspace = 'Test workspace!'
        libclient.set_workspace(domain, token, new_workspace)
        assert libclient.get_workspace(domain, token) == new_workspace

        # Restore the old workspace.
        libclient.set_workspace(domain, token, old_workspace)
        assert libclient.get_workspace(domain, token) == old_workspace


class TestRepo(unittest.TestCase):
    def test_round_trip(self):
        """Tests that a file can be round-tripped."""
        file_name = 'test-file.txt'

        # Acquire a lock.
        assert libclient.request_lock(domain, token, test_repo_name, file_name)

        # Include a random number in the message so our changes
        # don't accidentally become no-ops.
        random_number = random.randint(0, 10000)
        message = 'The number I\'m thinking of is %d.\n' % random_number
        libclient.set_file_contents(domain, token, test_repo_name, file_name,
                                    message)
        assert libclient.get_file_contents(domain, token, test_repo_name,
                                           file_name) == message

        libclient.relinquish_lock(domain, token, test_repo_name, file_name)


if __name__ == '__main__':
    global token
    token = user_sign_in()
    unittest.main()
