# Security: known issues and limits

Honest list for reviewers. "Open" means not fixed. Nothing here has been reviewed by an outside party, and
this is a synthetic-data prototype, not a HIPAA-covered production system. Updated Nov 6, 2026.

## Fixed in the latest pass
| Item | What changed |
|---|---|
| Ingested sessions were invisible (wrote to memory, dashboard read the DB) | Ingest writes through the clinical database (collaborator fix), with end-to-end HTTP tests |
| Ingest replay created duplicates | Same patient + start + platform + module + duration returns the existing session |
| Ingest accepted any start time, any hardware id length | Start must be within the last 2 years and not in the future; hardware id ≤ 64 |
| Same external patient id under two providers was matched arbitrarily | Ambiguous id is refused with 409 |
| Weak or placeholder ingest key accepted in Production | Startup fails unless the key is ≥ 32 chars and not a placeholder |
| Audit trail was in memory and reset on restart | Durable, append-only, hash-chained `AuditEvents` table in the account DB; edits detected by `VerifyAsync` |
| No record of who opened a chart or exported a report | `patient-viewed` and `report-exported` events (IDs only) |
| No second factor | Optional TOTP authenticator + one-time recovery codes; `Security:RequireMfaForAdmins` makes it mandatory for admins |
| Unscoped follow-up tasks risk | Every follow-up operation resolves the patient through the assigned-provider repository; stale edits refused |

## Open: higher risk
1. **Shared global login limiter (denial of service).** The named `account` rate-limit policy uses one global partition (120 requests/min across all clients). A single client can exhaust it and block legitimate sign-ins and invite acceptance. The per-IP credential limiter (30/min) is separate. (`Program.cs`, `AddPolicy("account"...)`)
2. **Account lockout as a denial-of-service.** 5 failures lock an account for 15 minutes, so anyone who knows an email can lock that provider out.
3. **Deactivation does not end live sessions immediately.** `SetActive(false)` locks the account and rotates the security stamp, but sessions held in the in-memory ticket store stay valid until the one-hour cookie expiry or the next stamp validation.
4. **Ingest uses one facility-wide shared key.** No per-device keys, rotation, expiry, IP allow-list or request signing. A leaked key allows writing sessions for any patient. Device lookup by `HardwareId` is global and in memory (`InMemoryDataStore.Devices`).
5. **Destructive reseed paths.** `ClinicalSeeder.EnsureAsync(reseed: true)` runs at startup when `Demo:Enabled` is true and from Team → Reset demo data. On Postgres it deletes all patient, session and follow-up rows. Never enable demo mode against a database holding real data.
6. **Untrusted PDF parsing.** Caseload import reads uploaded intake PDFs (`IntakePdfReader`, `PdfContentText`, `IntakeLabeledPdf`). Reviewers should check size limits, decompression bombs, malformed-file crashes, parser DoS, and that no file content reaches logs.
7. **Invite link built from the request host.** `Url.Action(..., Request.Scheme)` uses the Host header. `AllowedHosts` blocks spoofed hosts in Production but it is `*` in Development, so a spoofed host would produce a phishing link.

## Open: medium
8. **Single-instance state.** Ticket store, invites, message outbox, chat budget, notification "last sent" map and the audit write lock are all in process memory. A second instance would break session revocation and the hash chain's ordering guarantee.
9. **Audit chain limits.** Detects edits in the middle, not truncation of the tail or replacement of the whole table, and the database owner can rewrite it. No external anchoring or log shipping yet.
10. **MFA gaps.** No TOTP replay protection (a code can be reused within its window), no device trust, recovery-code regeneration UI is missing, and the pending-MFA cookie depends on data-protection keys that are not persisted on ephemeral hosts.
11. **No email verification, password reset or email-change flow.** Invite email is not verified; no breached-password check.
12. **Assistant guardrails are regex-based.** Names and free-text identifiers are not detected, prompt-injection detection is a keyword list, and the optional OpenRouter path sends text to third-party vendors with no BAA. The model is off by default.
13. **Messages are a prototype.** Message text sits in memory only, nothing is delivered, and the identifier filter is the same regex set.
14. **Preferences are Identity claims copied into the cookie.** Values are validated against allow-lists, but claim and cookie size grow with each preference.
15. **Schema upgrade runs DDL at startup.** `SchemaUpgrade.EnsureTablesAsync` executes statements from the model's create script and needs DDL permission; there are no versioned migrations or rollback.
16. **Email notifications are untested against a real mail server.** The body is a generic count with no patient information, but TLS settings and credential handling have only been exercised through unit tests.

## Open: low / by design
17. The hosted demo login is a shared credential, and that account is also facility admin.
18. Patient assignment is one provider per patient, with no effective dates.
19. Care devices are still held in memory rather than the clinical database; device reassignment history resets on restart.
20. Content-Security-Policy allows nonce-scoped inline scripts; styles are served from `self`. Review for any remaining inline handlers.
21. Development mode keeps open self-registration (`/Account/Register`) and `AllowedHosts: *`.
22. No dependency or container image scanning in CI beyond the NuGet vulnerability check.

## Not claimed
No encryption at rest, no BAAs, no penetration test, no durable external log storage, no SSO, no disaster recovery.
