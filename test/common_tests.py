#!/usr/bin/env python3
"""A collection of unit tests that are shared between the manual and automated tests."""

import unittest
import libclient
import random

test_repo_name = 'dubious-developments/editor-test'


def create_unit_tests(domain, token):
    """Creates the common unit tests."""

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
        def test_acquire_lock(self):
            """Tests that acquiring a lock works as expected."""
            file_name = 'test-file.txt'
            assert not libclient.has_lock(domain, token, test_repo_name,
                                          file_name)
            assert libclient.request_lock(domain, token, test_repo_name,
                                          file_name)
            assert libclient.has_lock(domain, token, test_repo_name, file_name)
            libclient.relinquish_lock(domain, token, test_repo_name, file_name)
            assert not libclient.has_lock(domain, token, test_repo_name,
                                          file_name)

        def test_round_trip(self):
            """Tests that a file can be round-tripped."""
            file_name = 'test-file.txt'

            # Poll the file. Don't include a timestamp the first time around.
            before_poll = libclient.poll_file(domain, token, test_repo_name,
                                              file_name)

            # May not be true on a live server:
            #     assert not before_poll['isModified']
            #     assert 'contents' not in before_poll

            # Acquire a lock.
            assert libclient.request_lock(domain, token, test_repo_name,
                                          file_name)

            # Include a random number in the message so our changes
            # don't accidentally become no-ops.
            random_number = random.randint(0, 10000)
            message = 'The number I\'m thinking of is %d.\n' % random_number
            libclient.set_file_contents(domain, token, test_repo_name,
                                        file_name, message)
            assert libclient.get_file_contents(domain, token, test_repo_name,
                                               file_name) == message

            # Poll the file and check that it has actually changed.
            first_after_poll = libclient.poll_file(
                domain,
                token,
                test_repo_name,
                file_name,
                last_change_timestamp=before_poll['lastChange'])
            assert first_after_poll['isModified']
            assert first_after_poll['contents'] == message

            libclient.relinquish_lock(domain, token, test_repo_name, file_name)

            # Now poll that same file again and check that it has *not*
            # changed.
            second_after_poll = libclient.poll_file(
                domain,
                token,
                test_repo_name,
                file_name,
                last_change_timestamp=first_after_poll['lastChange'])
            assert not second_after_poll['isModified']
            assert 'contents' not in second_after_poll

            # Also check that the file exists on the server.
            assert file_name in libclient.get_file_names(domain, token,
                                                         test_repo_name)

        def test_create_file(self):
            """Tests that a file can be created."""
            file_name = 'test-file%d.txt' % (random.randint(1, 100000))

            # Acquire a lock.
            assert libclient.request_lock(domain, token, test_repo_name,
                                          file_name)

            # Create a new file by setting its contents.
            message = 'This is just a test file. Move along now.\n'
            libclient.set_file_contents(domain, token, test_repo_name,
                                        file_name, message)

            # Check that the newly created file's contents are okay.
            assert libclient.get_file_contents(domain, token, test_repo_name,
                                               file_name) == message

            # Release the lock.
            libclient.relinquish_lock(domain, token, test_repo_name, file_name)

    return {
        'TestUser': TestUser,
        'TestRepo': TestRepo,
        'TestWorkspace': TestWorkspace
    }


def import_unit_tests(domain, token, environment):
    """Imports common unit tests into a particular environment."""
    for key, value in create_unit_tests(domain, token).items():
        environment[key] = value
