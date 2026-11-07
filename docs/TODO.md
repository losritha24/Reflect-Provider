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
- [x] Official module catalog (14 Mobile + 17 XR, from the 2026-09-09 Product Catalog): seed data uses real
  modules, ingest rejects unknown or wrong-platform modules and stores the catalog spelling, sessions show
  "Program · Module", patient overview has a "Programs used" panel, sidebar "Module reference" page.
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
- [x] Assistant chatbot skeleton: floating logo bubble on every page, guardrail, stub responder, rate limit, tests. No model.
- [x] Privacy & trust page redesigned with honest status pills (In place / Production target / Not yet).
- [x] Remove unused patientsIndexController.js (selector never matched the view).
- [x] `tests/verify.py --browser` passes (108 checks); axe shows no WCAG A/AA violations on dashboard, Privacy & trust, caseload map, devices, open chat panel and tour dialog (Nov 6).
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
- [x] Company-wide display time zone via `Company:TimeZone` (UTC stored). [ ] Per-user/facility setting is still open.
- [x] Add Trust, caseload map, Module reference, open chat panel and tour dialog to `tests/browser_checks.py`; axe reports zero A/AA violations on all audited views (Nov 6).
- [x] Caseload knowledge graph landing page and Trust/connectivity page (added by teammate/Freebuff). Graph is now a dashboard suggestion + "Caseload map" nav link, not the landing page.

## Found in review of the Graph / Ingest / Trust commits (Nov 6)

- [x] **Ingest reaches the dashboard.** `SessionIngestService` writes through `ClinicalDbContext`; the checks confirm an accepted session is visible through `EfTherapySessionRepository`. The disposable demo still reseeds on restart by design.
- [x] Ingest resolves patients through `ClinicalDbContext` rather than the global seed list.
- [ ] Ingest is not idempotent: replaying a POST creates a duplicate session. Add a client event ID or (patient, startedAt, module) uniqueness.
- [ ] Ingest input limits: catalog validation, external-ID length and reflection-summary length are in place; still reject absurd/future `startedAt` values.
- [ ] Ingest startup guard: when `Integration:Enabled`, require a strong `Integration:ApiKey` from a host secret; never ship a key in appsettings; add a rate limit tuned for integrations and an audit entry per accepted row.
- [ ] Ingest check coverage: current tests call the service directly. Add HTTP-level tests (disabled → 404, missing/wrong key → 401, oversize → 413, valid → 202 and visible in the dashboard).
- [x] Trust page wording now calls the integration a shared-secret header and marks encryption at rest and the BAA as production targets.
- [ ] Move care devices (and assignments) into `ClinicalDbContext`; they are still in memory.
- [x] README, the provider menu JSON and the screen table now describe the clinical store and Dashboard home. Keep NEXT-STEPS current as the schema changes.

### Delivered Nov 6 (second pass)
- [x] Ingest: idempotent, start-time bounds, hardware-id limit, ambiguous-id refusal (409), production key-strength guard, HTTP-level tests.
- [x] Durable hash-chained audit log (account DB) incl. patient views and report exports; Team and Settings read from it.
- [x] Follow-up tasks (versioned, append-only history, provider-scoped) on the patient record and dashboard.
- [x] Program filter on the timeline, per-program last-30-day counts and "not yet tried".
- [x] Weekly digest page (printable).
- [x] Generic email notifications: preferences, quiet hours, daily worker, test button. Off until SMTP is configured; untested against a real mail server.
- [x] TOTP MFA with recovery codes; `Security:RequireMfaForAdmins` option.
- [x] Demo: reset button, `docs/DEMO-SCRIPT.md`.
- [x] `docs/SECURITY-KNOWN-ISSUES.md` and `docs/SECURITY-REVIEW-PROMPT.md` for an outside review.
- [ ] Still open: care devices in the clinical DB (device lookups for ingest are still in memory), versioned migrations, MFA enforced by default, TOTP replay protection, push notifications, two-way messaging, SSO.

### Module catalog follow-ups
- [ ] Plan/entitlement per patient or device (Free/Basic/Premium/XR Home): flag modules outside the plan and show AI image usage against the 20/30 monthly allowance. Needs a schema change and seed data.
- [ ] Program filter next to the platform filter on the patient timeline, and program-level trends.
- [ ] Tour copy and chatbot starter chips still use generic wording; use real module names and let the stub answer "what does Hope Tree do?" from the catalog (no API).
- [ ] Catalog is a consumer list; confirm with Persistent Technology whether clinical editions add or rename modules before real data.
- [ ] Catalog changes need a code change; decide whether it should move to a table once migrations exist.

## Accounts, administration and provider settings

Built as a prototype slice (Nov 6): invite-only onboarding (`/Account/AcceptInvite`), a Team page
(invite, revoke, deactivate, assign patients, audit trail), Settings (profile, display, notifications,
plan placeholder, recent activity) and a Messages outbox. The demo login is also `Facility_Admin`.
Limits: invites, the audit trail and the message outbox are in memory (`TeamStore`) and reset on restart;
invite links are copied by the admin (no email); notification preferences are saved but nothing is sent;
per-account time zone and clock are saved but not applied; patient assignment is one provider per patient.
Order matters: A before B and C; D is independent of A-C until it needs recipients.

### A. Admin and onboarding (needed before any real provider uses it)
- [ ] **Facility and role model.** Add a `Facility` and a `providers` table (see Data platform item on providers). Roles: `Provider`, `FacilityAdmin`. Admin role gets no clinical-record access by default (minimum necessary); it manages people, not charts.
- [x] (prototype: in-memory, link copied by admin, no email) **Invite-only onboarding.** Admin enters an email and role; app creates a single-use invite token (hashed at rest, 72-hour expiry, revocable). The invitee sets a password and a display name on first visit. Registration stays closed in every non-Development environment. Tests: expired, reused and revoked tokens; token never logged.
- [x] (prototype) **Admin user list.** Deactivation locks the account and rotates its security stamp; open sessions end within the one-hour cookie lifetime. No "last sign-in" column yet.
- [x] (prototype: one provider per patient, no effective dates or sharing) **Assign patients to providers.** Admin UI to assign, reassign and share a patient between providers, with effective dates. Replace the seed-time `AssignPatientsToProvider` with this. Reassignment must keep past sessions and notes attributed to the original provider. Tests: another provider's patient stays invisible (existing fail-closed checks must still pass).
- [ ] **Add patients.** Admin or provider can create a patient (synthetic-ID rules for the demo, real MRN mapping later), link them to Reflect Mobile and XR installs, and archive (soft-delete) them. Overlaps the XLS/CSV import slice in COPILOT-HANDOFF.md; import must use the same service.
- [~] (prototype: in memory, covers sign-in, invites, assignments, settings and queued messages; not durable, does not record patient views or exports) **Audit log.** Append-only record of sign-ins, invites, role and assignment changes, patient views and exports (who, what, when, from where). Admin-readable, tamper-evident, no PHI in the log body. Needed for the HIPAA story on the Trust page.

### B. Account security
- [ ] Password reset by emailed one-time link (rate limited, same response whether or not the email exists).
- [ ] Multi-factor authentication (TOTP first; recovery codes). Required for admins.
- [ ] Account lockout after repeated failures and a visible "recent sign-in activity" list for the user.
- [ ] Session management: see and revoke other active sessions; idle timeout setting per facility.
- [ ] Password policy and breach-list check; forced change after an admin reset.

### C. Provider settings (new Settings page, replaces the bare "Account & display" menu)
- [x] **Profile.** (Email change needs re-verification, not built.)
- [~] (stored as Identity user claims; theme, text size, landing page and tour toggle take effect; time zone and clock saved only) **Preferences, stored per account (not just localStorage).** Theme, text size, reduced motion and the other accessibility options now kept in the browser; plus default landing page, default patient sort and trend filter, rows per page, and whether to show the first-visit tour again.
- [ ] **Time zone and formats.** Per-account time zone (today `Company:TimeZone`, see Configurable time zone in COPILOT-HANDOFF.md), 12/24-hour clock, date format.
- [~] (saved only, nothing is sent) **Notification preferences.** Which alerts a provider receives (patient quieter this week, below personal baseline, new message from a patient) and the channel for each (in-app, email, push to the provider's own device), with quiet hours.
- [~] (placeholder card only) **Plan and billing page.** Read-only for providers, editable by admin. Blocked on real clinical pricing from Persistent Technology; the consumer price list is in the Product Catalog PDF and is not for this tool. Until then show the plan name, seat and device counts, and a contact link.
- [ ] **Data and privacy.** Download my account data, a list of my own recent record access (from the audit log), and a sign-out-everywhere button.

### D. Communication with patients (needs a product decision first)
The app is read-only today: it shows patient activity and has an assistant stub, but no way to reach a patient.
Decide scope before building. Recommended order:
- [ ] **D0. Decide the model.** One-way "nudges" (provider to patient, e.g. "please try Breathing Mandala this week") vs two-way secure messaging. Recommend one-way nudges and care-team announcements first; two-way messaging carries clinical-response expectations and liability (who reads it, how fast, after hours).
- [~] (prototype: queued per provider, identifiers removed by the chat guardrail, status "not delivered"; no groups or templates beyond four presets) **D1. Message outbox in the dashboard.** Provider composes a nudge to one patient or a filtered group; recipients are limited to the provider's assigned patients; send history and delivery status per patient. Templates for common nudges. No PHI in free text warnings, reuse `ChatGuardrail` redaction.
- [ ] **D2. Delivery into Reflect Mobile and XR.** Patient apps poll or receive messages through a signed API; shown in an in-app inbox. Needs the patient-app team and a patient-facing auth model. Define the API contract in docs first.
- [ ] **D3. Push notifications.** APNs (iOS), FCM (Android) and Meta Quest notifications as a separate pass. Rules: the push payload must be generic ("You have a new message from your care team") with no names, module names, scores or clinical content; details only after the patient opens the app and signs in. Per-device tokens stored per patient install and revoked on sign-out or device reassignment. Patient opt-in and quiet hours; opt-out honoured immediately. Confirm that every provider in the chain (push services, email) is covered by a BAA before any real data.
- [ ] **D4. Consent and preferences on the patient side.** Patient chooses what the care team may send and by which channel; consent recorded with timestamp; revoking consent stops sends.
- [ ] **D5. Two-way messaging (later, only if D0 says yes).** Needs a response-time policy, after-hours auto-reply, escalation for crisis language (route to a crisis resource, never to the assistant), retention policy and records access rules.
- [ ] Rate limits and abuse controls on all send paths; every send written to the audit log.

### E. Demo-safe slice (if time is short before the competition)
- [x] Add a read-only **Settings** page with Profile, Preferences (theme, text size, time zone, tour toggle) saved on the account, and a plan placeholder.
- [x] Add an admin-only **Team** page with an invite form that creates a pending invite for synthetic emails (no email sent) and a patient-assignment screen over the seeded patients.
- [x] Add a **Message a patient** mock under the patient record that writes to the outbox and shows "delivered" with no real push; label it clearly as a prototype.
- [x] Add a line to the Trust page: accounts are invite-only, patients are assigned by an administrator, pushes carry no patient information. Only claim what exists.

## Assistant: before connecting a model

- [x] OpenRouter responder (`OpenRouterChatResponder`), switched off by default (`Chat:OpenRouter:UseModel=false`); needs `UseModel=true` plus a key in user secrets or `Chat__OpenRouter__ApiKey`. Free models were rate limited on the shared pool (429) when tried; use a paid model; sanitized text only, no patient data, 15 s timeout, 500 calls/day cap, falls back to the stub on any failure. Prototype/synthetic data only.
- [ ] BAA-covered vendor, zero data retention, server-side calls only. OpenRouter routes to third-party vendors without a BAA; pick a covered provider before real data.
- [ ] Per-user daily cap and cost alerting (today the cap is global and in memory).
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
