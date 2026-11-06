# Copilot handoff

Read `.github/copilot-instructions.md` first. Do not commit or create paid resources.
Run `python3 tests/verify.py` (and `--browser` for UI work) and report the real output.
Slices are independent except where noted; avoid overlapping edits to `Program.cs`.

1. **Responsive caseload (Patients/Index.cshtml, provider-workspace.css).** At 390px and 200% zoom
   show patient name, last activity and review action without horizontal scroll; secondary
   columns behind a labelled expansion. Keep `scope` attributes and filter-state links. Add browser checks.
2. **Browser coverage (tests/browser_checks.py).** Empty-result message, first/last page, keyboard
   search→filter→patient→back, chat send/refusal, tour keyboard use, axe on /Provider/Chat.
3. **Database follow-up (core store done by lead).** `ClinicalDbContext`, `EfPatientRepository`,
   `EfTherapySessionRepository` and `ClinicalSeeder` exist and are covered by `ClinicalStoreChecks`.
   Remaining: add versioned migrations (replace `EnsureCreated`), a providers table and relationship,
   and run `ClinicalStoreChecks`-equivalent tests against a real Postgres (Docker or Neon free tier).
4. **File import (after 3).** Authorized upload of XLS/CSV, size and type limits, row validation,
   idempotent upsert, per-row error report, antiforgery. No file content in logs.
5. **Configurable time zone.** Setting on the provider account; `DateTimeDisplayExtensions` takes the zone;
   tests for DST in two zones.
