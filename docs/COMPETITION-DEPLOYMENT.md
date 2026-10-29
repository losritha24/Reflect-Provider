# Competition demo hosting

Reviewed October 29, 2026. This guide is for fictional records only.

## Free options

| Option | Fit for this repository | Limits to plan around |
| --- | --- | --- |
| **Render Free web service — recommended** | Deploy the included Dockerfile; hosted HTTPS URL; no frontend rewrite. | Sleeps after 15 minutes; wake-up takes about a minute. Local SQLite and keys disappear on restart/redeploy/sleep. 750 instance hours per workspace/month; bandwidth/build limits also apply. |
| **Azure App Service F1** | Native ASP.NET hosting; verify .NET 10 runtime availability in the selected OS/region, or deploy self-contained. | 60 CPU minutes/day, 1 GB RAM and 1 GB storage on the documented Windows Free tier; shared compute. Use the supplied azurewebsites.net HTTPS hostname. |
| **Koyeb Free instance** | Alternative container host for the same Dockerfile. | Free instance sleeps after one idle hour. Check current account eligibility, region, memory, and storage restrictions before committing. |

Sources: [Render free service limits](https://render.com/docs/free),
[Azure App Service pricing](https://azure.microsoft.com/en-gb/pricing/details/app-service/windows/),
[Azure plan behavior](https://learn.microsoft.com/en-us/azure/app-service/overview-hosting-plans),
[Koyeb free instances](https://www.koyeb.com/docs/reference/instances).
These are current published offers, not guarantees of eligibility or future pricing.
Choose only the free compute tier and review billing/spend controls; extra resources
and overages can cost money. A free Render Postgres database expires after 30 days;
this disposable demo does not need one.

GitHub Pages/static hosting cannot run this MVC backend. A frontend rewrite would
add competition work without improving the core demo. Keep the current architecture.

## Render setup

1. Push this repository to your own Git host. Create a **Web Service**, choose
   **Docker**, repository root as context, `Dockerfile`, and **Free** instance.
2. Set the following environment variables in the host dashboard. Do not commit
   credentials or paste them into build arguments.

   | Variable | Value |
   | --- | --- |
   | `ASPNETCORE_ENVIRONMENT` | `Production` (already the container default) |
   | `ASPNETCORE_HTTP_PORTS` | `8080` (container default; expose this service port) |
   | `AllowedHosts` | The actual assigned hostname, e.g. `your-demo.onrender.com`; no scheme/path/wildcard |
   | `Demo__Enabled` | `true` |
   | `Demo__Email` | A fictional reviewer address, e.g. `reviewer@example.invalid` |
   | `Demo__Password` | A unique secret, at least 12 characters including upper/lowercase, digit, symbol |
   | `Hosting__HttpsAtEdge` | `true`, only with enforced public HTTPS and no direct public access to the container HTTP port |

3. Set the health check path to `/health/live`. It reports process liveness only;
   it does not assert database, identity-provider, or clinical-API readiness.
4. Open the HTTPS URL and sign in using the seeded account. Public registration is
   disabled outside Development. Share credentials privately with judges.
5. Verify HTTP redirects to HTTPS at the host edge, the app's HTTP port is not
   publicly reachable, sign-in works, patient filtering works, and missing patient
   URLs return 404. Test a cold start before presenting.

`Hosting__HttpsAtEdge` is an explicit trust assumption: it treats all backend
requests as HTTPS without trusting user-supplied forwarding headers. Never enable
it on an HTTP server exposed directly to the internet. Other deployment topologies
should use HTTPS directly or a specifically trusted reverse-proxy configuration.

The startup seed recreates a missing demo account after storage loss. Existing
accounts retain their password: changing `Demo__Password` does not rotate an
existing account. For this disposable demo, reset its account database/redeploy
onto a clean filesystem to rotate credentials; active cookies also need revocation
or key replacement. Do not use this reset procedure with production records.

SQLite accounts, session cookies/Data Protection keys, and synthetic activity are
disposable. Restarts may sign reviewers out. One instance is intentional. There is
no clinician onboarding, MFA, tenant separation, durable audit trail, or real
clinical integration. Never connect patient data to this deployment.

## Presentation reliability

Open the app a few minutes before judging; do not rely on free hosting having no
cold start. Keep a short screen recording and a working local demo as backup.
Seeded records regenerate on process startup. There is no in-app reset button yet.
Account endpoints share a 30-request/minute budget; an attack can exhaust it and
five failed password attempts can temporarily lock the shared demo account.

## Build and verification

```bash
dotnet run --project tests/Reflect.ProviderDashboard.Checks
node --test tests/layoutController.test.cjs
python3 tests/hosted_demo_smoke.py
docker build -t reflect-provider-demo .
```

The container runs as the non-root `app` user. CI builds, runs regression and HTTP
smoke checks, and publishes the app. This change does not create a hosting account
or publish a public deployment. Container execution still needs verification with
a running Docker daemon and on the chosen host.

Authentication tickets now live in a per-process server-side store. Logout revokes
only that session; process restarts sign everyone out. Use a shared ticket store
before scaling beyond one instance.

## Release plan for the redesigned demo

Recommended: one Render Free Docker web service. No external database or paid
custom domain is necessary for these disposable synthetic records. The public root
URL now shows the judge introduction; signed-in visitors go to the dashboard.

1. Finish the local review using the landing → login → Needs attention → Timeline
   path. Build the Docker image once a Docker daemon is available. The current
   automated checks validate the application, not a deployed container.
2. Push the reviewed source to the intended repository/branch. Create a Render
   Docker web service on **Free**, using this repository's Dockerfile and port 8080.
   No deployment was performed during this implementation pass.
3. Set Production, the exact assigned hostname in AllowedHosts, Demo__Enabled=true,
   Demo__Email=doctor@reflect.demo, and the agreed demo password through the Render
   environment dashboard. The Development settings file is not used in Production.
   Enable Hosting__HttpsAtEdge only after confirming the TLS/private-origin
   conditions documented above. Keep public self-registration disabled.
4. Configure /health/live. Verify the HTTPS root page, successful seeded login,
   patient assignment, timeline, mobile layout, and logout/replay behavior on the
   actual host. Share the public root URL and credentials with the judges.
5. Test a restart and a cold start. Local database, tickets and seed state are
   disposable; the account is recreated, and reviewers must sign in again. If
   cold-start reliability is unacceptable, use a local presentation plus recording
   as backup rather than implying a free tier guarantees availability.
6. Check free-plan selection and usage/spend settings before publishing. Free
   compute is not a guarantee that every optional resource/overage is free.

Current constraints were checked against [Render's official free-service guide](https://render.com/docs/free)
on October 29, 2026: idle sleep after 15 minutes, about a minute to wake, ephemeral
local files, and shared monthly free-hour limits. Recheck terms when deploying.

Presentation walkthrough: start at `/`, choose Explore the demo, sign in, review
Sam Patel's signal, open Timeline, and expand Source details. Follow-up task editing
is still future work and should not be presented as implemented.
