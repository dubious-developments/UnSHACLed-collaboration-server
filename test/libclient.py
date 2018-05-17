"""A collection of functions that relate to testing the server."""

import requests
import os
import platform
from subprocess import Popen


def log(s, color_code=36):
    """Logs a message."""
    if os.name == 'nt':
        formatter = '*** %s'
    else:
        formatter = '\x1b[' + str(color_code) + 'm*** %s\x1b[0m'
    print(formatter % s)


def popen_dotnet(path, *args):
    """Launches a .NET executable."""
    log('Launching %s' % path)
    log('Platform: %s' % os.name)
    cmd = [path] + list(args)
    if os.name != 'nt' and 'CYGWIN' not in platform.system():
        cmd.insert(0, 'mono')
    read, _ = os.pipe()
    return Popen(cmd, stdin=read)


def start_server(domain, *args):
    """Launches the server. Does not return until it is ready to handle requests."""
    server_path = os.path.abspath(
        os.path.join('src', 'UnSHACLed.Collaboration', 'bin', 'Debug',
                     'collaboration-server.exe'))

    log('Launching server...')
    server = popen_dotnet(server_path, '-d', domain, *args)

    # Wait for the server to start.
    while True:
        try:
            requests.head(domain, timeout=3.05)
            log('Server launched (PID=%d).' % server.pid)
            break
        except requests.exceptions.RequestException:
            # log('Waiting for server...')
            pass
    return server


def parse_bool(value):
    """Converts a string to a Boolean."""
    return value.strip().lower() == 'true'


def request_token(domain):
    """Requests a token from the server."""
    response = requests.post(domain + '/auth/request-token')
    assert response.ok
    return response.text


def is_authenticated(domain, token):
    """Tests if a user's token is authenticated."""
    response = requests.get(domain + '/auth/is-authenticated/' + token)
    assert response.ok
    return parse_bool(response.text)


def get_workspace(domain, token):
    """Gets an authenticated user's workspace."""
    response = requests.get('%s/workspace/%s' % (domain, token))
    assert response.ok
    return response.text


def set_workspace(domain, token, workspace_contents):
    """Sets an authenticated user's workspace."""
    response = requests.put(
        '%s/workspace/%s' % (domain, token), data=workspace_contents)
    assert response.ok


def get_login(domain, token):
    """Gets a user's login."""
    response = requests.get('%s/user/login/%s' % (domain, token))
    assert response.ok
    return response.text


def get_name(domain, token):
    """Gets a user's name."""
    response = requests.get('%s/user/name/%s' % (domain, token))
    assert response.ok
    return response.text


def get_email(domain, token):
    """Gets a user's email address."""
    response = requests.get('%s/user/email/%s' % (domain, token))
    assert response.ok
    return response.text


def get_repo_names(domain, token):
    """Gets a list of all repositories for a user."""
    response = requests.get('%s/user/repo-list/%s' % (domain, token))
    assert response.ok
    return response.json()


def has_lock(domain, token, repo_slug, file_path):
    """Tests if a lock has been acquired by a user with a
       particular token on a file in a repository."""
    response = requests.get('%s/repo/has-lock/%s/%s/%s' % (domain, repo_slug,
                                                           token, file_path))
    response.raise_for_status()
    return parse_bool(response.text)


def request_lock(domain, token, repo_slug, file_path):
    """Requests a lock on a file in a repository."""
    response = requests.post('%s/repo/request-lock/%s/%s/%s' %
                             (domain, repo_slug, token, file_path))
    response.raise_for_status()
    return parse_bool(response.text)


def relinquish_lock(domain, token, repo_slug, file_path):
    """Releases a lock on a file in a repository."""
    response = requests.post('%s/repo/relinquish-lock/%s/%s/%s' %
                             (domain, repo_slug, token, file_path))
    response.raise_for_status()


def get_file_contents(domain, token, repo_slug, file_path):
    """Gets a file's contents."""
    response = requests.get('%s/repo/file/%s/%s/%s' % (domain, repo_slug,
                                                       token, file_path))
    response.raise_for_status()
    return response.text


def set_file_contents(domain, token, repo_slug, file_path, contents):
    """Sets a file's contents."""
    response = requests.put(
        '%s/repo/file/%s/%s/%s' % (domain, repo_slug, token, file_path),
        data=contents)
    response.raise_for_status()


def poll_file(domain, token, repo_slug, file_path, last_change_timestamp=None):
    """Polls a file for changes."""
    response = requests.get(
        '%s/repo/poll-file/%s/%s/%s' % (domain, repo_slug, token, file_path),
        data=last_change_timestamp)
    response.raise_for_status()
    return response.json()


def get_file_names(domain, token, repo_slug):
    """Gets a list of the file names in a repository."""
    response = requests.get('%s/repo/file-names/%s/%s' % (domain, repo_slug,
                                                          token))
    response.raise_for_status()
    return response.json()
