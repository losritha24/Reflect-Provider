# Reflect provider UI implementation plan

Status: foundations, dashboard composition, session timeline, caseload filter styling, simplified login copy and judge landing page implemented. Task persistence/editing and timeline filters remain planned. October 29, 2026.
The goal is a competition demo where a judge can identify a follow-up signal,
understand its evidence, inspect a patient timeline, and see a coherent provider
workflow in under two minutes. Work remains in ASP.NET MVC/Razor.

## 1. Authoritative design requirements

Source supplied by the user: [Reflect XR Visual Identity Guidelines](https://drive.google.com/file/d/1Kci4USv2WFdDz2ZQXW8yh19SBjDz7mrW/view),
in the [Reflect XR folder](https://drive.google.com/drive/folders/1L_OIT35h1AeS26TOH1QVfunJ__ycf276).
Read through the connected Drive tool. The font-assets folder confirms the primary
and secondary families. This is the visual identity guide; a complete extension
technical specification or competition scoring rubric was not present among this
folder's direct files. Do not claim this review checked those separate requirements.

| Requirement confirmed in guide | Implementation constraint |
| --- | --- |
| Primary treatment: IBM Plex Sans Condensed, Bold, Uppercase | Keep existing local condensed 700 font; use CSS uppercase for primary headings and short CTA labels. Do not replace it with Inter, a serif, or another display family. |
| Secondary treatment: IBM Plex Sans, Regular, Sentence Case | Keep local Sans 400 for body, descriptions and patient names; avoid all-caps paragraphs. |
| Soft coral | `#FF8282` |
| Twilight indigo | `#1F305E` |
| Slate blue | `#586F8A` |
| Cool gray / off white | `#E6E8EB` / `#FAFAFB` |
| Light coral / pale coral | `#F4AA9B` / `#FDE6D9` |
| Brand gradient | Coral → indigo; reserve for deliberate brand moments, not data surfaces. |

Keep the supplied logo assets and proportions. Preserve the existing Provider area,
menu integration and vendor expectations recorded in README. The generic design
skill's preference against uppercase is overridden by the actual Reflect guide.
Existing title-case headings should be reviewed against the uppercase primary type
standard during phase 1; the fonts themselves already load correctly in Chromium.

## 2. Best-fit references researched

“Best” here means a strong fit for this workflow, not a universal ranking or an
independent usability certification. References were public official documentation;
we did not log into these products or test their authenticated interfaces.

| Reference | Pattern to borrow | Adaptation for Reflect |
| --- | --- | --- |
| [Healthie provider dashboard](https://help.gethealthie.com/article/535-your-provider-dashboard) | Prioritize daily work, task ownership and recent client entries. | Make follow-up review the dominant action. Keep operational work separate from aggregate analytics. Do not add billing/scheduling solely to resemble Healthie. |
| [Canvas Medical patient timeline](https://help.canvasmedical.com/articles/4791996458-filter-notes-in-patient-timeline) | Filter a longitudinal record by type, author and period. | One dated stream of sessions, reflections and eventual follow-up events, with visible source and timezone. |
| [IBM Carbon data tables](https://www.carbondesignsystem.com/building-blocks/core/components/data-table/guidelines) | Coherent search/filter toolbar, deliberate row density, sorting, expandable detail and pagination. | Use these interaction patterns in Razor tables; do not import Carbon React or its branding. |
| [IBM Plex in Carbon](https://github.com/carbon-design-system/carbon/blob/main/docs/guides/ibm-plex.md) | Mature use of the same font family. | A useful typography reference that respects our required font rather than changing it. |
| [NHS typography guidance](https://service-manual.nhs.uk/design-system/styles/typography) | Clear hierarchy and legible clinical-service text. | Borrow hierarchy/readability principles only. NHS fonts and brand rules do not apply to Reflect. |

Chosen direction: a calm provider worklist, with a recognizable indigo navigation
rail and coral used to guide action. Patient names and evidence lead; decorative
metric cards and marketing copy recede. Left-align operational information.

## 3. Evidence from the current browser render

Screenshots: [desktop](artifacts/dashboard-desktop.png),
[dark](artifacts/dashboard-dark.png), [390px mobile](artifacts/dashboard-mobile.png).

- Mobile header, banner and breadcrumb consume substantial vertical space before
  the first follow-up. Move account/preferences into a compact menu; retain a short
  “Synthetic demo” label and put architecture explanations in About/demo help.
- The attention card spends more space on its explanation than the actual patient.
  Put the patient, change, last activity and Review action first; disclose the
  calculation in a keyboard-accessible “Why this signal?” section.
- Default blue anchors are hard to read in dark mode. Nominal `#0000EE` on `#243566`
  is approximately 1.26:1. Establish explicit link tokens for both themes.
- White on coral is approximately 2.40:1. Indigo on the same coral is approximately
  5.34:1: preserve the brand coral and change foreground contrast rather than
  inventing a new primary color. These are color-pair calculations, not an audit
  of every rendered pixel/background.
- Mobile viewport itself does not overflow, but the recent-session table requires
  internal horizontal navigation. Show patient/date/platform first, move optional
  details into expansion, and retain an accessible full table view.
- Existing date presentations mix UTC and ET. Add one visible display-timezone
  context, with exact UTC/source timestamps in details. Keep comparison boundaries
  explicit and consistent across list, dashboard and patient screens.

## 4. Tokens and layout contract

Reuse the palette above. Add semantic tokens rather than hardcoding per-view colors:
`--link-color`, `--action-text`, `--surface`, `--surface-raised`, `--focus-ring`,
`--text-secondary`, `--signal-attention`. All themes need independently checked
foreground/background pairs. Do not encode “diagnosis” in a signal color.

Proposed sizes (design decisions, not claimed requirements from the PDF): body
16px/24px; secondary text 14px/20px; page title 28–32px; section heading 20–24px.
Use rem units and the existing font preferences. Body/clinical text stays readable
at 200% zoom. Suggested spacing scale: 4, 8, 12, 16, 24, 32px; one restrained radius
scale. Target 44px controls on touch surfaces. No new font/CDN dependency.

Desktop option chosen: worklist occupies the main column, a narrow side panel
contains recent activity/context. On small screens, one ordered content column.
An alternative grid of equally weighted analytics cards was rejected because it
would bury the provider's next action and repeat the current weak hierarchy.

```text
[Reflect rail] [PROVIDER DASHBOARD] [Synthetic demo] [Account]
               [Period / timezone] [Freshness]
               +--------------------------------+---------------------+
               | NEEDS ATTENTION                | RECENT ACTIVITY     |
               | Patient | Change | Last active | Patient · event     |
               | Why this signal?       Review  | View session        |
               +--------------------------------+---------------------+
               [Compact caseload/session totals, with comparison]

Patient page:
[Back to caseload] [PATIENT NAME] [Synthetic record]
[Overview] [Timeline] [Follow-ups]
[Period / event-type filters]
[Date] [Session + duration/platform] [Source / reflection details]
[Date] [Provider follow-up event]   [Owner / status / audit context]
```

## 5. Sequenced implementation

| Phase | Concrete changes and likely files | Acceptance criteria |
| --- | --- | --- |
| 1 — Brand/accessibility foundation | `reflect-theme.css`, Provider `_Layout.cshtml`: semantic color/type tokens, explicit link colors, contrast-safe coral buttons, uppercase primary headings, compact mobile chrome. | Required fonts and brand colors retained; visible keyboard focus; contrast verified in light/dark; 390px/768px/1440px screenshots; 200% zoom remains usable. |
| 2 — Dashboard hierarchy | `Home/Dashboard.cshtml` and dashboard view model: compact attention worklist, disclosure of signal calculation, consistent timestamps, recent activity beside the queue. | First follow-up action visible without navigating past marketing/architecture copy; same counts and exact comparison windows as the tested backend; empty/no-signal state works. |
| 3 — Caseload and patient detail | `Patients/Index.cshtml`, `Details.cshtml`, patient view models: aligned search/filter/reset toolbar, actionable pagination, responsive detail rows, chronological session/reflection timeline, useful 404 page. | Keyboard-only search → patient → timeline → back path; filter state retained through navigation; no clipping of clinical text; no cross-provider data loading. |
| 4 — Explainable trends | Shared signal-calculation service used by list/detail/dashboard; small period-comparison graphic and equivalent table. | Boundary/timezone tests; charts expose exact values in a table; seeded labels no longer conflict with computed signals; reduced motion honored. |
| 5 — Demo follow-up workflow | Task model/repository, scoped POST actions, audit events, assign/complete/dismiss states. Add reset only after scoped persistence is designed. | CSRF, ownership and audit tests precede UI enablement; visible synthetic labeling; judges can complete a reversible example action. No fake Save buttons or implied clinical integration. |
| 6 — Presentation verification | Browser regression checks, axe accessibility checks, human keyboard/screen-reader pass, container/host smoke. | No serious/critical automated accessibility findings; manual checks recorded; screenshots approved; cold start and backup demo rehearsed. |

Start with phases 1–3 for the competition. Phases 4–5 depend on backend work and
must not be represented as finished capabilities. Hosting/public deployment is a
separate operation and is not part of this plan's execution.

## 6. Handoff between agents

Both agents can read `skills/vendor/frontend-design/SKILL.md` and
`skills/vendor/webapp-testing/SKILL.md`. This plan and the linked Reflect guideline
are the project brief and take precedence over generic style suggestions. Keep
security work and UI work in separate file scopes/worktrees when another agent is
active; coordinate changes to Program.cs, repositories and shared tests explicitly.

## Next implementation batches — dashboard research update

Reviewed official product documentation again on October 29, 2026. These are
workflow references, not claims that we tested their logged-in products.

| Application | Useful pattern | Reflect implementation |
| --- | --- | --- |
| [Healthie](https://help.gethealthie.com/article/535-your-provider-dashboard) | Work-oriented overview with tasks and a separate client-entry feed | Main column: follow-up worklist. Secondary column: recent sessions. Put analytics below the immediate work. |
| [Canvas Medical](https://help.canvasmedical.com/articles/4791996458-filter-notes-in-patient-timeline) | Longitudinal chart filtered by event type, author and date | Patient header plus a dated session/reflection timeline; add follow-up events when the task backend exists. |
| [SimplePractice](https://support.simplepractice.com/hc/en-us/articles/41983181128205-Using-your-Analytics-dashboard) | Compact practice-pulse metrics with a selected time period | A restrained summary strip for caseload, recent activity and platform mix. Do not import financial metrics irrelevant to Reflect. |

Recommended build order, one reviewable batch at a time:

1. **Dashboard composition:** compact follow-up rows with patient, last activity,
   previous/current counts and Review action; disclosure for the calculation.
   Two columns on desktop, worklist first on mobile. Keep data and authorization
   logic intact. Verify empty state and first action visibility at 390px.
2. **Patient story:** timeline using existing session/reflection data, useful date
   filters, readable expanded details and a clear return to the filtered caseload.
   Keep timestamps and signal definitions consistent. No invented clinical scores.
3. **Judge entry page and login:** a short public introduction with an authentic
   dashboard screenshot, three concrete benefits and an Explore demo link to login.
   Reduce the login slogan and prioritize the form. Keep Create account for local
   Development; explain that self-created accounts start with an empty caseload.
4. **Follow-up action:** implement scoped task persistence/audit first, then expose
   create/complete/dismiss controls. Finish the story with an observable completed
   action, not a decorative button.

Judge journey: introduction → sign in → inspect a falling-engagement signal →
review its session evidence → record a follow-up (once batch 4 is implemented).
Maintain IBM Plex Sans Condensed Bold uppercase primary text, IBM Plex Sans regular
body copy, and the approved Reflect coral/indigo palette throughout. Reuse current
Razor layouts; no framework rewrite, extra font package, or animation library.

The requested local demo account is configured in `appsettings.Development.json`.
It is a deliberately public synthetic-demo credential, not a production secret.
Hosted demo credentials remain an explicit environment/secret configuration.

## Implemented UI batch

- Public judge introduction with a clearly labeled illustrative sample and login CTA.
- Compact dashboard worklist, expandable signal explanation, accessible platform
  meters/data table, and responsive activity feed.
- Chronological session/reflection timeline with exact source timestamps and a
  table alternative; existing keyboard tab behavior retained.
- Styled caseload search/filter toolbar and shorter login story.
- Required local IBM Plex fonts and Reflect palette retained.

Verification: 27 C# checks and HTTP/Chromium checks passed; desktop/mobile/landing
screenshots were inspected. This is not a full screen-reader or accessibility audit.
Source metadata describes the synthetic seed honestly; no live synchronization or
follow-up task persistence is implied. Hosting remains a plan, not a deployment.
