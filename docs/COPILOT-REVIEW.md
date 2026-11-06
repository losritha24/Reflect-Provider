> Historical source-review input from Copilot. Navigation, timeline filtering,
> future-session handling, table semantics, DST tests and browser coverage were
> subsequently addressed. See NEXT-STEPS.md for the remaining work. Priorities
> below are Copilot suggestions; the future-session issue is a correctness issue
> under non-seeded input, not a demonstrated security vulnerability.

# Copilot review — remaining UI, backend, and test gaps

Reviewed 2026-10-29 from the current working tree. This review is limited to
patient navigation, filters, UI accessibility, tests, and documentation
consistency. Authentication configuration, rate-limit/logout safety, Docker
readiness, and the main execution plan are intentionally out of scope.

## Prioritized findings

| Priority | Status | Issue and exact references | Acceptance criteria |
|---|---|---|---|
| P0 | **Verified in source; not exposed by the current seed** | Session inclusion is inconsistent. `PatientRowViewModel.FromEntity` (`Reflect.ProviderDashboard.Web/Models/ViewModels/Provider/Patients/PatientRowViewModel.cs:18-42`) and `PatientDetailsViewModel.PrepareData` (`.../PatientDetailsViewModel.cs:34-43`) use every repository-returned session, while `ProviderDashboardViewModel.PrepareData` (`.../Dashboard/ProviderDashboardViewModel.cs:27-48`) explicitly excludes sessions after `AsOfUtc`. A future session would therefore change list totals/detail history but not dashboard totals or signals. | Define one “as of now” rule in the repository/view-model layer and apply it to list totals, last session, detail totals/timeline/table, dashboard cards, and signals. Add a fixture with a future session and assert all screens agree; retain a separate test that cancellation propagates. |
| P1 | **Verified in source** | Detail navigation drops the active caseload context. `Patients/Index.cshtml:13-39` builds filtered/paged links, but `Patients/Details.cshtml:8-14` hard-codes “Back to patients” to bare `Patients/Index`, so search, engagement, and page state are lost. This conflicts with the documented phase-3 acceptance criterion in `docs/UI-IMPLEMENTATION-PLAN.md:118`. | Carry `search`, `trend`, and `page` (or a signed/session-backed return URL) into every detail link and the back link. A keyboard-only search → filter → patient → back test must restore the same result set and visible filter values; unsafe return URLs must not be accepted. |
| P1 | **Verified in source; behavior is untested** | The patient table is marked `text-nowrap` and wrapped in a generic responsive container (`Patients/Index.cshtml:40-61`), so narrow screens require horizontal scrolling for all columns. Table headers lack `scope` attributes, and the pagination nav (`:67-79`) has no current-page indication or accessible names beyond “Previous”/“Next”. The optional `wwwroot/js/controllers/provider/patientsIndexController.js:1-17` targets `[data-patient-row]`, while the view emits `[data-navigation-row]`; its row keyboard behavior is consequently not wired. Native patient links remain usable, but the duplicate controller is misleading and untested. | At 390px/200% zoom, expose patient/name, last activity, and review action without clipping; move secondary fields behind a usable detail/expansion pattern or provide a clearly labelled table view. Add `scope="col"`/`scope="row"`, `aria-current="page"`, disabled/omitted boundary links, and descriptive page labels. Either remove the unused controller or align and test its selector; do not maintain two navigation implementations. |
| P1 | **Verified gap in test coverage; recommendation requires browser validation** | The committed checks cover repository/controller behavior and simulated accessibility-panel focus (`tests/Reflect.ProviderDashboard.Checks/Program.cs:15-110`, `tests/layoutController.test.cjs:38-91`). `tests/browser_checks.py:42-56` checks tabs and mobile overflow, but does not exercise patient-list filter submission, pagination, return navigation, table semantics, 200% zoom, focus order, or a screen reader. No automated test renders/inspects the patient list’s accessible tree. | Add browser coverage for search + trend + pagination, filter-state round trip, empty-result messaging, boundary pages, keyboard list navigation, table header semantics, 200% zoom, and 390px layout. Add automated accessibility checks where the project can run them, and record a manual keyboard/screen-reader pass separately; do not call the latter complete based only on screenshots or simulated DOM tests. |
| P2 | **Verified in source; documented recommendation** | The UI labels patient timeline and recent activity as “Eastern Time” (`Patients/Details.cshtml:65-66`, `Dashboard.cshtml:57`), and the formatter hard-codes `America/New_York` (`Extensions/DateTimeDisplayExtensions.cs:6-19`). The source metadata shows UTC, but there is no user/facility timezone setting or boundary/DST test. `docs/UI-IMPLEMENTATION-PLAN.md:72-73` already identifies this as unresolved. | For the synthetic demo, make the fixed timezone explicit and test UTC-to-Eastern conversion across DST boundaries. Before presenting it as provider-ready, make timezone configurable and show the display zone plus exact source timestamp consistently in list, dashboard, detail, and filters. |
| P2 | **Verified documentation inconsistency** | `README.md` says patient search supports “pages of 10 records” and that checks cover paging, while `docs/REVIEW.md` and `docs/TODO.md` describe the broader UI as implemented. Those statements omit the still-unmet filter-state retention and full accessibility/screen-reader coverage; `docs/UI-IMPLEMENTATION-PLAN.md:118` does state the stronger requirement. The documentation therefore reads as if the patient-navigation slice is complete when it is not. | Update the authoritative status documentation in the same change as the fixes: distinguish repository/filter functionality from unverified rendered accessibility and explicitly mark return-context preservation and timeline filters as open until tested. Keep claims tied to named commands and test scope. |

## Small parallel work breakdown

1. **Navigation/filter slice:** preserve the caseload query state through list →
   detail → back, define safe return handling, and add browser regression cases.
2. **Responsive/accessibility slice:** fix table semantics, pagination announcements,
   narrow/zoom layout, and reconcile or remove the unused patient-row controller;
   run automated and manual accessibility checks.
3. **Data/time slice:** centralize the as-of/future-session rule and add
   timezone/DST fixtures; then update the relevant UI documentation and status
   claims.

The three slices can be implemented in parallel if they avoid overlapping edits
to `Patients/Index.cshtml`; merge validation should run the C# checks, Node tests,
hosted smoke, and browser checks together. No new issue above is claimed to have
been verified in a real screen reader or deployed container.
