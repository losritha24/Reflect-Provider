"""Optional Playwright checks called by hosted_demo_smoke.py with REFLECT_BROWSER=1."""
import json
from pathlib import Path
from urllib.parse import quote
from playwright.sync_api import sync_playwright


def run_browser(base_url, email, password):
    import os
    output = Path(os.environ.get('REFLECT_ARTIFACTS', str(Path(__file__).resolve().parents[1] / 'docs/artifacts')))
    output.mkdir(parents=True, exist_ok=True)
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        try:
            context = browser.new_context(viewport={'width': 1440, 'height': 1000})
            page = context.new_page()
            axe_path = Path(__file__).resolve().parent / 'node_modules/axe-core/axe.min.js'
            if not axe_path.exists():
                raise RuntimeError('Install accessibility checks first: npm ci --prefix tests --ignore-scripts')
            axe_source = axe_path.read_text()
            accessibility_failures = []
            def audit(name):
                page.evaluate(axe_source)
                results = page.evaluate('''async () => await axe.run(document, {
                    runOnly: {type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']}
                })''')
                (output / (name + '-axe.json')).write_text(json.dumps(results, indent=2))
                blocking = [v for v in results['violations'] if v['impact'] in ('serious', 'critical')]
                accessibility_failures.extend((name, v['id'], [n['target'] for n in v['nodes']]) for v in blocking)
            errors = []
            page.on('pageerror', lambda error: errors.append(str(error)))
            page.goto(base_url + '/')
            page.wait_for_load_state('networkidle')
            assert page.get_by_role('link', name='Explore the demo', exact=True).count() == 1
            audit('landing')
            page.screenshot(path=str(output / 'landing-desktop.png'), full_page=True)
            page.goto(base_url + '/Account/Login')
            page.wait_for_load_state('networkidle')
            assert page.get_by_role('button', name='Sign in', exact=True).count() == 1
            audit('login')
            page.get_by_label('Work email', exact=True).fill(email)
            page.get_by_label('Password', exact=True).fill(password)
            page.get_by_role('button', name='Sign in', exact=True).click()
            page.wait_for_load_state('networkidle')
            assert page.locator('#tourDialog[open]').count() == 1, 'First-time tour opens on first visit'
            page.get_by_role('button', name='Skip', exact=True).click()
            assert page.locator('#tourDialog[open]').count() == 0
            page.reload()
            page.wait_for_load_state('networkidle')
            assert page.locator('#tourDialog[open]').count() == 0, 'Tour stays dismissed after skip'
            assert page.get_by_role('heading', name='Needs attention').count() == 1
            assert page.evaluate('document.fonts.check(\'16px "IBM Plex Sans"\')')
            assert 'IBM Plex Sans Condensed' in page.locator('h1').evaluate('(e) => getComputedStyle(e).fontFamily')
            audit('dashboard-light')
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
            audit('patient-timeline')
            page.screenshot(path=str(output / 'patient-timeline.png'), full_page=True)
            page.goto(base_url + '/Provider/Patients/Index?search=Sam&trend=Declining&page=1')
            page.wait_for_load_state('networkidle')
            assert page.locator('#Search').input_value() == 'Sam'
            page.get_by_role('link', name='Sam Patel', exact=True).click()
            page.get_by_role('tab', name='Timeline', exact=True).click()
            page.get_by_label('Time period', exact=True).select_option('7')
            page.get_by_label('Platform', exact=True).select_option('Mobile')
            page.get_by_role('button', name='Apply filters', exact=True).click()
            page.wait_for_load_state('networkidle')
            assert page.get_by_role('tab', name='Timeline').get_attribute('aria-selected') == 'true'
            assert page.get_by_label('Time period', exact=True).input_value() == '7'
            assert page.locator('.patient-timeline li').count() == 0
            assert 'No sessions match these filters' in page.inner_text('body')
            page.get_by_role('link', name='Clear filters', exact=True).click()
            page.wait_for_load_state('networkidle')
            assert page.locator('.patient-timeline li').count() > 0
            page.get_by_role('link', name='Back to patients', exact=True).click()
            page.wait_for_load_state('networkidle')
            assert page.locator('#Search').input_value() == 'Sam'
            assert page.locator('#Trend').input_value() == '3'
            assert page.locator('th[scope="row"] a').count() == 1
            assert page.locator('th[scope="col"]').count() == 6
            audit('caseload')
            page.set_viewport_size({'width': 390, 'height': 844})
            page.evaluate('document.documentElement.style.fontSize = "200%"')
            assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'), 'Caseload overflows at enlarged text'
            assert page.locator('.skip-link').evaluate('(e) => e.getBoundingClientRect().bottom <= 0'), 'Unfocused skip link obscures enlarged-text content'
            page.screenshot(path=str(output / 'caseload-mobile-large-text.png'), full_page=True)
            page.evaluate('document.documentElement.style.fontSize = "100%"')
            page.set_viewport_size({'width': 1440, 'height': 1000})
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
            assert all(key in ['reflect-provider-theme', 'reflect-provider-accessibility', 'reflect-provider-tour-seen'] for key in awaitable)
            page.locator('.account-menu > summary').click()
            page.locator('#themeToggle').click()
            page.locator('.account-menu > summary').click()
            audit('dashboard-dark')
            page.screenshot(path=str(output / 'dashboard-dark.png'), full_page=True)
            page.locator('.account-menu > summary').click()
            page.locator('#themeToggle').click()
            page.locator('.account-menu > summary').click()
            page.set_viewport_size({'width': 390, 'height': 844})
            page.screenshot(path=str(output / 'dashboard-mobile.png'), full_page=True)
            assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'), 'Mobile viewport overflows horizontally'
            assert not errors, errors
            assert not accessibility_failures, accessibility_failures
            print('PASS: Chromium login, font loading, modal focus trap, keyboard tabs, reflected-XSS/HTML-helper probes, browser storage, mobile overflow, and JS errors')
        finally:
            browser.close()
