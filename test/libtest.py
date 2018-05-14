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


def start_server(domain, client_id, client_secret):
    """Launches the server. Does not return until it is ready to handle requests."""
    server_path = os.path.abspath(
        os.path.join('src', 'UnSHACLed.Collaboration', 'bin', 'Debug',
                     'collaboration-server.exe'))

    log('Launching server...')
    server = popen_dotnet(server_path, '-d', domain, '--client-id', client_id,
                          '--client-secret', client_secret)

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
