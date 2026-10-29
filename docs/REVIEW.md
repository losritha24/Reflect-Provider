# Competition readiness and security review

October 29, 2026. Scope: source review, compilation, dependency advisory check,
focused automated checks, and localhost hosted-demo HTTP tests. This is not a
penetration test, formal accessibility audit, or clinical compliance assessment.

## Implemented in this pass

- **UI:** Needs attention queue calculated from session counts in adjacent seven-day
  windows, explicit period boundaries and synthetic-data freshness, last activity,
  suggested check-in, and direct review links. No diagnostic inference.
- **Accessibility:** patient tab roles, selected state, roving focus, arrow/Home/End
  keys, and a skip-to-content link.
- **Hosted access:** opt-in seeded reviewer account in Production; registration
  stays Development-only. Hosted login accepts only the configured demo account.
- **Security:** secure hosted auth/antiforgery cookies, fixed one-hour auth lifetime,
  global MVC CSRF validation, account request-size and rate limits, generic login
  failures, no-store responses, restrictive browser headers, narrower CSP,
  allowlisted badge colors and escaped badge text, explicit hosted AllowedHosts.
- **Authorization integration:** preserved the concurrent patient-assignment changes and applied the same scope to session history and aggregate counts. New local registrations have no assignments; enable the seeded demo account to explore the fictional caseload.
- **Backend:** dashboard drops orphan/future sessions from recent history, queue
  groups activity once, liveness endpoint, non-root Dockerfile, CI checks.
- **Filesystem:** creating an account directory no longer changes permissions of
  an already-existing parent directory chosen through configuration. Use a dedicated
  private account directory; do not put the database in a shared directory.

## Remaining issues, in priority order

| Priority | Finding | Impact and next action |
| --- | --- | --- |
| Critical before real data | Prototype now enforces provider assignment for patients and sessions, but has no organization/tenant model | Replace in-memory assignments with tenant-aware database relationships, verified identity/MFA, and cross-tenant negative tests before any real patient connection. |
| High before real data | No immutable access/change audit, retention, or incident controls | Establish organization-owned policies and durable audited storage. |
| High before durable hosting | SQLite, EnsureCreated, ephemeral keys, no migration/restore workflow | Use versioned migrations, managed persistence, protected persistent keys, and tested recovery. Demo restarts deliberately lose state. |
| Medium for public demo | Shared account can be locked out; global rate budget can be exhausted | Share credentials privately, test before judging, keep local fallback. Add per-client/edge limits and individual reviewer accounts if broad public traffic is expected. |
| Medium for UI accuracy | Existing list/detail trend badges are seeded labels; queue is calculated | Badges now identify themselves as synthetic scenario labels; use one tested signal service across all screens in a future pass. |
| Medium for scale | Dashboard reads all sessions; list performs one session query per displayed patient | Add repository-side grouped summaries and date filtering when replacing the in-memory store. |
| Medium for UX | Details have no chronological notes/task workflow; empty/error states are basic | Add a compact longitudinal timeline and reversible demo follow-up tasks with persistence and audit together. |
| Validation gap | No real browser/screen-reader/mobile visual audit in this pass; container daemon unavailable | Verify narrow screens, dark-mode contrast, text zoom, tab order, and container startup before judging. |

## Suggested next UI pass

1. Keep the dashboard story focused: “who needs a check-in, why, and what happened.”
2. Add a patient timeline that combines sessions and provider follow-ups, with
   source/timezone and freshness visible. Keep clinical notes visibly synthetic.
3. Add a small current-versus-previous-period chart with a matching data table;
   avoid decorative charts that cannot explain a signal.
4. Replace blank 404 pages with a helpful return-to-caseload screen. Improve filter
   spacing, narrow-screen tables, and empty-state copy after browser review.

The next substantial backend milestone is the tenant/provider/assignment model,
then scoped persistence. Hosting this fictional demo does not require pretending
those production capabilities already exist.

## Verification results

- 19 C# regression assertions passed, including provider isolation for patient
  queries, session history, and aggregate counts.
- 3 Node accessibility behavior tests passed.
- Hosted Production-mode HTTP smoke passed: seeded login, registration disabled,
  antiforgery rejection, safe return URL, secure cookies, no-store/security headers,
  protected routes, queue rendering, search, invalid-ID 404, and rate limiting.
- NuGet vulnerability audit (including transitive dependencies) reported no known
  vulnerable packages from the configured advisory source at review time.
- Docker runtime and real hosted deployment remain unverified because the local
  Docker daemon is unavailable. No public deployment was created.
