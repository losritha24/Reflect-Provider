"""Ensure unsafe Production host allowlists fail before accepting traffic."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import time
import sys
root = Path(__file__).resolve().parents[1]
dotnet = os.environ.get('DOTNET') or shutil.which('dotnet') or str(Path.home() / '.dotnet/dotnet')
failed = []
for hosts in ['*', '127.0.0.1;*', ' ', 'https://demo.example', 'demo.example:8080']:
    with tempfile.TemporaryDirectory(prefix='reflect-config-') as directory:
        env = dict(os.environ, ASPNETCORE_ENVIRONMENT='Production', Demo__Enabled='false', AllowedHosts=hosts,
                   AccountStore__Path=str(Path(directory) / 'accounts/db.sqlite'))
        with open(Path(directory) / 'log', 'w+') as log:
            process = subprocess.Popen([dotnet, str(root / 'Reflect.ProviderDashboard.Web/bin/Debug/net10.0/Reflect.ProviderDashboard.Web.dll'), '--urls', 'http://127.0.0.1:0'],
                                       cwd=root / 'Reflect.ProviderDashboard.Web', env=env, stdout=log, stderr=log)
            try:
                try:
                    code = process.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    code = None
                log.seek(0)
                rejected = code not in (None, 0) and 'AllowedHosts' in log.read()
                print(('PASS: rejected ' if rejected else 'FAIL: accepted ') + repr(hosts))
                if not rejected:
                    failed.append(hosts)
            finally:
                if process.poll() is None:
                    process.terminate()
                    process.wait(timeout=10)
if failed:
    sys.exit(1)
