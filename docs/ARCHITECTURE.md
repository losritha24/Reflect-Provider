# Architecture — Reflect Provider Dashboard MVP

## Purpose

Give clinicians longitudinal visibility into client engagement with Reflect XR (VR) and Reflect Mobile: session counts, timing, duration, platform mix, and intervention modules—using **synthetic data** in this prototype.

## Layering

```
┌─────────────────────┐     ┌──────────────────────┐
│ Reflect XR / Mobile │────▶│ Product analytics    │  (Firebase → BigQuery)
└─────────────────────┘     └──────────────────────┘
            │                           │
            └───────────┬───────────────┘
                        ▼
              ┌─────────────────────┐
              │ Clinical API        │  HIPAA boundary, PHI, RTM workflows
              └──────────┬──────────┘
                         ▼
              ┌─────────────────────┐
              │ Provider Dashboard  │  This prototype (Web MVC)
              └─────────────────────┘
```

The MVP implements only the dashboard UI and domain shapes; repositories are in-memory. On merge into Reflect XR, interfaces move to `Reflect.Domain` and EF Core backs clinical data.

## Security notes (prototype)

- Development-only account creation and sign-in use ASP.NET Core Identity with a SQLite store under the app content root's `App_Data/` directory.
- Passwords are stored as Identity password hashes, and repeated failed sign-in attempts trigger a temporary lockout.
- The SQLite directory and database file are restricted to the current OS user on Unix-like systems and excluded from Git.
- Registration assigns the provider role and does not verify email or professional credentials. Replace it with verified clinician onboarding and the facility identity provider (for example, Entra ID) before production use.
- CSP header matches Reflect XR demo policy (stand-in scripts use nonces on inline blocks).
- No real PHI—seed data is fictional.

## Extension merge checklist

1. Copy `Areas/Provider/**` into Reflect XR web project.
2. Register `ProviderOnly` policy against real Entra roles.
3. Replace in-memory repositories with EF Core + clinical API clients.
4. Drop `wwwroot/standins/` layout; views use production `_Layout.cshtml`.
5. Append `provider-menu.json` items to the facility/provider sidebar config.
