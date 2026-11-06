# Reflect Provider Dashboard — work queue

Updated November 6, 2026. See [NEXT-STEPS.md](NEXT-STEPS.md) for sequencing,
contracts, acceptance criteria, dependencies and bounded Copilot assignments.
This prototype contains synthetic records; demo readiness does not establish
clinical production readiness.

## Implemented competition foundation

- [x] Judge-facing landing page and opt-in seeded hosted demo login.
- [x] Keep registration Development-only and newly registered caseloads empty.
- [x] Provider-scoped patient and session queries with negative authorization tests.
- [x] Explainable dashboard attention queue, platform summaries and recent activity.
- [x] Patient search, engagement filter, pagination and empty results.
- [x] Timeline with platform and rolling 7/30/90-day filters, source timestamps and table alternative.
- [x] Preserve caseload context through detail, timeline apply/reset and back navigation.
- [x] Exclude future sessions from dashboard, list and detail summaries.
- [x] Accessible tabs, skip link, focus styles, display preferences and accessibility dialog.
- [x] Secure hosted cookies, session revocation, absolute lifetime and replay checks.
- [x] Default MVC antiforgery, request size limits, no-store and browser security headers.
- [x] Credential-only rate limits; login pages and logout remain available when exhausted.
- [x] Explicit trusted-proxy configuration; reject unrestricted forwarding switches.
- [x] Validate host allowlists, literal SQLite paths and seeded password consistency.
- [x] Separate `/health/live` and Identity-schema `/health/ready` probes.
- [x] Exclude Development settings from published output.
- [x] Add reproducible verification entry point and expanded browser/configuration/state checks.
- [x] Prepare Render Free blueprint, native Linux container CI and Copilot instructions.

## Release gates

- [ ] Run native Linux amd64 CI container check on the reviewed commit.
- [ ] Scan final container OS/packages and repository history for vulnerabilities/secrets.
- [ ] Complete manual keyboard/screen-reader/actual browser-zoom review.
- [ ] Create the selected free-host service and verify its TLS/private-origin boundary.
- [ ] Rehearse deployed cold start, restart recovery and backup screen recording.

Actual local test evidence and limitations are in [READINESS-REPORT.md](READINESS-REPORT.md).
Deployment instructions are in [COMPETITION-DEPLOYMENT.md](COMPETITION-DEPLOYMENT.md).

## Demo and pitch (competition deadline)

- [x] Architecture request-flow diagram (docs/ARCHITECTURE.md).
- [x] HIPAA safeguards position (docs/HIPAA-SAFEGUARDS.md).
- [x] Time zone (EST/EDT) shown with every timestamp; DST boundary checks.
- [x] First-time "Take the tour" dialog (stored in localStorage only).
- [x] Assistant chatbot skeleton: guardrail, stub responder, rate limit, tests. No model.
- [x] Remove unused patientsIndexController.js (selector never matched the view).
- [ ] Run `python3 tests/verify.py --browser` (Playwright not installed on this machine) and confirm the tour/chat pages pass axe.
- [ ] Tech-stack slide: C#/ASP.NET MVC, JS, SQLite now / Postgres planned, Python test tooling only.
- [ ] 4-minute talk track and backup screen recording (7-minute slot).
- [ ] Rehearse cold start on Render Free (sleeps after 15 min; open the URL before presenting).
- [ ] Prepare security Q&A: data protection, authentication, authorization, chatbot safeguards.

## Data platform (Copilot candidates, see COPILOT-HANDOFF.md)

- [x] Patients and sessions stored in a database (`ClinicalDbContext`, separate from Identity) behind the existing repository interfaces; provider scoping retained; 17 DB-backed checks on real SQLite.
- [x] Postgres selectable via `Clinical__Provider=Postgres` + `ConnectionStrings__Clinical` (package added; **not yet run against a real Postgres server**).
- [ ] Replace `EnsureCreated` with versioned EF migrations (install `dotnet-ef`); required before the schema changes on a database that holds data.
- [ ] Add providers/organizations as a table and replace the single `AssignedProviderId` string with a real relationship.
- [ ] Verify against a free Neon/Supabase Postgres (case-insensitive search, tick-based date columns, unique index).
- [ ] Encrypt clinical data at rest (managed Postgres encryption, or SQLCipher for SQLite).
- [ ] XLS/CSV import for providers, validated and idempotent, with an import report.
- [ ] Database and API source adapters behind one import interface.

## UI follow-ups from review

- [ ] Narrow-screen caseload: show name, last activity and review action at 390px without scrolling (Patients/Index.cshtml `text-nowrap`).
- [ ] Browser tests: empty search result, boundary pages, keyboard search→filter→patient→back.
- [ ] Manual screen-reader and keyboard pass (record separately).
- [ ] Configurable time zone (user/facility setting).
- [ ] Chat page and tour included in axe/browser coverage.
- [ ] Orientation beyond the tour: help page or knowledge map.

## Assistant: before connecting a model

- [ ] BAA-covered vendor, zero data retention, server-side calls only.
- [ ] Output filtering and injection red-team tests.
- [ ] Optional name detection for redaction (patterns miss names).

## Next feature work

- [ ] **Follow-up workflow (next substantial feature).** Implement the contract in
  [NEXT-STEPS.md §2](NEXT-STEPS.md#2-follow-up-workflow--next-substantial-feature):
  `FollowUpTask` (bounded title/note, due time, status, integer version) plus an
  append-only `FollowUpEvent`, saved in one transaction; use a dedicated
  workflow context with an initial EF migration. Add provider-scoped
  create/complete/dismiss/reopen POST actions with antiforgery, validation,
  optimistic-concurrency conflicts, a Follow-ups tab and timeline events. Never
  trust a posted provider ID; missing or inaccessible patients/tasks are 404.
  Tests: cross-provider denial, deleted patient, stale version, duplicate
  submit, event rollback, length/date validation and restart persistence. Keep
  task text out of localStorage and telemetry.
- [ ] Implement a shared engagement calculation across dashboard, list and detail.
- [ ] Decide per-session vs shared editable demo state before exposing task editing.
- [ ] Add useful 404/error views without revealing record existence.
- [ ] Review mobile menu focus/inert behavior and full caseload responsive layout.
- [ ] Add rendered pagination fixtures, user/facility timezone settings and boundary tests.

## Before real clinical data

- [ ] Tenant/organization model and cross-tenant authorization tests.
- [ ] Approved facility identity, onboarding, MFA and role mapping.
- [ ] Persistent database, versioned Identity migrations and backup/restore verification.
- [ ] Protected shared Data Protection keys and ticket storage before multiple instances.
- [ ] Protected durable audit sink, retention, incident response and monitoring.
- [ ] Approved clinical API contract, source metadata, timeouts/retries and stale/outage states.
- [ ] Confirm vendor/region/agreement requirements with the responsible organization.

Historical assessments remain available in REVIEW.md, SECURITY-REVIEW.md,
SECURITY-TEST-REPORT.md and COPILOT-REVIEW.md. Their earlier findings and test counts
are snapshots; use NEXT-STEPS.md and READINESS-REPORT.md for current status.
