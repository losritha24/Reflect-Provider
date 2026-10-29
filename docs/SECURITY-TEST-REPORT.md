# Local application vulnerability assessment

Date: October 29, 2026. Target: this working tree, including existing uncommitted
changes. The other agent's original report is retained in `SECURITY-REVIEW.md`.
This report supersedes its old statement that runtime tests could not be run.

## Scope and method

Authorized target: the local ASP.NET Core application with synthetic records and
an isolated temporary SQLite account database. Servers bound only to loopback.
No public host, real patient system, or third-party service was penetration tested.

Applied the installed Trail of Bits **sharp-edges** and **variant-analysis** skills
and Anthropic **webapp-testing**. These supplied methods, not magic vulnerability
coverage. The small codebase was reviewed directly without delegated agents.

Assets/boundaries considered: password hashes, authentication tickets, provider
assignment, patient/session reads, rendered user input, account POSTs, host/proxy
configuration, local files and browser storage. Attackers considered: an anonymous
client, an authenticated unassigned provider, a stolen-cookie holder, and a deployer
copying an unsafe configuration. We followed controller → view model → repository
calls and checked framework protections before calling a candidate exploitable.

## Findings

| ID | Severity / confidence | Status | Evidence and effect |
| --- | --- | --- | --- |
| SEC-01 | Medium / high | **Fixed** | Production startup rejected only the exact `*` string. `127.0.0.1;*` and whitespace were accepted, defeating the intended explicit-host requirement; URL/port forms were also silently accepted as configuration. Startup probes reproduced acceptance before the patch. Each semicolon-delimited host is now trimmed and validated; empty, wildcard, URL/path/port entries are rejected. |
| SEC-02 | Low defense-in-depth / high | **Fixed** | A repository called with an unauthenticated identity carrying the assigned ID returned patients. A new C# test failed before the patch. The shared patient guard now requires authentication, provider role and a nonempty ID. Session repositories inherit that scope. Existing HTTP authorization already blocked anonymous requests; this was **not** a demonstrated internet login bypass. |
| SEC-03 | Medium / high | **Fixed in follow-up** | After valid CSRF-protected logout, replaying the previously captured auth cookie returned HTTP 200 for the dashboard. `SignOutAsync()` clears the browser cookie but the app has no per-session server-side revocation. Requires a previously stolen cookie; it does not steal one. Fixed one-hour expiration limits the window. Add a per-session server-side ticket/revocation store and verify replay becomes 302/401. Avoid a global logout workaround that unexpectedly signs every judge out of the shared account. |
| SEC-04 | Medium availability / high | **Open, known demo limitation** | Account endpoints share one 30-request/minute limiter. The test exhausted it and observed 429; an anonymous client can consume the budget also used by legitimate login/logout requests. Use separately partitioned mutation limits with a global cap and correctly trusted client-IP handling; do not trust arbitrary forwarded IPs. |
| SEC-05 | High if misdeployed / high source confidence | **Open deployment constraint** | `Hosting:HttpsAtEdge` unconditionally treats backend requests as HTTPS. It is only safe when an edge enforces TLS and prevents direct public access to the HTTP origin. No remote deployment was available to verify that boundary. Prefer explicit trusted-proxy configuration for production; never enable this flag on an exposed HTTP server. |

Locations: `Program.cs` host validation, rate-limiter and HTTPS-at-edge setup;
`Services/InMemoryPatientRepository.cs:55` shared access guard;
`Controllers/AccountController.cs:151` logout. Line numbers may move during other
agents' edits. No new remotely exploitable critical issue was demonstrated in the
local configured demo. That is a scoped result, not a guarantee of security.

Production blockers still apply: no tenant model, verified clinician identity/MFA,
immutable audit trail, durable protected key storage, migration/restore workflow or
clinical-data retention policies. Provider isolation tests are not cross-tenant
tests; cross-tenant functionality does not yet exist.

## Executed checks and results

| Check | Result | Important limits |
| --- | --- | --- |
| C# regression runner | 27 assertions passed after follow-up fixes | Covers search/page boundaries, deleted/missing patients, cancellation, assignment isolation, session access/counts, anonymous and non-provider identities, and queue calculations. |
| Deployment startup probes | All 5 invalid host configurations rejected after fix | Before fix only bare `*` was rejected; tests accept no listening server for unsafe values. |
| Hosted HTTP probes | Passed rejection/success expectations | Anonymous protected routes, spoofed user header, invalid Host, private file paths, hosted registration GET/POST closure, login CSRF, safe external return URL, auth cookie flags, forged cookie, logout CSRF, invalid filters, reflected script encoding, oversized account POST rejection, 404 and limiter 429. |
| Logout replay probe | **Now returns 302; SEC-03 fixed** | The original 200 response was reproduced before remediation; denial is now a required regression assertion. |
| Real Chromium checks (Playwright 1.60.0) | Passed | Login, actual font loading, modal focus trap/Escape restoration, patient tab arrow navigation, reflected XSS payload, legacy badge helper encoding, browser storage keys, 390px viewport overflow and JavaScript runtime errors. Screenshots saved. |
| Existing Node accessibility tests | 3 passed | Simulated DOM tests; complemented by Chromium, not a screen-reader audit. |
| NuGet direct/transitive vulnerability audit | No known vulnerable packages returned | Advisory coverage at run time; does not audit custom code or container/OS packages. |
| Semgrep 1.182.1 general security-audit pack | 23 applicable rules, 63 targets, 0 findings/errors | Pack supplied no C# rules here; mostly JS/generic coverage. Do not cite this alone as a C# security audit. |
| Explicit local Semgrep C# rules | 5 rules, 33 C# targets, 0 findings/errors | Narrow checks for unsafe deserialization, concatenated raw SQL, CSRF bypass attributes, permissive certificate callbacks and process execution. No whole-program taint/CodeQL claim. |

Artifacts: [general scanner JSON](artifacts/semgrep-general.json),
[C# scanner JSON](artifacts/semgrep-csharp.json), and browser screenshots linked
in [the UI plan](UI-IMPLEMENTATION-PLAN.md). Scanner reports precede the final
small role-check addition, which is covered by the subsequent C# regression run.

## Variant-analysis record

Original issue: patient/session reads lacked an authenticated provider assignment
boundary. The historical vulnerable form was `InMemoryDataStore.Patients` followed
only by deletion/name/ID predicates. We calibrated on the current surviving direct
store access and expanded toward sibling session access and all consumers.

| Step | Pattern / scope | Result and triage |
| --- | --- | --- |
| 1 | `InMemoryDataStore.Patients` in application C# | One current read location: guarded `AccessiblePatients`. ID-only guard produced SEC-02; now fixed. |
| 2 | `InMemoryDataStore.(Patients|Sessions)` | Three read sites total: patient guard, session listing filtered by allowed IDs, session detail preceded by patient authorization. The latter two are protected variants, not new bypasses. |
| 3 | `GetByIdAsync|GetByPatientIdAsync|GetAllAsync|CountSinceAsync|CountByPlatformSinceAsync` | Followed dashboard and patient view-model callers, then verified unauthorized detail/list/aggregate paths in tests. No additional unscoped session read found. |
| 4 | `Html.Raw|innerHTML|eval|Process.Start|FromSql|ExecuteSql|BinaryFormatter|Redirect|AllowAnonymous|localStorage` | Reviewed matches: return URLs use IsLocalUrl; badge helper uses textContent plus color allowlist; Razor encodes search input; browser storage holds display settings only; anonymous endpoints are intentional login/register gates and liveness. No matching raw SQL, eval or unsafe serializer use was found in application sources. |

A generic scanner pack was a failed route to C# coverage; explicit rules corrected
that gap. We did not interpret a zero-finding scanner result as a clean bill of
health. The concrete negative tests in CI are the regression guards for the actual
assignment/configuration bugs; the optional static rules live at
`tests/security/csharp.yml`.

## Reproduce

From repository root, using .NET 10 and Python 3:

```bash
dotnet run --project tests/Reflect.ProviderDashboard.Checks -p:UseSharedCompilation=false
node --test tests/layoutController.test.cjs
python3 tests/deployment_config_checks.py
python3 tests/hosted_demo_smoke.py
```

For actual Chromium tests, install Playwright into a virtual environment and its
browser, then run the same isolated HTTP harness with the optional browser checks:

```bash
python3 -m venv /tmp/reflect-security-venv
/tmp/reflect-security-venv/bin/pip install playwright==1.60.0
/tmp/reflect-security-venv/bin/python -m playwright install chromium
REFLECT_BROWSER=1 /tmp/reflect-security-venv/bin/python tests/hosted_demo_smoke.py
```

Optional local static scan (no hosted scan account; metrics disabled):

```bash
uv tool run --python 3.12 semgrep==1.182.1 scan --config tests/security/csharp.yml --metrics off --disable-version-check --no-git-ignore --exclude '**/obj/**' --exclude '**/bin/**' Reflect.ProviderDashboard.Web Reflect.ProviderDashboard.Domain
```

## Follow-up remediation

Logout now removes the individual server-side authentication ticket. The browser
holds a protected reference; replay after logout must redirect to login. A late
renewal cannot recreate a removed ticket, and renewal preserves absolute expiry.
Other sessions remain signed in. This in-memory store is deliberately limited to
one demo instance; restarts sign users out, and scaled hosting needs a shared store.

The UI foundation now preserves the required fonts and palette while improving
link/button contrast, uppercase primary headings, and compact account controls.

## Not covered / next actions

1. Replace the in-memory ticket store with a shared, protected store before multi-instance hosting.
2. Verify real TLS/proxy/origin isolation and Docker runtime on the chosen host.
3. Add tenant-aware authorization and database/audit persistence before any real
   clinical data; test two tenants with separate identities and assignments.
4. Add external identity/MFA and partitioned abuse controls before broad public use.
5. Run container-image/OS advisory scanning, full secret-history scanning, automated
   accessibility testing and a human screen-reader pass. None is claimed here.

No brute-force credential campaign, destructive fuzzing, load/DoS test, external
service scan, or exploit against real records was performed. The rate-limit test
sent only the small bounded burst needed to establish its behavior on localhost.
