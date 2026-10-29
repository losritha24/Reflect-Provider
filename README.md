# Reflect Provider Dashboard (PTI Challenge X MVP)

Prototype **mental health provider dashboard** for Persistent Technology’s Immersive Arts for Wellness products (**Reflect XR** and **Reflect Mobile**). Built as a standalone ASP.NET Core MVC extension demo aligned with PTI’s Reflect XR extension tech spec (mergeable `Provider` area, synthetic data only).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or retarget projects to `net8.0` if needed)

## Run locally

```bash
cd Reflect-Provider
dotnet build
dotnet run --project Reflect.ProviderDashboard.Web
```

Open the URL shown in the console (default `https://localhost:7150`). Root `/` redirects to the provider dashboard.

### Doctor accounts

Create a local account from the sign-in page. Accounts are stored in `App_Data/provider-accounts.db` under the app's content root; the directory is private to the current OS user and excluded from Git. ASP.NET Core Identity stores salted password hashes, applies password complexity requirements, and temporarily locks accounts after repeated failed sign-in attempts.

Account creation is enabled only in Development. Sign-in is also available in Production when explicitly configured for the seeded synthetic competition demo (see the deployment guide). This self-registration flow assigns the provider role but does not assign patients; use the configured seeded demo account to view the fictional caseload. Replace it with the facility's verified identity provider and controlled clinician onboarding before production use.

## Screens and URLs

| Screen | URL |
|--------|-----|
| Provider dashboard | `/Provider/Home/Dashboard` |
| Patient list | `/Provider/Patients/Index` |
| Patient detail (overview + sessions) | `/Provider/Patients/Details/{patientId}` |

Example patient: `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3` (Sam Patel — declining engagement).

## Architecture (target vs prototype)

**Production (Challenge X):**

- Reflect XR / Reflect Mobile → Firebase Analytics → BigQuery
- Secure clinical API/backend (HIPAA) → Provider Dashboard
- Separate **product analytics** from **clinical/RTM documentation** layers

**This prototype:**

- `Reflect.ProviderDashboard.Domain` — entities, enums, repository interfaces
- `Reflect.ProviderDashboard.Web` — MVC `Provider` area, in-memory patient repositories, local Identity accounts in SQLite
- Stand-in layout/CSS/JS under `wwwroot/standins/` (delete on merge into Reflect XR)

## Entities added

- `Patient` — caseload member (synthetic ID, engagement trend, notes)
- `TherapySession` — session date/time, duration, platform (VR/Mobile), module, reflection summary

## Menu JSON (for Reflect XR merge)

`Reflect.ProviderDashboard.Web/Areas/Provider/provider-menu.json`

## Vendor libraries

Copy Bootstrap 5.3.3, jQuery 3.7.1, DataTables, RemixIcon v4.2, and IBM Plex fonts into `wwwroot/assets/libs/` per the PTI extension spec before production merge. The MVP uses stand-in CSS/JS so the app runs without those downloads.

## Documentation

See `docs/ARCHITECTURE.md` and `CHANGELOG.md`.

## Development checks

```bash
dotnet run --project tests/Reflect.ProviderDashboard.Checks
node --test tests/layoutController.test.cjs
```

The dependency-free check runner covers patient search, paging, cancellation,
and controller handling of invalid, missing, and deleted patients. The Node tests
cover accessibility focus and preference storage; they do not replace browser
or screen-reader testing.

Patient search supports name or external ID, engagement filters, and pages of
10 records. Display preferences (theme, text size, color-blind mode) are stored
locally in this browser; patient data is not stored in browser storage.

## Competition hosting and review

See [free hosting options and deployment steps](docs/COMPETITION-DEPLOYMENT.md)
and the [UI/backend/security review](docs/REVIEW.md). A Dockerfile and CI workflow
are included. Hosted demo mode requires explicit configuration; never expose the
Development environment publicly. `/health/live` reports process liveness only.

## Shared skills, security assessment, and UI roadmap

- [Installed shared skills](skills/README.md)
- [Latest local vulnerability assessment](docs/SECURITY-TEST-REPORT.md)
- [Brand-constrained UI implementation plan](docs/UI-IMPLEMENTATION-PLAN.md)

### Stable local demo login

In Development, the app seeds `doctor@reflect.demo` with password
`ReflectDemo2026!` and assigns the synthetic caseload. These intentionally public
demo credentials are configured only in `appsettings.Development.json`.
Run `dotnet run --project Reflect.ProviderDashboard.Web` and use the displayed URL.
New self-registered accounts start with no assigned patients. Existing account
passwords are not overwritten by startup seeding. Hosted Production deployments
must explicitly supply their own `Demo__Enabled`, `Demo__Email`, and
`Demo__Password` settings; the Development file is not loaded there.
