# Changes since the last checkout

Updated: October 29, 2026.

**Baseline:** `6cceeae` — `Align provider dashboard with extension spec`.
The local reflog records the initial clone at this commit and no later pull.
This summary therefore covers the current working tree against that checkout,
including new, untracked files. These changes have not been committed or deployed
by this workflow.

## Interface and provider experience

- Added a public judge-facing landing page with an illustrative synthetic patient
  preview and an Explore the demo link. Signed-in visitors go to the dashboard.
- Redesigned the dashboard around an explainable Needs Attention worklist,
  compact caseload metrics, platform activity meters with a data-table alternative,
  and a responsive recent-activity feed.
- Signals compare session counts in consecutive seven-day windows and show last
  activity, comparison boundaries, and synthetic-data freshness.
- Added a chronological patient session/reflection timeline, expandable source
  details, and an alternative session table.
- Added patient search by name or external ID, engagement filtering, bounded
  pagination, reset controls, and empty-result messaging.
- Simplified login copy, polished filters, and consolidated account/display
  controls into a compact menu.
- Preserved the required IBM Plex Sans and IBM Plex Sans Condensed fonts and the
  Reflect coral/indigo palette; improved contrast and uppercase primary typography.
- Added a dedicated `provider-workspace.css` stylesheet and browser screenshots
  under `docs/artifacts/`.

## Accessibility and functional fixes

- Native links support dashboard/patient navigation, including added Space-key support.
- Patient tabs now expose selected state and support arrow, Home, and End keys.
- Added skip navigation, visible focus styles, and reduced-motion support.
- Accessibility settings persist display preferences; the settings dialog traps
  focus, closes with Escape, and restores focus to its trigger.
- Malformed, missing, deleted, and inaccessible patient IDs return 404.
- Improved mobile layout and removed dependence on wide activity tables on the dashboard.

## Backend, identity, and security

- Added provider assignment to synthetic patients. Patient queries enforce an
  authenticated provider role and matching provider ID.
- Scoped session history and aggregate counts to accessible patients.
- Added repository-level patient filtering/pagination through `PatientPage`.
- Dashboard recent activity excludes future sessions and sessions without an
  accessible patient; session aggregates exclude future activity.
- Added opt-in hosted synthetic demo login and startup account seeding. Hosted
  login accepts the configured demo account; registration remains Development-only.
- Configured the requested local demo account in `appsettings.Development.json`:
  `doctor@reflect.demo` / `ReflectDemo2026!`. These are intentionally public,
  synthetic-demo credentials. Newly registered accounts have no assigned patients.
- Added secure hosted cookies, a fixed one-hour authentication lifetime, default
  MVC antiforgery validation, account request-size limits, and a shared rate limit.
- Added server-side authentication tickets: logout revokes the individual session,
  captured cookies cannot be replayed afterward, and late renewal cannot restore
  a revoked session. Other login sessions remain independent.
- Added no-store responses, browser security headers, a narrower CSP, and escaped
  text/allowlisted colors in the legacy badge helper.
- Production startup now rejects empty, wildcard, URL, and port-bearing host
  allowlist entries rather than silently accepting unsafe configuration.
- Added `/health/live` for process liveness. It is not a dependency-readiness check.
- Account-directory creation no longer changes permissions on an existing parent
  directory selected through configuration.

## Verification and engineering

- Added a dependency-free C# regression runner, Node accessibility behavior tests,
  hosted HTTP smoke tests, unsafe-configuration probes, and optional Playwright
  browser checks.
- Latest application pass: build succeeded without warnings; 27 C# checks and the
  HTTP/Chromium checks passed. Earlier Node and configuration checks also passed.
- Browser checks cover login, font loading, keyboard interaction, input encoding,
  mobile overflow, timeline/landing rendering, and logout-cookie replay rejection.
- Ran direct/transitive NuGet vulnerability checks and Semgrep scans. No findings
  were returned by the selected rules/advisory source; scope and limitations are
  documented in [the security test report](SECURITY-TEST-REPORT.md).
- Added GitHub Actions for build/regression/configuration checks and Release publish.
- Added Python cache exclusions to `.gitignore`.

## Deployment, documentation, and shared skills

- Added a multi-stage .NET 10 Dockerfile running as a non-root user, plus `.dockerignore`.
- Wrote a [free-hosting plan](COMPETITION-DEPLOYMENT.md), recommending one Render
  Free Docker service for a disposable synthetic demo, with environment setup,
  health checks, cold-start expectations, and a presentation fallback.
- Added/updated the [UI implementation plan](UI-IMPLEMENTATION-PLAN.md),
  [TODO list](TODO.md), [review findings](REVIEW.md),
  [original security review](SECURITY-REVIEW.md),
  [security test report](SECURITY-TEST-REPORT.md), architecture notes, README,
  and changelog. README no longer requires a developer-specific absolute path.
- Vendored `sharp-edges`, `variant-analysis`, `frontend-design`, and `webapp-testing`
  skills with source/license records and file hashes. Personal Codex/Claude skill
  links point to these shared copies; those machine-local links are not Git files.

## Remaining limitations

- No public deployment has been created; Docker execution on the chosen host is
  still unverified. The local app has been tested directly with .NET.
- Patients/sessions remain synthetic and in memory; Identity accounts use SQLite.
  Authentication tickets are per-process, so restarts sign users out and multiple
  instances require a shared ticket store.
- Tenant-aware authorization, facility identity/MFA, immutable auditing, production
  persistence/migrations, backup/restore, and clinical integrations remain open.
- Follow-up task editing/persistence and timeline filtering are not implemented.
- Shared account rate limits can affect other reviewers; HTTPS-at-edge mode still
  depends on a correctly protected hosting boundary.
- Full accessibility/screen-reader review, container scanning, and secret-history
  scanning are not claimed complete. This prototype is not ready for real patient data.
