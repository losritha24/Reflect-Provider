# Reflect Provider Dashboard — TODO

This project is currently a synthetic-data prototype. Complete the security,
data, and deployment work before connecting it to real patient information.

## Product direction

- [x] Add a provider-focused **Needs Attention** queue.
  - Show explainable engagement signals.
  - Include the last activity date and suggested follow-up.
  - Use "signal" or "follow-up recommended" language, not diagnosis claims.
- [ ] Make engagement trends explainable.
  - Show the comparison period and metrics used.
  - Show the sessions or activity changes behind each signal.
  - Display the data freshness timestamp.
- [ ] Add a longitudinal patient timeline.
  - Include sessions, platforms, modules, durations, reflections, notes, and tasks.
  - Include source and synchronization metadata.
- [ ] Add a provider workflow.
  - Create, assign, update, complete, and dismiss follow-up tasks.
  - Add provider notes with appropriate audit history.
- [ ] Add accessible activity visualizations.
  - Provide a table alternative for every chart.
  - Support keyboard navigation, screen readers, contrast, and reduced motion.
- [ ] Create a polished synthetic-data demo mode.
  - Provide a seeded demo account or controlled demo login.
  - Clearly label all records as synthetic.
  - Add a way to reset demo data.
  - Include simulated synchronization status and representative audit events.

## Identity and authorization

- [ ] Replace development-only self-registration with the facility identity provider.
  - Integrate Entra ID/OIDC or the approved equivalent.
  - Require verified clinician onboarding and MFA.
  - Map trusted identity claims to provider roles.
  - Disable automatic provider-role assignment for untrusted registrations.
- [ ] Implement provider-to-patient authorization.
  - Add provider, organization/facility, and patient-assignment relationships.
  - Scope every patient and session query to the signed-in provider and tenant.
  - Add authorization tests for unauthorized patient IDs and cross-tenant access.
- [ ] Configure production session security.
  - Enforce HTTPS-only secure cookies.
  - Configure persistent, protected Data Protection keys.
  - Define idle timeout, absolute timeout, and reauthentication behavior.
  - Restrict `AllowedHosts` to approved production domains.

## Clinical data and integrations

- [ ] Replace in-memory patient and session repositories.
  - Integrate the approved clinical API or persistent database.
  - Preserve the separation between product analytics and clinical documentation.
  - Define synchronization, retry, timeout, outage, and partial-data behavior.
- [ ] Add production data fields and relationships.
  - External source identifiers.
  - Source system and synchronization timestamps.
  - Provider and organization ownership.
  - Consent/status fields where required.
  - Versioning and soft-delete metadata.
- [ ] Move filtering and aggregation to the data/API layer.
  - Add server-side search, sorting, date ranges, and pagination.
  - Avoid loading the complete patient and session population into memory.
- [ ] Configure and test time zones.
  - Make the display time zone configurable by facility or user.
  - Test Windows/Linux time-zone identifiers and daylight-saving transitions.
- [ ] Document interoperability requirements.
  - Define mappings for Patient, Practitioner, Organization, Encounter,
    Observation, CarePlan, and Task if FHIR integration is needed.
  - Do not claim FHIR compliance until it is implemented and tested.

## Privacy, security, and compliance

- [ ] Add an immutable audit trail.
  - Record sign-ins, sign-outs, patient list/detail access, searches, exports,
    notes, task changes, authorization failures, and administrative changes.
  - Include actor, organization, patient/resource, timestamp, outcome, and
    correlation ID.
- [ ] Define retention and operational policies.
  - Audit retention and review.
  - Data retention and deletion.
  - Backup and restore.
  - Incident and breach response.
  - Security monitoring and alerting.
- [ ] Review clinical-data handling for HIPAA and organizational requirements.
  - Confirm hosting provider, database, identity provider, and vendors support
    the required agreements and controls.
  - Do not use real PHI in the current prototype.
- [ ] Harden browser and API security.
  - Review CSP, security headers, CORS, CSRF, XSS, and error disclosure.
  - Keep sensitive patient data out of browser local storage.
  - Restrict and audit exports.
- [ ] Add health and readiness endpoints.
  - Report application, database, identity, and clinical API dependency health.
  - Avoid exposing sensitive configuration or patient data.

## Reliability and engineering quality

- [ ] Add automated tests.
  - Domain and view-model unit tests.
  - Repository and integration tests.
  - Authentication and authorization tests.
  - Negative tests for invalid, deleted, unauthorized, and cross-tenant records.
  - UI smoke and accessibility tests.
- [ ] Add CI/CD.
  - Restore, build, test, analyzers, and publish checks.
  - Dependency and secret scanning.
  - Preview deployment validation.
  - Production deployment approval gates.
- [ ] Add resilience behavior.
  - Propagate cancellation tokens to database/API calls.
  - Configure dependency timeouts.
  - Add appropriate retries and circuit breakers.
  - Provide user-facing stale-data and dependency-failure states.
- [ ] Replace `EnsureCreatedAsync()` with versioned database migrations.
  - Define migration deployment and rollback procedures.
  - Add backup/restore verification.
- [ ] Add structured logging and correlation IDs.
  - Avoid logging PHI, credentials, tokens, or unnecessary identifiers.
  - Centralize error handling and map missing patients to a proper 404.

## Hosting and deployment

- [ ] Choose a .NET-compatible production host.
  - Preferred initial options: Azure App Service or Azure Container Apps.
  - Evaluate Render, Railway, Fly.io, or another provider for prototype use.
  - Verify healthcare compliance, regional hosting, backups, and support.
- [ ] Do not deploy the current SQLite/in-memory implementation with real PHI.
  - Use a managed production database.
  - Store secrets in a managed secret store.
  - Configure persistent Data Protection keys.
- [ ] Evaluate Vercel only as an intentional architecture choice.
  - Vercel is better suited to a future Next.js/React frontend.
  - If using Vercel containers for ASP.NET, validate runtime, persistence,
    scaling, logging, regional, and compliance requirements first.
  - A recommended split is Vercel frontend plus separately hosted ASP.NET Core
    clinical API and managed database.
- [ ] Add deployment documentation.
  - Supported .NET SDK/runtime version.
  - Environment variables and secret configuration.
  - Domain and HTTPS setup.
  - Database migrations.
  - Health checks, monitoring, rollback, and disaster recovery.

## Current functional fixes

- [x] Fix dashboard card and recent-session row navigation.
  - Support click, Enter, Space, focus, and screen-reader interaction.
- [x] Add proper not-found handling for invalid patient IDs.
- [x] Add search, filtering, and pagination to the patient list.
- [x] Persist accessibility preferences intentionally and implement focus
  trapping for the accessibility panel.
- [x] Replace developer-specific absolute paths in [README.md](README.md).

## Recommended implementation order

1. Define the identity, tenant, provider-to-patient authorization, and audit
   model.
2. Replace in-memory data access with a scoped clinical repository/API.
3. Implement production authentication and secure deployment configuration.
4. Add the Needs Attention queue, explainable trends, timeline, and follow-up
   workflow.
5. Add automated tests, CI/CD, observability, resilience, and migrations.
6. Select and validate hosting, then run a security/compliance review before
   any real patient data is introduced.

## Competition review pass (2026-10-29)

- [x] Add an opt-in seeded hosted demo login; keep hosted registration closed.
- [x] Harden cookies, caching, browser headers, CSRF defaults, and account rate/request-size limits.
- [x] Add accessible patient tabs, a skip link, and an explainable session-count attention queue.
- [x] Add Docker build instructions, CI checks, and a comparison of free hosting options.
- [x] Add liveness and hosted-demo HTTP regression checks.
- [ ] Validate the Docker image and chosen hosting environment end to end.
- [ ] Complete browser, mobile, contrast, and screen-reader review.

See [review findings](docs/REVIEW.md) and [competition deployment](docs/COMPETITION-DEPLOYMENT.md).
The broader production tasks above remain open unless explicitly checked.

## Skill-assisted assessment and UI plan (2026-10-29)

- [x] Install shared Codex/Claude security and UI skills with source/license records.
- [x] Run local HTTP, Chromium, repository/configuration and static/dependency checks.
- [x] Reject unsafe hosted allowlists and require an authenticated provider in repositories.
- [x] Read the supplied Reflect XR visual identity guide and preserve its fonts/colors in the UI plan.
- [x] Add per-session revocation and a captured-cookie replay regression check.
- [ ] Partition account rate limits and validate the real HTTPS proxy boundary.
- [ ] Implement phases 1–3 of [the UI plan](docs/UI-IMPLEMENTATION-PLAN.md).
- [ ] Complete container, secret-history, automated accessibility and human screen-reader checks.

Assessment evidence and limitations: [security test report](docs/SECURITY-TEST-REPORT.md).

- [x] Start UI foundation: brand typography, contrast fixes and compact mobile account controls.

## Judge-facing UI batch

- [x] Build a public demo introduction and simplify login messaging.
- [x] Redesign dashboard worklist, platform summary, and recent activity.
- [x] Add chronological session/reflection timeline and source details.
- [x] Polish caseload search/filter layout.
- [x] Update the free Render hosting release plan.
- [ ] Add timeline filtering and provider follow-up persistence/audit.
- [ ] Validate Docker and publish the chosen free-host demo.
