#!/usr/bin/env python3
"""A collection of unit tests for our electronic checkbook system"""

import unittest
import requests
import os
import platform
from subprocess import Popen

domain = 'http://localhost:8080'

def log(s, color_code = 36):
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

def start_server():
    """Launches the server. Does not return until it is ready to handle requests."""
    server_path = os.path.abspath(
        os.path.join(
            'src',
            'UnSHACLed.Collaboration',
            'bin',
            'Debug',
            'collaboration-server.exe'))

    log('Launching server...')
    server = popen_dotnet(server_path, '-d', domain)

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

class TestAuthentication(unittest.TestCase):
    def test_request_token(self):
        """Tests that a token can be requested."""
        response = requests.post(domain + '/auth/request-token')
        assert response.ok

if __name__ == '__main__':
    server = start_server()
    try:
        unittest.main()
    finally:
        server.kill()
        log('Stopped server.')
