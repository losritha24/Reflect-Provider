# HIPAA safeguards — design position

This prototype uses fictional records only and is **not HIPAA compliant**; compliance
is organizational (policies, agreements, training) as well as technical. This file maps
the HIPAA Security Rule safeguards to what exists and what is required before real PHI.

| Safeguard | Implemented in prototype | Required before real PHI |
|---|---|---|
| Access control (unique IDs, least privilege) | Identity accounts, provider role, provider-scoped repositories, negative authorization tests | Facility SSO, MFA, role mapping, tenant isolation |
| Session / automatic logoff | 1-hour absolute cookie, server-side revocation, replay checks | Idle timeout tuned with clinicians |
| Transmission security | HTTPS redirect, HSTS, secure cookies, CSP | Verified TLS at host, no public origin port |
| Integrity | Antiforgery on mutations, input validation | Tamper-evident audit records |
| Audit controls | Demo-level only | Durable protected audit sink of who viewed which record, retention, review process |
| Encryption at rest | None (disposable SQLite/in-memory) | Encrypted database and backups, managed keys |
| Minimum necessary / no PHI in logs | Chat messages never logged or stored; identifiers redacted before processing | Log scrubbing review, error pages without record existence |
| Business associate agreements | N/A (synthetic) | BAA with host, database and any AI vendor. Free tiers (Render Free, Neon, Supabase free) generally do not include a BAA |
| Breach response, risk analysis, training | Not covered | Written policies, risk assessment, incident runbook |

## Chatbot controls
1. Length cap and prompt-injection refusal (`ChatGuardrail`).
2. Identifier redaction (SSN, email, phone, dates, record numbers) before any responder.
3. No persistence or logging of message text; rate limit 20/min per provider.
4. Responses rendered as text, never HTML.
5. Before connecting a model: BAA-covered vendor, zero-data-retention setting, server-side
   calls only (no keys in the browser), output filtering, no patient records in prompts
   unless the provider explicitly selects them, and red-team tests for injection.
Pattern-based redaction misses names and free text; treat it as a first layer.
