# Reflect Provider Dashboard — design direction

Status: proposed visual direction, revised November 6, 2026 after a code and
brand review. This document applies the existing Reflect XR visual identity to
the provider dashboard. It does not replace the product, security or
accessibility requirements in [NEXT-STEPS.md](NEXT-STEPS.md),
[TODO.md](TODO.md), or [UI-IMPLEMENTATION-PLAN.md](UI-IMPLEMENTATION-PLAN.md).
If this document conflicts with the supplied Reflect XR visual identity guide or
logo assets, the guide and assets win.

Review note: the first draft was checked against the live UI and the extracted
brand guide. This revision separates guide rules from project interpretation,
moves type and spacing to rem so the in-app Text size control works, resolves the
button and focus contradictions, preserves tested element IDs, and narrows the
first PR to CSS-only changes. See §14 for accepted and open review items.

The goal is a provider workspace that looks deliberately designed rather than
template-styled: calm, precise, readable, and clearly Reflect. The current UI is
coherent and accessible, but it relies on a card stack, pill-shaped controls,
uppercase labels at every level, and marketing copy inside the product. The fix
is composition and restraint, not a rebrand.

## 1. Brand rules and project interpretation

The extracted guide text contains the palette, the gradient stops, three
taglines, and three type treatments. Detailed logo, button, nav and table rules
may live in images the text extraction missed. The supplied assets and the guide
remain authoritative. The tables below separate confirmed guide content from
decisions this document adds.

### 1.1 From the guide

| Requirement | Rule |
| --- | --- |
| Logo | Use the supplied SVG assets and proportions. Never recolor, outline, stretch or rebuild the mark. Exact clear-space rules were not confirmed in the extracted text; follow the assets and get brand sign-off for edge cases. |
| Primary type | IBM Plex Sans Condensed, Bold (700), uppercase. |
| Secondary type | IBM Plex Sans, Regular (400), sentence case. |
| Soft coral | `#FF8282` |
| Twilight indigo | `#1F305E` |
| Slate blue | `#586F8A` |
| Cool gray | `#E6E8EB` |
| Off-white | `#FAFAFB` |
| Light coral | `#F4AA9B` |
| Pale coral | `#FDE6D9` |
| Brand gradient | Coral `#FF8282` to indigo `#1F305E`. The guide gives the stops, not a usage rule. The current CSS defines `--brand-gradient` but does not use it. |

### 1.2 Project interpretation (needs sign-off where noted)

| Decision | Rationale / status |
| --- | --- |
| Condensed uppercase is reserved for page titles, section headings, the primary CTA, and at most one eyebrow per page. | Prevents the current all-caps flattening. The guide does not set this scope; treat it as the project's hierarchy rule. |
| The primary CTA uses Condensed Bold uppercase; secondary and tertiary controls use Sans Regular sentence case. | Resolves the first draft's contradiction. The guide supports short uppercase CTA labels and specifies Regular for the secondary voice. |
| Plex Sans Medium (500) and Condensed SemiBold (600) are not used unless the brand owner approves them. | The guide lists only Regular 400 and Bold 700. The repo ships Medium and SemiBold files, but availability is not a brand rule. Default to 400/700. |
| The gradient is optional and, if used, only for a landing/sign-in brand moment. | The guide gives no usage rule; the CSS currently leaves it unused. Never place it behind data, tables or clinical content. |
| No white text on coral; use indigo on coral. | Contrast: white on `#FF8282` is about 2.4:1; indigo is about 5.3:1. This is a contrast decision, not a guide rule. |
| No green/amber/red status palette. `#8C3A3A` is allowed only for validation text. | Functional exception for error semantics; it is not a brand color. Remove the off-palette `--warning-rgb` value (`232,163,61`). |
| No new UI framework, font package, CDN, icon pack or animation library. | Keeps the existing Razor/CSS stack and the local brand assets. |

The important interpretation: the condensed uppercase treatment is the
**primary** voice, not the only voice. Using it on every button, badge, nav item
and table header destroys hierarchy and makes the product shout. Body copy,
patient names and secondary controls stay sentence case.

## 2. Design position: calm clinical editorial

Three words: **quiet, precise, human**.

- **Quiet** — flat surfaces, hairline rules, generous but controlled spacing,
  one accent. No decorative shadows, glass, gradients or entrance animations in
  the authenticated product.
- **Precise** — a strict grid, aligned numbers, compact tables, functional
  headings. A provider should see the next action without reading a paragraph.
- **Human** — the patient is the largest thing in a row; the language is plain;
  the brand's warmth comes through in coral accents and the landing page, not in
  decoration layered over clinical work.

The product should feel like a well-edited clinical journal or a mature
operations tool, not a marketing dashboard. The landing and login pages can carry
more atmosphere; the workspace should be restrained.

## 3. What is wrong now

These are the specific patterns to remove, measured at 1440×900:

1. **Card stack instead of a work surface.** The dashboard is roughly 2,753px
   tall for five patients and eight sessions. The first "Review patient" action
   sits around 886px down — below the fold at 900px, and at 883px on a
   1366×768 laptop. "Needs attention," "One connected view" and "Recent
   activity" are three competing panels. The page has three eyebrows.
2. **Pill-shaped everything.** 37 of 40 controls are 999px capsules with
   letterspaced uppercase text. Every action has the same visual weight.
3. **All-caps everywhere.** Condensed uppercase is used on nav, badges, table
   headers, filters, buttons and eyebrows. The brand guide reserves it for
   primary display; using it everywhere flattens the hierarchy.
4. **Marketing copy inside the product.** "Care between sessions," "Remember
   who it's for," "One connected view" and "The story between sessions" belong on
   the landing page. In the workspace they make the user translate poetry into a
   task.
5. **Too much chrome.** Sidebar brand, header kicker, "Provider workspace,"
   demo banner, breadcrumb, page eyebrow and legal fineprint appear before the
   content. The sidebar is 264px. On mobile the only visible logo is the chat
   launcher; the header leaves "Menu" and "Account & display" above a two-line
   demo banner and breadcrumb.
6. **Coral overload.** Coral is the primary action, active nav, attention badge,
   timeline dot, chart deviation and brand accent. When everything is coral,
   nothing is.
7. **Generic kit gestures.** One-rem rounded cards, some with soft shadows,
   initial avatars, progress bars, a split-screen login with a decorative ring, a
   three-column feature grid and a hand-built fake dashboard card on the landing
   page. Each is harmless alone; together they read as a template. The work
   panels themselves are flat with a 16px radius; the shadow belongs to
   `.custom-card`.
8. **Mobile table collapse.** At 390px and 200% text, the caseload table is
   617px wide inside a 356px container. The page scrolls sideways, shows one
   column, and produces a large empty card and "1 patients found."

## 4. Design tokens

Implement these in
`Reflect.ProviderDashboard.Web/wwwroot/standins/css/reflect-theme.css` and
`provider-workspace.css`.

**Units:** all type, spacing, control heights, gaps and radii are specified in
`rem`. The in-app Text size control sets
`document.documentElement.style.fontSize`; a `px` value would ignore both that
control and browser zoom. Values in parentheses are the 16px-root reference
only, not implementation values. Borders may stay at `1px`.

**Naming:** introduce the semantic names as aliases to existing tokens; do not
mass-rename the current CSS. Example:

```css
--canvas: #F2F3F5;
--page-bg: var(--canvas);
--surface: var(--off-white);
--surface-raised: #FFFFFF;
--line: var(--default-border);
--ink: var(--default-text-color);
--muted: var(--text-muted);
--subtle: var(--slate-blue);
--accent: var(--soft-coral);
--accent-hover: var(--light-coral);
--accent-soft: var(--pale-coral);
--accent-ink: var(--twilight-indigo);
--focus: var(--slate-blue);
```

### 4.1 Color roles

Use existing brand colors; do not add new hues. Tints and shades are allowed for
surfaces and dark mode.

| Token | Light | Role |
| --- | --- | --- |
| `--canvas` | `#F2F3F5` | Page background, a neutral between cool gray and off-white |
| `--surface` | `#FAFAFB` | Panels, tables, form surfaces |
| `--surface-raised` | `#FFFFFF` | Menus, dialogs and popovers only |
| `--line` | `#D8DCE1` | Hairline borders and table rules |
| `--line-strong` | `#C3C9D1` | Selected or emphasized rules |
| `--ink` | `#1F305E` | Primary text, headings, sidebar |
| `--muted` | `#4B6079` | Secondary text, captions |
| `--subtle` | `#586F8A` | Icons, tertiary text, chart axes |
| `--accent` | `#FF8282` | Primary CTA fill, attention marker, `0.125rem` active-nav indicator |
| `--accent-hover` | `#F4AA9B` | Primary CTA hover |
| `--accent-soft` | `#FDE6D9` | Attention row or panel wash |
| `--accent-ink` | `#1F305E` | Text on coral |
| `--focus` | `#586F8A` | Focus ring on light surfaces |
| `--danger` | `#8C3A3A` | Validation text only; functional exception, not a brand color |

Dark mode:

| Token | Dark |
| --- | --- |
| `--canvas` | `#1F305E` |
| `--surface` | `#243566` |
| `--surface-raised` | `#2B3D70` |
| `--line` | `rgba(255,255,255,0.14)` |
| `--ink` | `#FAFAFB` |
| `--muted` | `#C9D1DE` |
| `--subtle` | `#AEB9C9` |
| `--accent` | `#FF8282` |
| `--accent-soft` | `rgba(255,130,130,0.14)` |
| `--accent-ink` | `#1F305E` |
| `--focus` | `#FF8282` |

Rules:

- Coral marks the **primary action** and **attention**. It may appear as a
  `0.125rem` active-nav indicator, not as a solid nav fill.
- Active navigation is a pale coral tint with a `0.125rem` coral left bar and
  indigo text. It is not a solid coral pill.
- Status is never conveyed by color alone; every badge has text.
- Do not use white text on coral. Use indigo on coral (approximately 5.3:1).
- Do not introduce green/amber/red status palettes. Use indigo and slate for
  neutral states, pale coral for attention, and the functional danger color only
  for validation text.
- Remove or remap the off-palette `--warning-rgb` value (`232,163,61`); it is not
  a brand color and should not survive the token pass.

### 4.2 Type scale

| Role | Family / weight | Size / line height | Case and spacing |
| --- | --- | --- | --- |
| Page title (h1) | Plex Sans Condensed 700 | `2.25rem` / 1.1 desktop; `1.875rem` / 1.1 mobile | Uppercase, `0.01em` |
| Section heading (h2) | Plex Sans Condensed 700 | `1.5rem` / 1.2 | Uppercase, `0.01em` |
| Row heading (h3) | Plex Sans 400 | `1.125rem` / 1.3 | Sentence case, `0` |
| Body | Plex Sans 400 | `1rem` / 1.6 | Sentence case |
| Secondary body | Plex Sans 400 | `0.875rem` / 1.5 | Sentence case |
| Caption / meta | Plex Sans 400 | `0.75rem` / 1.4 | Sentence case |
| Label | Plex Sans 400 | `0.8125rem` / 1.4 | Sentence case |
| Primary CTA | Plex Sans Condensed 700 | `0.875rem` / 1 | Uppercase, `0.04em` |
| Secondary/tertiary button | Plex Sans 400 | `0.875rem` / 1 | Sentence case, `0` |
| Table header | Plex Sans 400 | `0.75rem` / 1.4 | Sentence case, `0.01em` |
| Eyebrow | Plex Sans Condensed 700 | `0.75rem` / 1.4 | Uppercase, `0.12em`; maximum one per page |

Rules:

- The primary CTA is the only button that uses uppercase condensed type. Keep
  its label short: "Review," "Apply filters," "Sign in."
- Secondary and tertiary buttons, nav, badges, filters and table headers are
  sentence case. They never use uppercase.
- Plex Sans Medium (500) and Condensed SemiBold (600) are not in the guide's
  listed treatments. Use Regular 400 and Bold 700. If the brand owner approves
  Medium or SemiBold, document the exception.
- Page titles are the only very large type. Do not use display sizes on cards.
- No letterspacing on body or controls.
- Use `font-variant-numeric: tabular-nums` for counts, dates and metrics.
- Body measure: 65–75 characters. Do not stretch clinical copy across the full
  viewport.
- At 200% text, nothing clips or overlaps. Prefer wrapping over truncation.

### 4.3 Spacing and grid

- Base unit: `0.25rem` (4px at a 16px root).
- Scale: `0.25`, `0.5`, `0.75`, `1`, `1.5`, `2`, `3`, `4rem`.
- Content max width: `80rem` (1280px at a 16px root).
- Page padding: `2rem` desktop, `1.5rem` tablet, `1rem` mobile.
- Sidebar: `14rem` (224px) fixed at `64rem` and above; off-canvas below. This is
  a deliberate reduction from the current `16.5rem` (264px); verify labels fit.
- Header: `4rem` desktop, `3.5rem` mobile.
- Dashboard split: 8/4 columns with a `1.5rem` gutter.
- Patient detail split: 8/4 columns with a `1.5rem` gutter.
- Section gap: `2rem`. Panel padding: `1.5rem`. Table row height: `3.5–4rem`.

### 4.4 Shape, border and elevation

- Controls: `0.375rem` radius.
- Panels and table containers: `0.625rem` radius.
- The table itself and its rows: 0 radius. Do not round individual rows.
- Chips and status badges only: `999px`.
- Borders: `1px` `--line`. Attention rows add a `0.1875rem` coral left border.
  That border is decorative (about 2:1 against pale coral); the row's text and
  badge must carry the meaning.
- Shadows: none on work panels or cards. The existing `.custom-card` shadow
  should be removed in the token pass. Use
  `0 1rem 2.5rem rgba(31,48,94,0.18)` only for menus, dialogs and popovers.
- No hover lift, no transform on cards, no gradient borders.

### 4.5 Motion

- `140ms` ease for color, border and opacity changes.
- No entrance animations, parallax, floating elements or autoplay.
- Honor `prefers-reduced-motion: reduce`.

### 4.6 Breakpoints

| Name | Width | Behavior |
| --- | --- | --- |
| Desktop | ≥`80rem` (1280px) | Full sidebar, 8/4 splits, tables |
| Compact | `64–79.99rem` (1024–1279px) | Narrower sidebar, 8/4 splits, tighter padding |
| Tablet | `48–63.99rem` (768–1023px) | Off-canvas sidebar, stacked rails, tables |
| Mobile | <`48rem` (768px) | Single column, list rows instead of primary tables |
| Small | <`30rem` (480px) | `1rem` padding, `3.5rem` header, compact controls |

## 5. App shell

```text
┌──────────────┬────────────────────────────────────────────────────────┐
│ REFLECT XR   │ [‹] Patients / Sam Patel            [Demo]           │ 4rem
│ Provider     │                                        A. Rivera ▾    │
├──────────────┼────────────────────────────────────────────────────────┤
│ WORKSPACE    │                                                        │
│ ▣ Overview   │  Page content, max 80rem, 2rem padding                 │
│ ◇ Caseload map│                                                       │
│ ? Privacy & trust│                                                    │
│ CASELOAD     │                                                        │
│ ○ Patients   │                                                        │
│ □ Devices    │                                                        │
│              │                                                        │
│ ? Help       │                                                        │
└──────────────┴────────────────────────────────────────────────────────┘
```

### Sidebar

- `14rem` (224px), indigo background, off-white text. This is a deliberate
  reduction from the current `16.5rem`; check the longest label before shipping.
- Logo mark plus "REFLECT XR" wordmark at the top. "Provider" is a `0.75rem`
  label beneath, not a second brand lockup.
- Nav groups: "Workspace" (Overview, Caseload map, Privacy & trust), "Caseload"
  (Patients, Devices). Do not omit Privacy & trust; it already exists in the
  app.
- Each item: `1.125rem` line icon, `0.875rem` sentence-case label, `2.75rem`
  height, `0.5rem` gap.
- Icons are optional for the first pass. Hand-drawn inline SVG only; no icon
  pack. If icons are omitted, the collapsed rail keeps the existing `data-short`
  initials so targets are not blank.
- Active: pale coral at 12% opacity, `0.125rem` coral left bar, indigo text. In
  dark mode, use a light coral tint and off-white text.
- Hover: white at 8% opacity; no movement.
- Footer: "Demo" and "Help" text links at `0.75rem`. Move "Take the tour" here or
  into the account menu. Remove the corporate "Persistent Technology · 2026"
  line or reduce it to `0.75rem`.

### Header

- `4rem`, `--surface`, 1px bottom border.
- Left: sidebar collapse chevron (icon button, `2.75rem` square) and a compact
  context: "Patients / Sam Patel" in `0.8125rem` muted, or a back link on detail
  pages. Do not use an uppercase breadcrumb bar.
- Right: "Demo · synthetic data" chip and an account menu showing the provider
  name and a chevron. The assistant opens from the floating logo bubble; do not
  add a second "Assistant" button in the header.
- Account menu: `0.75rem` panel radius, raised surface, 1px border, `0.5rem`
  padding, full-width menu items for Accessibility, Theme, and Sign out.
- Remove the global demo banner, the "Immersive Arts for Wellness" kicker, the
  duplicate "Provider workspace" label, and the "Collapse navigation" pill.
- Mobile: `3.5rem`; menu icon, `1.75rem` logo mark, demo chip, account
  avatar/initials. Keep the logo visible.
- Demo chip on mobile: at `<30rem`, show a compact "Demo" chip in the header with
  `aria-label="Demo, synthetic data"`. If it cannot fit beside the logo and
  account, use a single `1.75rem` strip directly under the header. Never hide the
  demo label on small screens.

**Preserve these test hooks** in Phase 1: `a11yPanel`, `a11yPanelToggle`,
`a11yPanelClose`, `a11yBackdrop`, `mobileMenuToggle`, `sidebarCollapseToggle`,
`themeToggle`, `tourOpen`, and `.account-menu > summary`. `browser_checks.py`
and `layoutController.test.cjs` depend on them.

### Content

- `main` starts `2rem` below the header; no breadcrumb bar.
- Add `6rem` bottom padding so the floating chat launcher never covers the last
  row or primary action.
- Page header: optional eyebrow (one), h1, one summary line, optional primary
  action on the right. No marketing sentence under the title.
- Legal fineprint: `0.75rem`/1.5 muted, in the footer, max 80ch. Keep the
  synthetic data and "not a HIPAA-covered production system" language.
- One persistent demo chip is enough. Keep "not a diagnosis" near signals and
  keep the synthetic-data statement on the landing, sign-in and Trust pages.

## 6. Components

### Buttons

| Variant | Spec | Use |
| --- | --- | --- |
| Primary | Coral fill, indigo text, `2.75rem` height, `0.375rem` radius, `0 1rem`, Plex Condensed 700 uppercase `0.875rem`, `0.04em` | One per view: Apply filters, Review, Sign in, Move device |
| Secondary | Transparent, 1px `--line`, indigo text, `2.75rem`, Plex Sans 400 `0.875rem` sentence case | Back, Cancel, View table |
| Tertiary | Text link, indigo, Plex Sans 400 `0.875rem`, underline offset `0.18em` | Clear filters, View all activity, Why this signal? |
| Icon | `2.75rem` square, `0.375rem` radius, transparent, `1.125rem` line icon | Collapse, menu, close, account |

- Hover: primary → light coral; secondary → pale coral wash; tertiary → darker
  indigo and thicker underline.
- Focus: `0.1875rem` `--focus` ring with `0.125rem` offset. Use slate in light
  mode and coral in dark mode.
- Disabled: 40% opacity, `cursor: not-allowed`, no hover change.
- Do not use pill-shaped buttons. Only the primary CTA uses uppercase; secondary
  and tertiary controls never do.
- Do not show a decorative primary button for an action that is not implemented.

### Links

- Body links: indigo, underlined, `text-underline-offset: 0.18em`.
- Navigation links: no underline until hover/focus.
- Visited links keep the same color.
- Do not use default browser blue.

### Badges and chips

- `999px` radius, `0.75rem` Plex Sans 400, sentence case, `0.25rem 0.625rem`
  padding.
- Neutral: cool gray background, indigo text.
- Attention: pale coral background, indigo text.
- Platform: neutral background with a `0.375rem` slate dot; no rainbow of colors.
- Demo chip: pale coral background, indigo text, `0.75rem`, prefixed with a small
  dot.
- No uppercase badges. No color-only status.

### Panels and tables

- Panel: `--surface`, 1px `--line`, `0.625rem` radius, `1.5rem` padding, no
  shadow.
- Panel header: `1rem` bottom border, h2 `1.25–1.5rem` uppercase, optional
  tertiary link on the right.
- Table container: `0.625rem` radius. Table and rows: 0 radius.
- Table: full width, 1px header and row rules, `0.75rem` sentence-case header,
  `3.5–4rem` rows, `1rem` cell padding.
- Hover: pale coral at 40% opacity.
- Attention row: pale coral at 60%, `0.1875rem` coral left border. The border is
  decorative; the badge and text carry the status.
- Numeric columns: right-aligned, tabular numerals.
- Do not use `text-nowrap` on primary patient tables. Allow clinical text to
  wrap. A full data table may scroll horizontally only inside a disclosure.

### Metric strip

- One row, not four cards.
- `1.5rem` number in Plex Sans Condensed 700, `0.75rem` label in Plex Sans 400.
- `1.5rem` gap, 1px vertical rules between metrics.
- Example: `12 sessions | 5 XR | 7 Mobile | 340 min`.
- On mobile, 2×2 grid with `1rem` gaps; labels may wrap.

### Forms

- Label above input, `0.8125rem` Plex Sans 400, `0.5rem` gap.
- Input/select: `2.75rem`, `0.375rem` radius, 1px `--line`, `--surface`
  background, `1rem` text.
- Focus: `0.125rem` `--focus` (slate) border plus a `0.1875rem` translucent
  slate ring. Do not use a coral focus ring on off-white; it is about 2.3:1.
- Error: `0.8125rem` danger text with an icon, placed below the field; do not
  rely on color alone.
- Help text: `0.8125rem` muted, `0.25rem` gap.
- Form gap: `1.25rem`. Group related fields in a fieldset with a `1rem` legend.
- Filter toolbar: inline row on desktop, stacked on mobile; no surrounding card.
  Search `17.5rem`, select `11.25rem`, Apply primary, Clear tertiary.

### Tabs

- `2.75rem` height, `0.875rem` sentence case, `0.125rem` transparent bottom
  border.
- Active: indigo text, `0.125rem` indigo bottom border. Do not use coral for the
  active underline; coral on off-white is about 2.3:1, below the 3:1 non-text
  contrast target.
- Hover: pale coral wash.
- Keyboard: Left/Right/Home/End; `aria-selected`; roving tabindex.
- Do not style tabs as pills.

### Disclosure

- Summary `0.875rem` Plex Sans 400, `2.75rem` target, chevron at the right.
- Content `1rem`/1.6, `0.75rem` top padding.
- Use for "Why this signal?", "View data table", "Source details" and
  "Assignment history." Do not wrap every disclosure in another card.

### Empty states

- Left-aligned, `1.5rem` padding, one sentence, one link.
- No illustration, no large icon, no marketing copy.
- Example: "No patients match these filters. Clear filters."

### Pagination

- "Showing 1–10 of 14" on the left; Previous and Next as secondary buttons or
  text links on the right.
- Disabled state at 40% opacity.
- Do not repeat "Page 1 of 1" twice. Pluralize correctly.

### Assistant

- Launcher: use the supplied white logo mark unmodified as the floating bubble,
  matching the current choice. Do not rebuild, recolor or crop it. Keep it out of
  the way of primary actions and add `6rem` bottom padding to `main`.
- Panel: `22.5rem`, `0.75rem` radius, raised surface, 1px border, `1rem` padding.
- Header: "Assistant" `1rem` Plex Sans 400, "Prototype · not connected to a model"
  `0.75rem` muted.
- Privacy line `0.75rem`. Chips `0.8125rem`, neutral background. Input `2.75rem`.
- Keep the panel secondary; it must not cover the primary action on mobile.

### Tour dialog

- Max width `30rem`, `1rem` padding, `0.75rem` radius, raised surface.
- h2 `1.25rem` uppercase; body `1rem`; step `0.75rem` muted.
- Buttons: Skip/Back secondary, Next primary. Focus trap and restore.

## 7. Page specifications

### 7.1 Landing

```text
REFLECT XR                         How it works   Sign in

CARE DOESN'T END                         [real dashboard screenshot]
AT THE SESSION.                          [one coral annotation: 1 → 0]

Bring Reflect XR and Mobile activity into one provider workspace...

[Explore the demo]   [See how it works →]
A working prototype. Every patient record is fictional.

──────────────────────────────────────────────────────────────
01 One worklist          02 Every signal           03 Every record
   XR + mobile              links to its sessions     keeps its source and time zone
──────────────────────────────────────────────────────────────
How it works: Notice → Understand → Act
Footer: logo · Competition demo · legal fineprint
```

- Header: logo left; "How it works" and "Sign in" as text links right. One
  primary CTA is enough; the header link is secondary.
- Hero: 7/5 split. H1 `3.5–4rem` desktop, `2.5rem` mobile, max 11ch, uppercase
  condensed. Deck `1.125rem`/1.6, max 48ch.
- Visual: a real cropped screenshot of the dashboard in a simple 1px frame, not
  a hand-built fake browser card. Add one coral annotation calling out the
  previous → current signal. Caption: "Illustrative synthetic data."
- Proof points: three specific outcomes, separated by rules, not cards. Avoid
  generic "Notice / Understand / Connect" labels. The third proof point is
  "Every record keeps its source and time zone" until the follow-up workflow
  ships; then replace it with "A follow-up you can close."
- How it works: three numbered steps with a rule. No cards.
- Do not add a "Watch the 2-minute walkthrough" link until a video exists.
- Footer: logo, "Reflect Provider · Competition demo", legal fineprint
  `0.75rem`.

### 7.2 Sign-in

- Layout: 5/7 split at desktop. Left: indigo panel with the horizontal logo, one
  sentence ("See what changed between sessions."), and one small screenshot
  fragment or a single data detail. Right: the form, max `22.5rem`, vertically
  centered.
- H1: "Provider sign in" (not "Doctor sign in"). `2rem` uppercase condensed.
- Optional eyebrow: "Welcome back" — one only.
- Intro: `0.875rem` muted, one sentence.
- Fields: email, password, remember me. Primary button full width.
- Demo note: one line or a single small block: "Demo access: doctor@reflect.demo
  · password supplied by the presenter." Do not stack a separate callout card.
- Security note: `0.75rem` muted. "Forgot your password? Contact your
  administrator."
- Create account: Development only, tertiary link.
- No decorative ring, no oversized headline, no second card.

### 7.3 Dashboard

```text
OVERVIEW                                   Fri, Nov 6 · 5 patients · 8 sessions · 1 review
[How signals work]

┌──────────────────────────────────────────────┬─────────────────────────────┐
│ NEEDS ATTENTION                              │ SESSIONS BY PLATFORM        │
│ Patient       Last      Change      Action    │ XR 4   Mobile 4   ▇▇▇▇     │
│ Sam Patel     10/17     1 → 0       Review    │                             │
│                         Fewer sessions       │ RECENT SESSIONS             │
│                                              │ Alex · Mandala · 10/28      │
│                                              │ Taylor · Grounding · 10/28  │
│                                              │ ...                         │
│                                              │ View all activity →         │
└──────────────────────────────────────────────┴─────────────────────────────┘
```

- Page title: "Overview," matching the sidebar category. The dashboard compares
  7- and 14-day windows, so "Today" would be inaccurate. One summary line with
  counts and the current date. No eyebrow, no marketing sentence.
- Do not add Engagement/Affect tabs. Use one worklist with a "Change" column.
  Today only the engagement rows are available; if affect flags cannot be merged
  without a view-model change, render them as a compact secondary list below the
  worklist. Do not create tabs for the visual pass.
- Worklist: 2/3 width. Columns: Patient, Last session, Change, Action. Do not add
  a patient ID column; `AttentionRow` does not carry one, and adding it requires a
  view-model change outside this pass.
- Keep the heading "Needs attention" for now. `browser_checks.py:51` and
  `hosted_demo_smoke.py:87` assert it. If the copy changes to "Needs review,"
  update both tests in the same PR.
- Row height `4rem`. Attention rows use the pale coral wash and a `0.1875rem`
  coral left border. The "Change" cell shows the previous → current counts and a
  "Fewer sessions" badge.
- Action: "Review" as a secondary button or tertiary link. One primary action per
  page; do not make every row's action a coral pill.
- Rail: 1/3 width. "Sessions by platform" as a single stacked bar with two
  labels and counts. "Recent sessions" with four rows: patient, module, date.
  Tertiary "View all activity." No second card stack.
- Keep the existing caseload-map suggestion, but place it below the worklist or
  in the rail. It must not precede the first Review action.
- Empty state: "No changes need attention." One link to the caseload.
- "How signals work" opens the Trust page or a small dialog. Do not place a large
  explainer paragraph under the table.
- At 1440×900, the summary, worklist and first Review action must be visible
  without scrolling. Target page height ≤1000px for five patients.

### 7.4 Patients

```text
PATIENTS                                            5 in caseload
[Search name or ID........] [All signals ▾] [Apply filters]  Clear filters

┌──────────────────────────────────────────────────────────────────────┐
│ Patient        ID          Sessions   Last session   Platform  Signal│
│ Sam Patel      PT-10063    12         10/17/2026     Mobile    Fewer │
└──────────────────────────────────────────────────────────────────────┘
Showing 1–5 of 5                                      Previous   Next
```

- Header: h1 "Patients", one summary line, no eyebrow.
- Filter toolbar: inline, no card. Search `17.5rem`, select `11.25rem`, primary
  Apply, tertiary Clear.
- Table: no `text-nowrap`; rows `3.5rem`; patient name `1rem`; ID and dates
  `0.875rem`; platform and signal badges `0.75rem`.
- Signal column: use the existing trend labels only. Do not invent "Within
  range" or "Below personal baseline" labels before the shared signal
  calculation exists; an absent flag is not a state. Unifying the dashboard and
  list signal remains a separate TODO, not part of this visual pass.
- Pagination: "Showing 1–5 of 5" plus Previous/Next. Fix pluralization:
  "1 patient found", "2 patients found."
- Mobile (`<48rem`): replace the table with a list: name, last session, signal
  badge, Review link. Put the full table behind a "View table" disclosure. Use
  `display: none` on the hidden version so screen readers do not read it twice.
  No horizontal scrolling for the primary content. At 390px and 200% text,
  everything remains readable and tappable.
- Empty state: "No patients match these filters." + "Clear filters."

### 7.5 Patient detail

```text
← Patients
SAM PATEL                                      PT-10063 · DOB 11/5/2001 · Synthetic
[Overview] [Timeline]

12 sessions        5 XR        7 Mobile        340 min
──────────────────────────────────────────────────────────────────────
┌──────────────────────────────────────────────┬───────────────────────┐
│ EMOTIONAL TRAJECTORY                         │ ABOUT                 │
│ [chart with usual-range band]                │ Clinical notes        │
│ View data table                              │ Devices               │
│                                              │ Manage devices →      │
└──────────────────────────────────────────────┴───────────────────────┘
```

- Back link: "← Patients" text link, not a pill button.
- Header: h1 `2.25rem`, meta line with ID, DOB, and a "Synthetic" tag. Keep the
  existing DOB for this pass. Replacing it with age is a minimum-necessary
  improvement but requires a view-model change; track it separately.
- Primary action: "Add follow-up" only when the workflow exists. Until then, do
  not render a disabled or decorative button.
- Tabs: Overview and Timeline; add Follow-ups only when implemented. Sentence
  case, indigo active underline.
- Metric strip: one inline row, not four cards. `1.5rem` numbers, `0.75rem`
  labels, hairline separators, tabular numerals.
- Main grid: 8/4. Left: trajectory chart. Right: "About" panel with clinical
  notes and assigned devices. No nested cards.
- Chart: `0.125rem` indigo line, shaded usual-range band, coral dots only for
  deviations, `0.75rem` slate axis labels. Include a caption and a "View data
  table" disclosure. Do not use a chart library; keep the existing SVG generator.
- Timeline: single panel, `6rem` date gutter, 1px vertical rule, coral dot for
  events, platform badge, `1.125rem` module heading, `1rem` reflection, source
  details disclosure. Filters inline above the list. No outer card around every
  event.
- Session table: behind a "View session table" disclosure, plain panel, no card
  header. Keep the full table for accessibility.
- Empty states: one sentence and one action.

### 7.6 Trust

- Max width `47.5rem`, left-aligned, editorial.
- Header: h1 "How client data is protected", `1rem` lede, no eyebrow.
- Status row: four inline items with `0.75rem` labels and `0.875rem` values,
  hairline rules.
- Sections: h2 `1.5rem`, `1rem` body, `2rem` section gap. Use rules, not cards.
- Safeguards: a 2-column definition list or table, not a grid of cards. Keep the
  "In place / Production target / Not yet" pills but make them `0.75rem` neutral
  chips.
- "Where data travels": numbered vertical list with a `0.125rem` indigo rule.
- "How data can be brought in": three columns with 1px top rules; no cards.
- Keep the legal and demo language. This page is the place for the fuller
  explanation that the workspace no longer carries.

### 7.7 Devices

- Replace per-device cards with one table: Device, Kind, Assigned to, Action.
- Row action: "Move" opens an inline select + primary button in an expanded row,
  or a small dialog. Keep the assignment history in a disclosure.
- Keep the "assignment is a relationship, not ownership" explanation as one
  paragraph, not a card.

### 7.8 Caseload map

- Keep as a secondary page. It can be a deliberate brand moment: indigo edges,
  pale coral patient nodes, coral alert rings, `0.75rem` labels, legend, table
  alternative.
- Keep the existing dashboard suggestion entry point, but place it below the
  worklist or in the rail, never above the first action.
- Ensure the SVG has a text alternative and the table is complete.

## 8. Copy rules

- Use functional page and section headings: "Overview", "Needs attention",
  "Patients", "Session timeline", "Sessions by platform", "Recent sessions."
- Save "Care doesn't end at the session" and "Remember who it's for" for the
  landing and sign-in pages.
- The primary CTA may be uppercase condensed and short: "Review", "Apply
  filters", "Sign in." Secondary and tertiary controls are sentence case and
  verb-led: "View all activity", "Move device."
- Use specific counts and dates. Pluralize correctly.
- "Provider" or "clinician" is the generic term; do not use "Doctor" for every
  role.
- One demo label in the app: "Demo · synthetic data." Keep the legal fineprint
  at `0.75rem` in the footer. Keep "not a diagnosis" near signals. On small
  screens, the chip may shorten to "Demo" with an accessible label; it may not
  disappear.
- Do not claim clinical validation, diagnosis, live synchronization, or a
  follow-up capability that is not implemented.
- Avoid "seamless", "empower", "unlock", "revolutionize", "AI-powered" and
  similar filler.

## 9. Accessibility requirements

- Body text contrast ≥4.5:1; large text ≥3:1. Coral buttons use indigo text.
- Visible `0.1875rem` focus ring on every interactive element: slate on light
  surfaces, coral on dark. Do not use coral on off-white.
- Skip link to `#main-content`; one `h1` per page; logical heading order.
- Tabs support Left/Right/Home/End, `aria-selected`, roving tabindex. Active tab
  underline is indigo, not coral.
- Dialogs trap focus, restore focus to the trigger, and close on Escape.
- `2.75rem` minimum touch targets; `0.5rem` minimum spacing between adjacent
  targets. Icon buttons are `2.75rem` square, not `2.5rem`.
- No horizontal scrolling for primary content at 390px. Full data tables may
  scroll only inside a labeled, focusable region.
- 200% text: no clipping, truncation or overlap.
- Status is conveyed with text and shape, not color alone. The coral attention
  border is decorative; the badge and text carry the meaning.
- `prefers-reduced-motion` disables transitions.
- Tables have captions and `scope` attributes; charts have a table alternative.
- The floating chat launcher must not cover the last row or primary action; add
  `6rem` bottom padding to `main`.
- Test at 390px, 768px and 1440px in light and dark, keyboard-only.

## 10. Implementation order

**Smallest first PR:** Phase 0 only — `reflect-theme.css` and
`provider-workspace.css`, no Razor, no copy, no controller or view-model changes.
It should pass the existing browser checks because the markup and headings are
unchanged.

| Phase | Scope | Files |
| --- | --- | --- |
| 0 | Token aliases, rem sizing, uppercase scope, button/nav radius, active-nav style, sentence-case table headers, indigo tab underline, remove `--warning-rgb` | `wwwroot/standins/css/reflect-theme.css`, `provider-workspace.css` |
| 1 | App shell: sidebar, header, demo chip, account menu, mobile, preserve test IDs | `Areas/Provider/Views/Shared/_Layout.cshtml`, `reflect-theme.css` |
| 2 | Dashboard worklist and context rail; keep "Needs attention" or update both tests | `Areas/Provider/Views/Home/Dashboard.cshtml`, `provider-workspace.css` |
| 3 | Patients toolbar, table, mobile list, pagination, pluralization | `Areas/Provider/Views/Patients/Index.cshtml`, `provider-workspace.css` |
| 4 | Patient detail header, metrics, chart, timeline; keep DOB | `Areas/Provider/Views/Patients/Details.cshtml`, `provider-workspace.css` |
| 5 | Landing, sign-in, login layout; no walkthrough link | `Views/Home/Index.cshtml`, `Views/Account/Login.cshtml`, `Views/Shared/_LoginLayout.cshtml` |
| 6 | Trust, devices, map polish | `Areas/Provider/Views/Home/Trust.cshtml`, `Areas/Provider/Views/Devices/Index.cshtml`, `Areas/Provider/Views/Home/Graph.cshtml` |
| 7 | Browser, axe, zoom, contrast and computed-style verification | `tests/browser_checks.py`, manual evidence |

Phase 0 details:

- Add semantic aliases; do not mass-rename existing tokens.
- Replace px type/spacing/control values with rem.
- Limit `text-transform: uppercase` to h1, h2, `.page-title`, the primary CTA
  and `.eyebrow`.
- Reset letter-spacing on controls.
- Replace Medium 500 and SemiBold 600 font-weight usages with Regular 400/Bold
  700 unless an approved exception exists.
- Change button, nav and `summary.btn` radius from `999px` to `0.375rem`; keep
  badges and chips as pills.
- Switch active nav to pale coral tint + `0.125rem` coral left bar.
- Make table headers sentence case at `0.75rem`.
- Use an indigo active-tab underline.
- Remove or remap `--warning-rgb`.
- Remove the `.custom-card` shadow from work surfaces.

Phase 1 constraints:

- Preserve the IDs listed in §5. The browser and JS tests depend on them.
- The sidebar width change from `16.5rem` to `14rem` is deliberate; verify the
  longest label and the collapsed initials.
- Nav icons are optional; do not block the layout on hand-drawn SVG.

Phase 2 constraints:

- Do not add a patient ID column; `AttentionRow` does not carry one.
- Do not add Engagement/Affect tabs. Use one list, or a compact secondary list
  if the data cannot merge without a view-model change.
- Keep the "Needs attention" heading unless both tests are updated.
- Keep the caseload-map suggestion below the worklist or in the rail.

Do not change controllers, repositories, authorization or data contracts for a
visual pass. The DOB-to-age change is a separate privacy/view-model change and
is deferred.

## 11. Acceptance criteria

A first pass is complete when:

- [ ] At 1440×900, the dashboard shows the summary, worklist and first Review
      action without scrolling; the page is ≤1000px tall for five patients.
- [ ] Each view has at most one primary (coral) button.
- [ ] The primary CTA is uppercase Condensed Bold; secondary and tertiary
      controls are sentence-case Plex Sans 400, `0.875rem`, `2.75rem` tall,
      `0.375rem` radius.
- [ ] Uppercase is limited to h1, h2, the primary CTA and at most one eyebrow per
      page. Nav, badges, filters and table headers are sentence case.
- [ ] Active navigation is a pale coral tint with a `0.125rem` coral left bar,
      not a solid coral pill.
- [ ] Active tab underline is indigo, not coral. Focus rings are slate in light
      mode and coral in dark mode.
- [ ] Panels have no shadow, a `0.625rem` radius and a 1px border. Table
      containers are rounded; tables and rows are not.
- [ ] Coral is used only for primary actions, attention markers and the active-nav
      indicator. It is not a focus color on light surfaces.
- [ ] At 390px and 200% text, the patients view is a readable list with no
      horizontal scrolling, no clipping, one version in the accessibility tree,
      and correct pluralization.
- [ ] Type, spacing, control heights and radii use rem; no px values for those
      properties remain in the touched CSS.
- [ ] Existing browser checks pass after Phase 0 without markup changes. Test IDs
      in §5 are unchanged after Phase 1.
- [ ] `--warning-rgb` is removed or remapped; no off-palette amber survives.
- [ ] The logo, IBM Plex fonts and brand palette are unchanged.
- [ ] Keyboard navigation, focus visibility, reduced motion and 200% zoom pass.
- [ ] Axe reports no serious or critical issues in the tested views.
- [ ] Screenshots at 1440, 768 and 390 in light and dark are stored with the
      reviewed commit.
- [ ] No new framework, font package, CDN, icon pack or animation library.

## 12. Open questions after review

Resolved in the November 6 review:

1. **Engagement vs affect:** use one worklist with a "Change" column. If the
   affect data cannot merge without a view-model change, keep it as a compact
   secondary list below the worklist. Do not add tabs for the visual pass.
2. **Dashboard title:** "Overview," not "Today." The signal windows are 7 and 14
   days, so "Today" is inaccurate; "Overview" matches the sidebar.
3. **Add follow-up:** hide it until persistence and authorization exist. Do not
   show a disabled primary action.
4. **Mobile patients view:** use a list at `<48rem`, with the full table behind
   a disclosure. This is already the plan in `NEXT-STEPS.md` §4.

Still open:

5. Does the shared demo account need per-session workspaces before task editing
   is exposed? This is a product/security decision, not a visual one.
6. Does the brand owner approve the hierarchy interpretation — condensed
   uppercase for h1/h2/primary CTA, sentence case for everything else — and the
   flat-surface treatment? The guide text does not settle this.
7. What are the exact logo clear-space and misuse rules? They were not confirmed
   in the extracted guide text; the supplied assets are the current reference.
8. Does the brand owner approve using only Plex Sans Regular 400 and Condensed
   Bold 700, excluding the shipped Medium 500 and SemiBold 600? If not, document
   the approved exception.

## 13. Do not

- Do not change the logo, fonts, palette or brand gradient.
- Do not add a UI framework, CSS reset, icon library, font CDN or animation
  library.
- Do not use pill-shaped buttons, uppercase secondary/tertiary buttons, uppercase
  nav, uppercase badges or uppercase table headers. The primary CTA is the
  exception.
- Do not use a coral focus ring or coral active-tab underline on off-white.
- Do not use px for type, spacing, control heights or radii.
- Do not stack every section in a rounded, shadowed card.
- Do not use marketing headlines inside the authenticated workspace.
- Do not add Engagement/Affect tabs; use one worklist or a compact secondary
  list.
- Do not add a "Watch the walkthrough" link until a video exists.
- Do not hide the synthetic/demo labeling or imply clinical validation.
- Do not add fake scores, fake charts, fake audit trails or decorative actions.
- Do not use color alone to communicate a signal.
- Do not use `text-nowrap` on primary patient tables.
- Do not use a gradient, glass effect or hover lift to make the product feel
  "modern." Restraint is the design.

## 14. Review reconciliation — November 6, 2026

Accepted from the review:

- Split guide rules from project interpretation; the guide's extracted text does
  not contain the logo, eyebrow, gradient-scope or button rules the first draft
  presented as non-negotiable.
- Move type, spacing, control heights and radii to rem so the in-app Text size
  control and browser zoom work.
- Add semantic aliases rather than mass-renaming existing tokens.
- Keep the primary CTA uppercase condensed; make secondary/tertiary controls
  sentence-case Sans 400. Remove the Medium 500/SemiBold 600 default.
- Use indigo active-tab underlines and slate focus rings on light surfaces.
- Make icon buttons `2.75rem`; raise the eyebrow to `0.75rem`.
- Preserve the tested IDs in Phase 1 and update tests only if the "Needs
  attention" copy changes.
- Make Phase 0 CSS-only; defer markup, shell and dashboard changes to later
  phases.
- Use "Overview," hide "Add follow-up," keep the existing engagement list, and
  keep the caseload-map suggestion below the worklist.
- Remove the walkthrough link until a video exists; defer the DOB-to-age change.
- Remove or remap the off-palette `--warning-rgb`.

Still open: shared demo state (Q5), brand sign-off on hierarchy and flat surfaces
(Q6), logo clear-space details (Q7), and Medium/SemiBold exceptions (Q8).
