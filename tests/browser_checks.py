"""Optional Playwright checks called by hosted_demo_smoke.py with REFLECT_BROWSER=1."""
from pathlib import Path
from urllib.parse import quote
from playwright.sync_api import sync_playwright


def run_browser(base_url, email, password):
    output = Path(__file__).resolve().parents[1] / 'docs/artifacts'
    output.mkdir(parents=True, exist_ok=True)
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        try:
            context = browser.new_context(viewport={'width': 1440, 'height': 1000})
            page = context.new_page()
            errors = []
            page.on('pageerror', lambda error: errors.append(str(error)))
            page.goto(base_url + '/')
            page.wait_for_load_state('networkidle')
            assert page.get_by_role('link', name='Explore the demo', exact=True).count() == 1
            page.screenshot(path=str(output / 'landing-desktop.png'), full_page=True)
            page.goto(base_url + '/Account/Login')
            page.wait_for_load_state('networkidle')
            assert page.get_by_role('button', name='Sign in', exact=True).count() == 1
            page.get_by_label('Work email', exact=True).fill(email)
            page.get_by_label('Password', exact=True).fill(password)
            page.get_by_role('button', name='Sign in', exact=True).click()
            page.wait_for_load_state('networkidle')
            assert page.get_by_role('heading', name='Needs attention').count() == 1
            assert page.evaluate('document.fonts.check(\'16px "IBM Plex Sans"\')')
            assert 'IBM Plex Sans Condensed' in page.locator('h1').evaluate('(e) => getComputedStyle(e).fontFamily')
            page.screenshot(path=str(output / 'dashboard-desktop.png'), full_page=True)
            page.locator('.account-menu > summary').click()
            page.locator('#a11yPanelToggle').click()
            assert page.locator('#a11yPanelClose').evaluate('(e) => e === document.activeElement')
            page.keyboard.press('Shift+Tab')
            assert page.locator('#colorBlindToggle').evaluate('(e) => e === document.activeElement')
            page.keyboard.press('Tab')
            assert page.locator('#a11yPanelClose').evaluate('(e) => e === document.activeElement')
            page.keyboard.press('Escape')
            assert page.locator('#a11yPanelToggle').evaluate('(e) => e === document.activeElement')
            page.get_by_role('link', name='Sam Patel', exact=True).first.click()
            page.wait_for_load_state('networkidle')
            page.locator('#tab-overview').focus()
            page.keyboard.press('ArrowRight')
            assert page.locator('#tab-sessions').get_attribute('aria-selected') == 'true'
            assert page.locator('#panel-sessions').is_visible()
            assert page.locator('.patient-timeline li').count() > 0
            page.screenshot(path=str(output / 'patient-timeline.png'), full_page=True)
            payload = '\"><img src=x onerror="window.__xss=1">'
            page.goto(base_url + '/Provider/Patients/Index?search=' + quote(payload))
            page.wait_for_load_state('networkidle')
            assert page.locator('#Search').input_value() == payload
            assert page.evaluate('window.__xss === undefined')
            assert page.locator('img[src="x"]').count() == 0
            # Exercise the legacy HTML helper with untrusted text and class values.
            safe = page.evaluate('''() => {
                const target = document.createElement('div');
                target.innerHTML = renderBadge('x" onclick="alert(1)', '<img src=x onerror="window.__xss=1">');
                return target.querySelectorAll('img, [onclick]').length === 0;
            }''')
            assert safe
            page.goto(base_url + '/Provider/Home/Dashboard')
            page.wait_for_load_state('networkidle')
            awaitable = page.evaluate('Object.keys(localStorage)')
            assert all(key in ['reflect-provider-theme', 'reflect-provider-accessibility'] for key in awaitable)
            page.locator('.account-menu > summary').click()
            page.locator('#themeToggle').click()
            page.locator('.account-menu > summary').click()
            page.screenshot(path=str(output / 'dashboard-dark.png'), full_page=True)
            page.locator('.account-menu > summary').click()
            page.locator('#themeToggle').click()
            page.locator('.account-menu > summary').click()
            page.set_viewport_size({'width': 390, 'height': 844})
            page.screenshot(path=str(output / 'dashboard-mobile.png'), full_page=True)
            assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'), 'Mobile viewport overflows horizontally'
            assert not errors, errors
            print('PASS: Chromium login, font loading, modal focus trap, keyboard tabs, reflected-XSS/HTML-helper probes, browser storage, mobile overflow, and JS errors')
        finally:
            browser.close()
