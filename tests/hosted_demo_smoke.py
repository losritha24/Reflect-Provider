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
               AccountStore__Path=str(Path(directory) / 'accounts/provider.db'),
               Clinical__Path=str(Path(directory) / 'clinical/clinical.db'))
    with open(Path(directory) / 'server.log', 'w+') as log:
        container_image = os.environ.get('REFLECT_CONTAINER')
        container_name = 'reflect-smoke-' + secrets.token_hex(6)
        if container_image:
            env['AccountStore__Path'] = '/app/App_Data/provider.db'
            env['Clinical__Path'] = '/app/App_Data/clinical.db'
            command = ['docker', 'run', '--rm', '--name', container_name,
                       '--memory=512m', '--cpus=1', '-p', f'127.0.0.1:{port}:8080']
            for key in ['ASPNETCORE_ENVIRONMENT', 'Demo__Enabled', 'Demo__Email',
                        'Demo__Password', 'AllowedHosts', 'Hosting__HttpsAtEdge', 'AccountStore__Path', 'Clinical__Path']:
                command += ['-e', key]
            command += [container_image]
        else:
            command = [dotnet, str(root / 'Reflect.ProviderDashboard.Web/bin/Debug/net10.0/Reflect.ProviderDashboard.Web.dll'),
                       '--urls', f'http://127.0.0.1:{port}']
        process = subprocess.Popen(command, cwd=root / 'Reflect.ProviderDashboard.Web', env=env, stdout=log, stderr=log)
        def request(path, method='GET', body=None, cookie='', headers=None):
            conn = http.client.HTTPConnection('127.0.0.1', port, timeout=10)
            conn.request(method, path, body, {'Cookie': cookie, 'Content-Type': 'application/x-www-form-urlencoded', **(headers or {})})
            response = conn.getresponse()
            data = response.read().decode()
            result = response.status, response, data
            conn.close()
            return result
        try:
            for _ in range(600):
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
            assert request('/health/ready')[0] == 200
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
            # Team, settings and messages: invite -> accept -> assign -> isolation, end to end.
            from urllib.parse import urlencode as enc
            def jar_request(jar, path, method='GET', fields=None):
                header = '; '.join(f'{k}={v}' for k, v in jar.items())
                status, resp, data = request(path, method, enc(fields) if fields else None, header)
                for key, value in resp.getheaders():
                    if key.lower() == 'set-cookie':
                        name, _, rest = value.partition('=')
                        jar[name] = rest.split(';')[0]
                return status, resp, data
            def csrf_of(page):
                return html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)[1])
            admin = {}
            for part in cookie.split('; '):
                k, _, v = part.partition('=')
                admin[k] = v
            for anonymous_path in ['/Provider/Team/Index', '/Provider/Settings/Index', '/Provider/Messages/Index']:
                assert request(anonymous_path)[0] == 302, anonymous_path
            for page in ['/Provider/Team/Index', '/Provider/Settings/Index', '/Provider/Messages/Index', '/Provider/Modules/Index']:
                assert jar_request(admin, page)[0] == 200, page
            _, _, team_page = jar_request(admin, '/Provider/Team/Index')
            invite_fields = dict(displayName='Pat Provider', email='pat.provider@example.invalid', __RequestVerificationToken=csrf_of(team_page))
            status, _, invited = jar_request(admin, '/Provider/Team/Invite', 'POST', invite_fields)
            link = re.search(r'value="(http[^"]*AcceptInvite[^"]*)"', invited)
            assert status == 200 and link, 'Invite link was not shown'
            invite_path = html.unescape(link[1]).split('127.0.0.1:%d' % port)[-1]
            token = invite_path.split('token=')[1]
            assert token not in request('/Provider/Team/Index', cookie='; '.join(f'{k}={v}' for k, v in admin.items()))[2], 'Raw invite token must be shown only once'
            assert 'pat.provider@example.invalid' in jar_request(admin, '/Provider/Team/Index')[2]
            print('PASS: admin can create a one-time invite; the raw token is not shown again')
            invitee = {}
            status, _, accept_page = jar_request(invitee, invite_path)
            assert status == 200 and 'pat.provider@example.invalid' in accept_page
            assert jar_request(invitee, '/Account/AcceptInvite', 'POST', dict(Token=token, DisplayName='Pat Provider', Password='short', ConfirmPassword='short', __RequestVerificationToken=csrf_of(accept_page)))[0] == 200
            new_password = secrets.token_urlsafe(24) + 'aA1!'
            status, response, _ = jar_request(invitee, '/Account/AcceptInvite', 'POST', dict(
                Token=token, DisplayName='Pat Provider', Password=new_password, ConfirmPassword=new_password,
                __RequestVerificationToken=csrf_of(accept_page)))
            assert status == 302 and 'ReflectProvider.Auth' in invitee, (status, response.getheader('Location'))
            print('PASS: an invitee can set a password and sign in; weak passwords are refused')
            assert jar_request({}, invite_path)[0] == 410, 'Used invite must be gone'
            assert jar_request(invitee, '/Provider/Team/Index')[0] == 302, 'Invited provider must not reach admin pages'
            _, _, empty = jar_request(invitee, '/Provider/Patients/Index')
            assert 'Sam Patel' not in empty, 'New provider must start with an empty caseload'
            print('PASS: used invites are rejected, invitees cannot reach admin pages and start with no patients')
            _, _, team_page = jar_request(admin, '/Provider/Team/Index')
            assert 'Sam Patel' not in team_page, 'Admin assignment list must not expose patient names'
            patient_id = re.search(r'name="patientId" value="([0-9a-f-]{36})"', team_page)[1]
            provider_id = re.search(r'<option value="([^"]+)"[^>]*>Pat Provider</option>', team_page)[1]
            assert jar_request(admin, '/Provider/Team/Assign', 'POST', dict(patientId=patient_id, providerId=provider_id, __RequestVerificationToken=csrf_of(team_page)))[0] == 302
            status, _, assigned = jar_request(invitee, f'/Provider/Patients/Details/{patient_id}')
            assert status == 200, 'Assigned provider must open the patient'
            assert jar_request(admin, f'/Provider/Patients/Details/{patient_id}')[0] == 404, 'Previous provider loses access'
            print('PASS: admin assignment moves a patient between providers; the previous provider loses access')
            _, _, compose = jar_request(invitee, '/Provider/Messages/Index?patientId=' + patient_id)
            send = dict(patientId=patient_id, template='', body='Please call me at 555-123-4567 about the plan.', __RequestVerificationToken=csrf_of(compose))
            assert jar_request(invitee, '/Provider/Messages/Send', 'POST', send)[0] == 302
            _, _, outbox = jar_request(invitee, '/Provider/Messages/Index')
            assert 'not delivered' in outbox and '555-123-4567' not in outbox, 'Identifiers must be removed and status shown'
            assert 'not delivered' not in jar_request(admin, '/Provider/Messages/Index')[2], 'Outbox is per provider'
            print('PASS: messages are queued per provider with identifiers removed and no delivery claim')
            _, _, settings = jar_request(invitee, '/Provider/Settings/Index')
            status, _, _ = jar_request(invitee, '/Provider/Settings/Display', 'POST', dict(theme='dark', textSize='125%', landingPage='Patients', timeZone='America/Chicago', clock='24h', __RequestVerificationToken=csrf_of(settings)))
            assert status == 302
            _, _, settings = jar_request(invitee, '/Provider/Settings/Index')
            assert 'value="America/Chicago"' in settings, 'Time zone was not saved'
            landing = jar_request(invitee, '/Provider/Patients/Index')[2]
            assert "var version = 'dark|125%|'" in landing, 'Theme preference was not applied'
            bad = jar_request(invitee, '/Provider/Settings/Display', 'POST', dict(theme='dark', textSize='', landingPage='Dashboard', timeZone='Not/AZone', clock='12h', __RequestVerificationToken=csrf_of(settings)))
            assert bad[0] == 400
            print('PASS: settings save to the account, apply theme, and reject unknown time zones')
            # Deactivation blocks sign-in.
            _, _, team_page = jar_request(admin, '/Provider/Team/Index')
            assert jar_request(admin, '/Provider/Team/SetActive', 'POST', dict(id=provider_id, active='false', __RequestVerificationToken=csrf_of(team_page)))[0] == 302
            _, login_resp, login_page = jar_request({}, '/Account/Login')
            fresh = {}
            _, _, login_page = jar_request(fresh, '/Account/Login')
            status, _, _ = jar_request(fresh, '/Account/Login', 'POST', dict(Email='pat.provider@example.invalid', Password=new_password, ReturnUrl='', __RequestVerificationToken=csrf_of(login_page)))
            assert status == 200 and 'ReflectProvider.Auth' not in fresh, 'Deactivated account must not sign in'
            print('PASS: a deactivated provider cannot sign in')
            if os.environ.get('REFLECT_BROWSER') == '1':
                from browser_checks import run_browser
                run_browser(f'http://127.0.0.1:{port}', 'reviewer@example.invalid', password)
            # Exhaust credential attempts, including forged forwarding headers. Logout and
            # GET pages must remain available so an attacker cannot prevent revocation.
            rejected = None
            for i in range(35):
                attempt = request('/Account/Login', 'POST', '', headers={'X-Forwarded-For': f'198.51.100.{i + 1}'})
                if attempt[0] == 429:
                    rejected = attempt
                    break
            assert rejected is not None, 'Credential rate limiter did not reject excess requests'
            assert int(rejected[1].getheader('Retry-After')) > 0
            assert request('/Account/Login')[0] == 200
            assert request('/health/ready')[0] == 200
            # Logout clears this browser's cookie, but capture whether a stolen copy remains valid.
            csrf = html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^\"]+)"', dashboard)[1])
            assert request('/Account/Logout', 'POST', urlencode({'__RequestVerificationToken': csrf}), cookie)[0] == 302
            replay = request('/Provider/Home/Dashboard', cookie=cookie)[0]
            assert replay == 302, 'Captured cookie still works after logout'
            print('PASS: captured authentication cookie is rejected after logout')
            assert request('/Account/Login', 'POST', '')[0] == 429
            print('PASS: logout still revokes sessions while credential rate limit is exhausted')
            print('PASS: hosted demo login, registration closure, CSRF, safe redirects, secure cookies, security headers, protected routes, queue rendering, filtering, 404, rate limiting')
        finally:
            if container_image:
                subprocess.run(['docker', 'rm', '-f', container_name], capture_output=True, timeout=30)
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
