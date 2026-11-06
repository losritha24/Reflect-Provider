# Changelog

## 2026-11-06

- Seeded sessions now use official Reflect Mobile and Reflect XR Home module titles (Breathing Mandala, Color Psychology, Flow, and so on).
- Added a Reports page: pick a patient and date range, preview name / period / main module, then print or download a PDF.
- After sign-in, clinicians land on a caseload knowledge graph of patients, sessions, devices, and follow-up signals.
- Normalized timestamps to UTC for RDS PostgreSQL; display uses the configurable company time zone.
- Added signed session ingest (`POST /api/v1/ingest/sessions`) and a Trust page covering HIPAA, CIA, TLS, and upload options.

## 2026-10-29

- Added calculated Needs Attention signals, comparison windows, and synthetic freshness.
- Added opt-in hosted demo access, security hardening, liveness, and HTTP smoke checks.
- Added keyboard-accessible patient tabs, skip navigation, Dockerfile, CI, and free-hosting/security review docs.

- Fixed dashboard and patient-row navigation with native links and Space support.
- Return 404 for malformed, missing, and deleted patient IDs.
- Added repository-level patient search, engagement filtering, and pagination.
- Persist accessibility display preferences and trap/restore modal focus; respect reduced motion.
- Added focused C# regression checks and Node accessibility behavior tests.
- Replaced the developer-specific README path.

## 2026-10-27

- Enforced per-request CSP nonces for inline scripts.
- Matched the extension theme tokens and used theme text colors for dashboard statistics.
- Added an off-canvas provider sidebar for narrow screens and a collapsible desktop navigation.

## 2026-10-16

- Initial MVP: Provider area with dashboard, patient list, and patient detail (overview + session history).
- Domain entities `Patient`, `TherapySession` with in-memory synthetic seed data.
- Stand-in Reflect XR-themed layout, theme toggle, accessibility panel, CSP header.
- Demo provider authentication (no Entra integration).

## October 29, 2026 — readiness pass

- Add timeline filters and retain caseload navigation context.
- Correct future-session counts and test Eastern DST display.
- Keep logout available under login throttling; validate explicit proxy trust.
- Detect seeded credential mismatch and safely encode SQLite paths.
- Add database readiness, publish hygiene, browser/axe checks and container CI.
- Prepare a Free Render Blueprint and detailed next-work/Copilot handoff plan.
