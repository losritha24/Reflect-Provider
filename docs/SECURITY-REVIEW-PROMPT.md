# Prompt for an independent security review (paste into GPT Astra or another reviewer)

You are an experienced application-security engineer reviewing **Reflect Provider Dashboard**, an ASP.NET Core MVC
(.NET 10) web app that lets mental-health clinicians review how their clients use the Reflect Mobile and Reflect XR
apps between sessions. It uses synthetic data only today, but is designed around HIPAA-style safeguards, so review it as
if real patient data would be stored. Repository: https://github.com/losritha24/Reflect-Provider (branch `main`).

## Ground rules
- Test only a local copy you run yourself. Do not attack any hosted instance. Use synthetic data only. Never exfiltrate
  or print real secrets; if you find one in the repo or history, report its location without repeating the value.
- Do not trust documentation or comments. Read the code and verify every claim in `docs/SECURITY-KNOWN-ISSUES.md`
  and on the in-app Privacy & trust page (`Areas/Provider/Views/Home/Trust.cshtml`), including the "Fixed" items.
- Prefer proof: a failing request, a unit test, or a precise code path. Mark anything you could not reproduce as "suspected".

## How to run it
`python3 tests/verify.py` runs the unit, config and hosted smoke tests (needs the .NET 10 SDK). `--browser` adds
Playwright and axe checks. Development run: set `ASPNETCORE_ENVIRONMENT=Development`, sign in as
`doctor@reflect.demo` / `ReflectDemo2026!` (public demo credential; this account is also facility admin).

## Architecture in brief
Provider area MVC controllers; ASP.NET Identity (SQLite, cookie auth with an in-memory ticket store);
clinical data in a separate EF Core context (SQLite or Postgres); provider scoping via `ProviderScope` and the
`Ef*Repository` classes (fail closed); roles `Mental_Health_Provider` and `Facility_Admin`; invite-only onboarding;
optional TOTP MFA; durable hash-chained `AuditLog`; follow-up tasks; messages outbox (prototype); session ingest API
(`/api/v1/ingest/sessions`, shared key header); PDF caseload import and PDF reports; chat assistant (prepared answers,
optional OpenRouter model, off by default); security headers and CSP with nonces in `CspMiddleware`.

## What to examine (highest value first)
1. **Authorization and tenant isolation.** Can one provider read or change another provider's patients, sessions, follow-ups,
   messages, reports or devices by changing an ID, route, query string or form field? Check every controller action, the
   Team admin actions, and the ingest service. Check that `Facility_Admin` cannot read charts.
2. **Authentication and session handling.** Login, lockout, invite acceptance (token handling, expiry, reuse), the MFA step
   (pending cookie, bypass, replay, recovery codes), logout and session revocation, deactivation, cookie flags, fixation.
3. **Ingest API.** Key handling, timing, replay, input validation, ambiguous patient matching, device lookup, rate limiting.
4. **File handling.** The intake PDF import and PDF report generation: size limits, parser denial of service, path or
   filename injection, content reaching logs.
5. **Injection and output encoding.** XSS (including `Html.Raw` uses and SVG generation), CSRF coverage, open redirects,
   header injection, SQL or LINQ injection, template injection, CSP weaknesses.
6. **Secrets and configuration.** Secrets in code, config or git history; unsafe defaults; Development-only features that
   could reach Production; the startup guards in `Program.cs`; destructive reseed paths.
7. **Denial of service and abuse.** The rate-limit design (note the global `account` partition), lockout abuse, large
   requests, expensive queries, background worker behaviour.
8. **Audit and privacy.** Does the audit log record what it claims and never PHI? Can it be bypassed or tampered with
   undetectably? Is any patient data written to logs, TempData, URLs, localStorage or third-party calls (chat model)?
9. **Dependencies and build.** Vulnerable packages, unpinned scripts, container and CI configuration.

## Output format
Return a report with: an executive summary; a table of findings sorted by severity (Critical, High, Medium, Low, Info);
and for each finding: title, severity with one-line reasoning, affected file and line, how to reproduce (exact requests or
commands), impact, and a concrete recommended fix. List separately: (a) known issues you confirmed, (b) known issues you
believe are wrong or already fixed, (c) things you checked and found sound. Finish with the three fixes you would do first.
