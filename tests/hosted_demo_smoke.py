"""Run after dotnet build; uses an isolated temporary account database."""
import html
import http.client
import os
from pathlib import Path
import re
import secrets
import shutil
import socket
import subprocess
import tempfile
import time

root = Path(__file__).resolve().parents[1]
dotnet = os.environ.get('DOTNET') or shutil.which('dotnet') or str(Path.home() / '.dotnet/dotnet')
with socket.socket() as listener:
    listener.bind(('127.0.0.1', 0))
    port = listener.getsockname()[1]
with tempfile.TemporaryDirectory(prefix='reflect-smoke-') as directory:
    password = secrets.token_urlsafe(24) + 'aA1!'
    env = dict(os.environ, ASPNETCORE_ENVIRONMENT='Production', Demo__Enabled='true',
               Demo__Email='reviewer@example.invalid', Demo__Password=password,
               AllowedHosts='127.0.0.1', Hosting__HttpsAtEdge='true',
               AccountStore__Path=str(Path(directory) / 'accounts/provider.db'))
    with open(Path(directory) / 'server.log', 'w+') as log:
        process = subprocess.Popen([dotnet, str(root / 'Reflect.ProviderDashboard.Web/bin/Debug/net10.0/Reflect.ProviderDashboard.Web.dll'),
                                    '--urls', f'http://127.0.0.1:{port}'],
                                   cwd=root / 'Reflect.ProviderDashboard.Web', env=env, stdout=log, stderr=log)
        def request(path, method='GET', body=None, cookie='', headers=None):
            conn = http.client.HTTPConnection('127.0.0.1', port, timeout=10)
            conn.request(method, path, body, {'Cookie': cookie, 'Content-Type': 'application/x-www-form-urlencoded', **(headers or {})})
            response = conn.getresponse()
            data = response.read().decode()
            result = response.status, response, data
            conn.close()
            return result
        try:
            for _ in range(100):
                if process.poll() is not None:
                    log.seek(0)
                    raise RuntimeError(log.read())
                try:
                    if request('/health/live')[0] == 200:
                        break
                except OSError:
                    pass
                time.sleep(.1)
            else:
                raise RuntimeError('Server did not start')
            assert request('/Provider/Home/Dashboard')[0] == 302
            assert request('/Account/Register')[0] == 404
            assert request('/Provider/Patients/Index', headers={'X-Forwarded-User': 'admin'})[0] == 302
            assert request('/health/live', headers={'Host': 'evil.example'})[0] == 400
            for private in ['/appsettings.json', '/App_Data/provider-accounts.db', '/.git/config', '/Program.cs']:
                assert request(private)[0] == 404, private
            assert request('/Account/Login', 'POST', 'Email=x')[0] == 400
            status, response, page = request('/Account/Login')
            assert status == 200 and 'Create an account' not in page
            assert 'no-store' in response.getheader('Cache-Control')
            assert response.getheader('X-Content-Type-Options') == 'nosniff'
            assert "frame-ancestors 'none'" in response.getheader('Content-Security-Policy')
            cookie = '; '.join(value.split(';')[0] for key, value in response.getheaders() if key.lower() == 'set-cookie')
            token = html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)[1])
            from urllib.parse import urlencode
            body = urlencode(dict(Email='reviewer@example.invalid', Password=password,
                                  ReturnUrl='https://example.invalid/evil', __RequestVerificationToken=token))
            status, response, _ = request('/Account/Login', 'POST', body, cookie)
            assert status == 302 and response.getheader('Location') in ('/Provider', '/Provider/Home/Dashboard'), (status, response.getheader('Location'))
            auth = next(v for k,v in response.getheaders() if k.lower() == 'set-cookie' and v.startswith('ReflectProvider.Auth='))
            assert 'secure' in auth.lower() and 'httponly' in auth.lower()
            cookie += '; ' + auth.split(';')[0]
            status, _, dashboard = request('/Provider/Home/Dashboard', cookie=cookie)
            assert status == 200 and 'Needs attention' in dashboard and 'Sam Patel' in dashboard
            assert request('/Provider/Patients/Details/not-a-guid', cookie=cookie)[0] == 404
            status, _, listing = request('/Provider/Patients/Index?search=Sam', cookie=cookie)
            assert status == 200 and 'Sam Patel' in listing and 'Alex Rivera' not in listing
            assert request('/Provider/Home/Dashboard', cookie='ReflectProvider.Auth=forged')[0] == 302
            assert request('/Account/Logout', 'POST', '', cookie)[0] == 400
            assert request('/Account/Register', 'POST', urlencode({'__RequestVerificationToken': token}), cookie.split('; ReflectProvider.Auth=')[0])[0] == 404
            for query in ['trend=999', 'page=not-a-number', 'trend=not-a-trend']:
                assert request('/Provider/Patients/Index?' + query, cookie=cookie)[0] == 400
            payload = '\"><script>window.__xss=1</script>'
            _, _, encoded = request('/Provider/Patients/Index?' + urlencode({'search': payload}), cookie=cookie)
            assert '<script>window.__xss=1</script>' not in encoded
            assert request('/Account/Login', 'POST', urlencode({'__RequestVerificationToken': token, 'x': 'a' * 20000}), cookie.split('; ReflectProvider.Auth=')[0])[0] in (400, 413)
            if os.environ.get('REFLECT_BROWSER') == '1':
                from browser_checks import run_browser
                run_browser(f'http://127.0.0.1:{port}', 'reviewer@example.invalid', password)
            # Logout clears this browser's cookie, but capture whether a stolen copy remains valid.
            csrf = html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^\"]+)"', dashboard)[1])
            assert request('/Account/Logout', 'POST', urlencode({'__RequestVerificationToken': csrf}), cookie)[0] == 302
            replay = request('/Provider/Home/Dashboard', cookie=cookie)[0]
            assert replay == 302, 'Captured cookie still works after logout'
            print('PASS: captured authentication cookie is rejected after logout')
            assert any(request('/Account/Login')[0] == 429 for _ in range(31))
            print('PASS: hosted demo login, registration closure, CSRF, safe redirects, secure cookies, security headers, protected routes, queue rendering, filtering, 404, rate limiting')
        finally:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
