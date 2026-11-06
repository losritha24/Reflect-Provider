# Competition readiness and implementation plan

Updated October 30, 2026. This is the authoritative next-work plan for the
synthetic Reflect Provider Dashboard. Keep ASP.NET Core MVC/.NET 10, the existing
Provider area, local IBM Plex fonts, and the approved Reflect brand.

## Outcome and order

Make the judge journey reliable: introduction → sign in → inspect a follow-up
signal → inspect/filter the evidence → return to the same caseload. Then add a
small, observable follow-up action with correctly scoped persistence and history.
Use one free Render Docker service for a disposable synthetic demonstration.
Production clinical integration is a separate release with different identity,
tenancy, storage and operational requirements.

| Priority | Work package | Completion condition | Dependencies |
| --- | --- | --- | --- |
| P0 | Current authentication/configuration fixes | Logout works under exhausted login budget; explicit proxy trust; stale seeded password fails startup; literal SQLite paths; readiness fails on missing schema | Implemented; see current verification report |
| P0 | Current navigation/filter fixes | Platform + 7/30/90-day filters; full totals distinct from filtered count; future activity excluded; caseload context survives apply/reset/back | Implemented with controller/browser checks |
| P0 | Release gate | Native amd64 CI/container pass; deployed HTTPS/private-origin test; no serious/critical automated accessibility findings in tested views; clean advisory check | Host/repository access and CI execution |
| P1 | Follow-up workflow | Authorized create/complete/dismiss/reopen with transactional history, conflict handling, and browser feedback | Data contract and migrations package below |
| P1 | Consistent engagement model | One tested computation across dashboard/list/detail; evidence and period boundaries visible | Product agreement on definition, no diagnosis inference |
| P1 | Presentation polish | Useful 404/error state, mobile navigation focus review, screenshot/recording, documented reset/recovery | Browser and human review |
| P2 | Real integration | Facility identity, organization isolation, clinical API, durable audit and backups | Facility requirements, approved endpoints/access and hosting |

## Completed in this pass

- Credential POSTs use a per-connection-address 30/minute budget and a shared
  120/minute ceiling. Login GET, health probes and sign-out remain usable.
  Rejections include Retry-After. Forwarding headers alone cannot select a bucket.
- Explicit `Hosting:KnownProxies` supports one trusted proxy hop. Reject wildcard
  proxy addresses, blanket forwarded-header switches and conflicting hosting modes.
  Edge TLS mode remains an explicit private-origin deployment assumption.
- Existing seeded demo passwords must match configuration on startup. A changed
  environment variable can no longer silently leave an old password active.
- SQLite uses a connection-string builder; special filename characters remain data.
- `/health/ready` checks Identity schema availability. `/health/live` checks only
  the running process. Readiness failure returns a generic 503 response.
- Production publish excludes Development settings/public local demo credentials.
- Timeline filters, consistent future-date handling, preserved caseload context,
  table header semantics and keyboard-scrollable table regions are implemented.
- Added a Render Free blueprint, combined verification command, browser/axe test
  dependencies, expanded CI, and repository instructions for Copilot.

## 1. Release preparation — do before sending judges a URL

1. Run `python3 tests/verify.py --browser` with the documented browser dependencies.
   Save the output and inspect screenshots/axe JSON under `test-results/`.
   CI must also restore with available vulnerability advisories; a feed failure
   is not a clean dependency scan.
2. Run the native Linux amd64 container job on GitHub Actions. Local Apple Silicon
   validation covers arm64; amd64 emulation crashed in the SDK and is not an app
   test result. Use the native CI runner to close that gap.
3. Build the same reviewed commit on Render using `render.yaml`, **Free** compute,
   exact assigned hostname, and host-supplied demo credentials. Auto-deploy is off.
   Creating the Blueprint can create/start a public service; inspect its plan first.
4. Validate real edge HTTP→HTTPS, no public origin port, no forwarded-header bypass,
   secure cookies, hosted registration closure, sign-in, patient scope and logout.
   Set the health check to `/health/ready`.
5. Restart once; verify fresh synthetic records, seeded login and expected logout
   of prior sessions. Test a cold start. Do not assume free hosting stays awake.
6. Record a two-minute walkthrough and keep a local demo available. Share only
   fictional reviewer credentials and the public landing URL. No public deployment
   is performed by this planning/implementation pass.

Remaining release security checks: container OS/package vulnerability scan,
repository-history secret scan, human screen-reader review, and real host/proxy
validation. Use source-verified tools and retain reports with the exact commit/image.
Do not expand shared proxy trust or disable security controls to make a test pass.

## 2. Follow-up workflow — next substantial feature

Implement as three small pull requests; keep the UI disabled until the server
contract and authorization tests pass. This is demo workflow history, not an
immutable clinical audit system.

**Data contract and storage (backend owner)**

- Add `FollowUpTask`: Id, PatientId, AssignedProviderId, CreatedByProviderId,
  bounded Title (120 chars), optional bounded Note (1000 chars), DueAtUtc,
  Status, CreatedAtUtc, UpdatedAtUtc and integer Version concurrency token.
- Allowed state transitions: Open → Completed/Dismissed; Completed/Dismissed →
  Open through an explicit Reopen action. Record a reason for dismissal.
- Add append-only application-level `FollowUpEvent` with task, actor, prior/new
  status, UTC time and correlation ID. Save the task and event in one transaction.
  Do not call a SQLite table immutable or tamper-proof.
- Use a dedicated workflow DbContext/database with an initial EF migration;
  avoid mixing `EnsureCreated` with migrations on the current Identity database.
  Document identity migration separately. A fresh disposable demo database can
  be reset deliberately; no automatic destructive reset on startup.
- Repository methods accept current-user scope, patient ID and cancellation.
  Validate assignment through the existing patient repository on every operation;
  never trust a posted provider ID. A missing/inaccessible patient or task is 404.
- Tests: cross-provider read/write denial, deleted patient, optimistic concurrency
  conflict, duplicate submit/idempotency, event rollback on failure, length and
  date validation, cancellation, restart persistence and schema upgrade.

**Server actions and UI (Copilot-sized slice after storage)**

- Add scoped create/complete/dismiss/reopen POST actions, automatic antiforgery,
  server validation, a POST/redirect/GET flow and accessible validation summary.
- A task version travels in the form; stale updates return a clear conflict message
  and show current state. Do not silently overwrite another reviewer's action.
- Start with one task owner equal to the logged-in demo provider. Multi-provider
  reassignment requires an actual provider directory and same-organization policy.
- Add a Follow-ups tab and append task events to the timeline. Render user text
  using Razor encoding. Keep task text out of localStorage and telemetry.
- Browser acceptance: create → refresh → complete → reopen; invalid form retains
  input and focus; keyboard-only flow; no hidden failure or decorative Save button.

**Shared demo state decision**

The existing seeded account is shared. Persistent tasks would be shared too. Prefer
per-session synthetic workspaces for public judge editing, or explicitly label
shared demo state and provide an operator-only reset between demonstrations.
Choose this before enabling public task editing; a reset must not alter other
accounts or an external clinical source.

## 3. Shared engagement calculation

Extract a pure `EngagementSignalCalculator` with an explicit `asOfUtc` and activity
input. Compare [asOf−7 days, asOf] with [asOf−14 days, asOf−7 days); exclude future,
deleted and unauthorized activity before calculation. Define the behavior for
no history and a zero baseline. Keep the comparison a count-based follow-up
signal, not a diagnosis or validated clinical score.

Use it in dashboard, list and details instead of mixing seeded scenario labels
with computed trends. Update the filter to the chosen definition. Capture one
clock instant per response and one authorized activity snapshot. Tests should
cover exact boundary instants, no sessions, ties, missing patients, future data,
multiple platforms, DST display and stable tie ordering. Large real datasets
should aggregate/filter in the database/API instead of loading all sessions.

## 4. UI and accessibility backlog

- Replace bare invalid/missing-record pages with an accessible “Patient unavailable”
  view returning a real 404; never distinguish nonexistent from unauthorized IDs.
- Validate mobile navigation focus: off-screen links must not remain keyboard
  reachable while closed; Escape restores the trigger; resizing clears stale state.
- Retain the complete table alternative. Consider a mobile caseload list showing
  name, last activity and Review first, with secondary data in a disclosure.
- Add real pagination fixtures (more than 10 visible assigned patients), keyboard
  navigation, empty results and 200% browser-zoom tests. Existing enlarged-text
  checks are not a full browser-zoom or screen-reader audit.
- Include dark/light account menus, mobile navigation and enlarged-text states in
  axe and human keyboard checks. Store screenshots from the reviewed commit.
- Make display timezone configurable only after a clear user/facility preference
  exists; currently label Eastern Time and expose exact UTC source timestamps.

## 5. Production prerequisites — not implied by a successful demo

| Package | Required decision/input | Implementation and proof |
| --- | --- | --- |
| Tenant/provider model | Organization ownership and assignment rules | Explicit tenant IDs, scoped queries, reassignment/deletion rules, cross-tenant negative tests |
| Facility identity | OIDC issuer, app registration, trusted claims, onboarding/MFA policy | Verify issuer/audience, role mapping, session policy and denial paths; remove demo authentication from clinical deployment |
| Durable persistence | Approved SQL service/region and retention | Versioned migrations, encryption/key protection, backup/restore drill, concurrency and retention tests |
| Audit | Required events, access policy, retention | Central protected audit sink; actor/resource/result/correlation; tamper detection and restricted access |
| Clinical API | Approved schema, auth, data contract and source identifiers | Timeouts, cancellation, idempotent sync, stale/partial/outage states, no PHI in logs |
| Hosting/operations | Approved vendors/agreements, traffic/availability targets | Shared ticket/key storage for replicas, monitoring, alerts, incident/recovery procedures |

Do not create fake tenant, identity-provider or clinical API integrations just to
check these boxes. Their acceptance tests depend on actual facility contracts.

## 6. Copilot delegation queue

Use `.github/copilot-instructions.md`. The lead agent owns integration, security
review and final test execution. Assign one slice at a time with named file scope;
do not have two agents edit the same view/controller/test runner concurrently.

| Slice | Scope | Deliverable | Gate |
| --- | --- | --- | --- |
| A — Error UX | New not-found view + patient controller + isolated checks | Accessible 404 without existence disclosure | Same status for nonexistent/unassigned patient |
| B — Mobile nav | layoutController.js + Node/browser tests | Focus/inert behavior at viewport breakpoints | Keyboard + screen-reader review |
| C — Signal calculation | New pure service + isolated C# checks | Deterministic count comparison and boundaries | Parent reviews semantics before wiring UI |
| D — Workflow storage | New task entities/DbContext/migration/repository + isolated tests | Atomic task/history persistence | Parent reviews authorization and migration |
| E — Workflow UI | New Follow-ups partial/actions + browser checks | Working reversible action | Starts only after D's contract passes |
| F — Test fixtures/docs | Tests + release evidence | Pagination/empty-state/zoom fixtures and fresh commands | No invented pass claims |

Suggested prompt: “Implement slice B from docs/NEXT-STEPS.md. Change only the
assigned JS and its tests. Preserve current accessibility dialog behavior. Do not
edit authentication, commit, deploy or add services. Return files changed, tests
run, limitations and any required integration edits.” Give only the necessary
read/write/tool permissions. Review the diff before merging and run the combined
suite. More available credits do not remove the need for bounded tasks.

Copilot was used in this pass for a source review and the timeline/filter/navigation
implementation. Its output was reviewed and corrected during integration; the
raw review in COPILOT-REVIEW.md is historical, not the current outstanding list.
