# Changelog

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
