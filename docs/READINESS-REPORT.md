# Readiness verification — October 30, 2026

Reviewed changes on top of commit `fdc4cb9`. The local demo is substantially better
prepared for competition review. Public hosting has not been created. Real patient
data, production identity and multi-tenant deployment remain outside this demo.

## Verification performed

| Check | Result | Scope |
| --- | --- | --- |
| C# regression runner | 43 checks passed | Provider isolation, patient/paging behavior, session revocation, rate-limit classification, timeline filters, future sessions and Eastern DST display |
| Node checks | 3 tests passed | Accessibility-dialog focus and display preferences |
| Hosted HTTP smoke | Passed directly and inside the final container | Seeded login, registration closure, CSRF, redirects, private paths, forged cookies/headers, filtering, rate-limit exhaustion, logout and cookie replay |
| Configuration probes | 12 unsafe configurations rejected | Host allowlists, unrestricted forwarding, invalid proxies, conflicting TLS modes, missing demo credentials |
| Startup/state probes | 3 passed | Literal semicolon in SQLite filename; stale persisted password rejects startup; missing Identity schema produces generic readiness 503 while liveness remains 200 |
| Chromium | Passed directly and in container | Login, modal/tab keyboard behavior, preserved filters/apply/reset/back, empty timeline, XSS probes, 390px layout and enlarged-text skip-link behavior |
| axe-core 4.14.0 | Zero violations in six tested states | Landing, login, dashboard light/dark, patient timeline, filtered caseload; WCAG 2 A/AA and 2.1 A/AA rules |
| Release publish | Passed | Development settings excluded from output |
| Linux arm64 Docker | Build and smoke passed | Non-root runtime; 512 MB memory/1 CPU test limit; seeded login and browser/axe checks |
| NuGet advisory query | No known vulnerabilities reported | Direct/transitive Web project packages from api.nuget.org, queried inside Linux build container |
| npm audit | Zero reported vulnerabilities | Pinned axe-core test dependency and lockfile |

The final combined command was `python tests/verify.py --browser --container
reflect-provider-demo:review`, using the existing local .NET SDK and a Python
virtual environment with the pinned Playwright dependency. The final local image
ID begins `5bddd5449663` (arm64).

Evidence: [combined verification output](artifacts/readiness-verification.txt),
[NuGet advisory output](artifacts/readiness-nuget-audit.json), and
[accessibility summary](artifacts/readiness-accessibility.json).
Reviewed screenshots: [timeline](artifacts/patient-timeline.png) and
[390px caseload at enlarged text](artifacts/caseload-mobile-large-text.png).

## Findings fixed

1. The account-wide limiter could prevent sign-out after budget exhaustion.
   Credential attempts now have their own policies; logout remains CSRF-protected
   and revokes the session even while sign-in POSTs receive 429.
2. Forwarding convenience switches could undermine explicit trust assumptions.
   They now fail startup; optional known proxies are exact IPs with one forwarded
   hop and no forwarded-host acceptance. The existing edge-TLS mode remains an
   explicit infrastructure assumption that requires real-host verification.
3. A new configured demo password silently left the stored old password valid.
   Startup now detects this mismatch and requires an explicit disposable-store reset.
4. A configured SQLite path was interpolated into a connection string. The builder
   now keeps semicolons and other special characters inside the literal path.
5. Liveness alone did not detect a missing Identity schema. A separate readiness
   check reports dependency failure without SQL/path/account details in its body.
6. Detail navigation lost search/filter/page context, and timelines had no filters.
   Typed route/hidden form fields preserve state; validated platform and time-window
   filters retain summary totals and display the matching count separately.
7. Future-dated sessions changed list/detail totals but were absent from dashboard
   signals. List/detail now exclude future records; dashboard uses one activity
   snapshot for its displayed counts.
8. Automated and visual checks found low-contrast slate text/badges and an enlarged
   skip link peeking into the viewport. Contrast-safe semantic text colors and a
   size-independent hiding transform fix these while retaining brand colors/fonts.

## Limits and remaining gates

- Native macOS builds logged NU1900 because the advisory feed was unreachable in
  that environment. The subsequent Linux container advisory query succeeded; do
  not describe the macOS restore alone as a successful vulnerability scan.
- The amd64 SDK crashed with AccessViolationException under local emulation before
  compiling this application. Native arm64 passed. The prepared GitHub Actions
  job must validate native amd64 before a Render release; it has not run remotely.
- axe requires human review for two dashboard checks in each theme. Zero automated
  violations is not a complete accessibility certification. Screen-reader and
  actual browser-zoom review remain open; the current test increases text size.
- Container OS vulnerability scanning and repository-history secret scanning were
  not completed in this pass. Prior Semgrep reports are historical; no fresh
  Semgrep result is claimed here.
- The shared demo account can still be locked after five failed passwords. Edge-TLS
  deployments may share one app-side client bucket across reviewers. Real edge
  protection and per-reviewer/session demo isolation remain release/product work.
- Authentication tickets, synthetic data and SQLite remain single-instance local
  state. No durable clinical audit, real facility identity, tenant isolation,
  managed clinical persistence or clinical integration is implied.

## Copilot contribution

GitHub Copilot CLI was initialized through the existing `gh` installation and used
for two bounded tasks: a source review and timeline/filter/navigation implementation.
The CLI reported 2.05 and 2.28 AI credits respectively. The lead reviewed the diff,
fixed GET-form context retention and accessibility details, and ran the integrated
suite. Copilot was not given permission to commit, push or deploy.

Use [NEXT-STEPS.md](NEXT-STEPS.md) for the ordered follow-up plan and
[COMPETITION-DEPLOYMENT.md](COMPETITION-DEPLOYMENT.md) for the Free hosting setup.
