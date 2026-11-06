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
cases = [(dict(AllowedHosts=host), 'AllowedHosts') for host in
         ['*', '127.0.0.1;*', ' ', 'https://demo.example', 'demo.example:8080', 'demo.example;']]
cases += [
    ({'ASPNETCORE_FORWARDEDHEADERS_ENABLED': 'true'}, 'Unrestricted forwarded headers'),
    ({'DOTNET_FORWARDEDHEADERS_ENABLED': 'true'}, 'Unrestricted forwarded headers'),
    ({'Hosting__KnownProxies__0': '*'}, 'KnownProxies'),
    ({'Hosting__KnownProxies__0': '0.0.0.0'}, 'KnownProxies'),
    ({'Hosting__KnownProxies__0': '127.0.0.1', 'Hosting__HttpsAtEdge': 'true'}, 'not both'),
    ({'Demo__Enabled': 'true', 'Demo__Email': ' ', 'Demo__Password': ' '}, 'Demo mode requires')
]
for settings, expected in cases:
    with tempfile.TemporaryDirectory(prefix='reflect-config-') as directory:
        env = dict(os.environ, ASPNETCORE_ENVIRONMENT='Production', Demo__Enabled='false', AllowedHosts='127.0.0.1', Hosting__HttpsAtEdge='false',
                   AccountStore__Path=str(Path(directory) / 'accounts/db.sqlite'))
        env.update(settings)
        with open(Path(directory) / 'log', 'w+') as log:
            process = subprocess.Popen([dotnet, str(root / 'Reflect.ProviderDashboard.Web/bin/Debug/net10.0/Reflect.ProviderDashboard.Web.dll'), '--urls', 'http://127.0.0.1:0'],
                                       cwd=root / 'Reflect.ProviderDashboard.Web', env=env, stdout=log, stderr=log)
            try:
                try:
                    code = process.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    code = None
                log.seek(0)
                rejected = code not in (None, 0) and expected in log.read()
                print(('PASS: rejected ' if rejected else 'FAIL: accepted ') + repr(settings))
                if not rejected:
                    failed.append(settings)
            finally:
                if process.poll() is None:
                    process.terminate()
                    process.wait(timeout=10)
if failed:
    sys.exit(1)
