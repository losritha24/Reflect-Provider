"""Exercise persisted credential mismatch, literal SQLite paths, and readiness failures."""
import http.client
import os
from pathlib import Path
import secrets
import shutil
import socket
import sqlite3
import subprocess
import tempfile
import time

root = Path(__file__).resolve().parents[1]
dotnet = os.environ.get('DOTNET') or shutil.which('dotnet') or str(Path.home() / '.dotnet/dotnet')
with tempfile.TemporaryDirectory(prefix='reflect-state-') as temporary:
    directory = Path(temporary)
    # The semicolon must be a literal filename character, not a connection-string option.
    database = directory / 'accounts;Mode=ReadOnly.db'
    password = secrets.token_urlsafe(24) + 'aA1!'
    env = dict(os.environ, ASPNETCORE_ENVIRONMENT='Production', Demo__Enabled='true',
               Demo__Email='state@example.invalid', Demo__Password=password,
               AllowedHosts='127.0.0.1', Hosting__HttpsAtEdge='true', AccountStore__Path=str(database))
    with socket.socket() as sock:
        sock.bind(('127.0.0.1', 0))
        port = sock.getsockname()[1]
    command = [dotnet, str(root / 'Reflect.ProviderDashboard.Web/bin/Debug/net10.0/Reflect.ProviderDashboard.Web.dll'),
               '--urls', f'http://127.0.0.1:{port}']
    def request(path):
        conn = http.client.HTTPConnection('127.0.0.1', port, timeout=3)
        try:
            conn.request('GET', path)
            response = conn.getresponse()
            return response.status, response.read().decode()
        finally:
            conn.close()
    def stop(process):
        if process.poll() is None:
            process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
    with open(directory / 'server.log', 'w+') as log:
        def start():
            process = subprocess.Popen(command, cwd=root / 'Reflect.ProviderDashboard.Web', env=env, stdout=log, stderr=log)
            try:
                for _ in range(100):
                    if process.poll() is not None:
                        raise AssertionError('App exited before becoming ready')
                    try:
                        if request('/health/ready')[0] == 200:
                            return process
                    except OSError:
                        pass
                    time.sleep(.1)
                raise AssertionError('Readiness timed out')
            except BaseException:
                stop(process)
                raise
        process = start()
        try:
            assert database.exists(), 'SQLite path was interpreted as connection options'
        finally:
            stop(process)
        print('PASS: SQLite path containing semicolons is treated as a literal path')
        wrong = dict(env, Demo__Password=secrets.token_urlsafe(24) + 'aA1!')
        log.seek(0); log.truncate()
        process = subprocess.Popen(command, cwd=root / 'Reflect.ProviderDashboard.Web', env=wrong, stdout=log, stderr=log)
        try:
            assert process.wait(timeout=15) != 0
            log.seek(0)
            assert 'Configured demo password differs' in log.read()
        finally:
            stop(process)
        print('PASS: stale persisted demo password fails startup instead of silently remaining active')
        process = start()
        try:
            with sqlite3.connect(database) as connection:
                connection.execute('DROP TABLE AspNetUsers')
            status, body = request('/health/ready')
            assert status == 503 and body == 'Unhealthy', (status, body)
            assert request('/health/live')[0] == 200
            assert str(database) not in body and 'AspNetUsers' not in body
        finally:
            stop(process)
        print('PASS: readiness detects missing identity schema without exposing details; liveness remains independent')
