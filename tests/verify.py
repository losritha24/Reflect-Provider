"""One entry point for local/CI regression checks; all account data is temporary."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--browser', action='store_true', help='Run Chromium checks; install requirements-browser.txt and chromium first')
parser.add_argument('--container', metavar='IMAGE', help='Additionally smoke-test an already-built Docker image')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
dotnet = os.environ.get('DOTNET') or shutil.which('dotnet') or str(Path.home() / '.dotnet/dotnet')
env = dict(os.environ, DOTNET=dotnet)
if args.browser:
    env['REFLECT_BROWSER'] = '1'
commands = [
    [dotnet, 'run', '--project', 'tests/Reflect.ProviderDashboard.Checks', '-p:UseSharedCompilation=false'],
    ['node', '--test', 'tests/layoutController.test.cjs'],
    [sys.executable, 'tests/hosted_demo_smoke.py'],
    [sys.executable, 'tests/deployment_config_checks.py'],
    [sys.executable, 'tests/startup_state_checks.py'],
    [dotnet, 'publish', 'Reflect.ProviderDashboard.Web', '-c', 'Release', '--no-restore', '-o', 'test-results/publish'],
]
for command in commands:
    print('RUN: ' + ' '.join(command), flush=True)
    subprocess.run(command, cwd=root, env=env, check=True)
assert not (root / 'test-results/publish/appsettings.Development.json').exists(), 'Development settings leaked into publish output'
if args.container:
    subprocess.run(['docker', 'run', '--rm', '--entrypoint', '/bin/sh', args.container,
                    '-c', 'test ! -f /app/appsettings.Development.json && test "$(id -u)" != 0'], check=True)
    subprocess.run([sys.executable, 'tests/hosted_demo_smoke.py'], cwd=root,
                   env=dict(env, REFLECT_CONTAINER=args.container), check=True)
print('PASS: all selected checks completed')
