# Demo video brief: Reflect Provider Dashboard

For whoever (or whatever tool) produces the demo video. Follow this as written. Everything on screen is synthetic.

## 1. What the product is, in one breath
Reflect XR and Reflect Mobile give people calming, creative and self-reflection activities. **Reflect Provider Dashboard** is
the clinician's view of how their clients use those apps *between* sessions: who has gone quiet, whose mood is below their
own usual range, what to follow up on, and how to reach them. It is a prototype built on fictional records.

**Audience:** competition judges and technical or clinical reviewers. **Tone:** calm, plain, confident, never salesy.
**Length:** a 3:00 main cut (section 4). Optional 7:00 extended cut using `docs/DEMO-SCRIPT.md`.
**One idea to land:** "A clinician sees who needs attention and why, without reading everyone's data, and the app is honest
about what is real and what is still a prototype."

## 2. Setup before recording
1. Start the app (`ASPNETCORE_ENVIRONMENT=Development`, https://localhost:7150) or use the hosted demo URL.
2. Sign in: `doctor@reflect.demo` / `ReflectDemo2026!` (Development). On the hosted demo use the host's demo credentials.
3. Team (left sidebar, under Admin) → **Reset demo data**. This refreshes dates so "this week" is correct. Do it right before each take.
4. Browser: Chrome, 1920x1080, 100% zoom, light theme, bookmarks bar hidden, no other tabs or notifications. Use a fresh
   window. Dismiss the welcome tour with Skip (or show it for 5 seconds in the intro, see shot 1).
5. Do not show: the browser address bar with a private host, your real email, any terminal, or user-secrets/API keys.
6. Record at 30 fps with a visible cursor and a subtle click highlight. Move the mouse slowly and deliberately.

## 3. Rules for what to show and say
- **Say "synthetic" or "fictional" at least twice.** The yellow "Synthetic demo" banner stays visible.
- **Never claim**: HIPAA compliant; messages delivered; real email or push sent; real clinical advice; a live AI model.
  Safe wording: "designed around HIPAA safeguards", "prototype outbox", "prepared answers".
- **Avoid** these pages or settings, which are saved but not yet applied: time zone, clock format, and notification toggles.
- Do not read patient *names* as if they were real people. Say "a client" or use first names only once.
- Show the product working; do not narrate every field. Zoom into the one thing that matters in each shot.
- Keep all text legible: zoom the recording to 125-150% on small text, never leave a small table unzoomed.

## 4. Shot list: 3:00 main cut
Timings are targets. Narration is the voiceover (about 150 words per minute). On-screen captions in quotes.

| # | Time | Screen (exact clicks) | Narration | Caption |
|---|---|---|---|---|
| 1 | 0:00-0:15 | Landing/login, sign in, Dashboard appears. | "Clinicians see their clients for an hour. This is what happens in the weeks between." | "Reflect Provider Dashboard · synthetic data" |
| 2 | 0:15-0:40 | **Dashboard.** Pause on the counts, then on **Needs attention**; hover the numbers. | "The dashboard answers one question: who needs me, and why. This person had no sessions this week, against several the week before. The numbers are shown, so nothing is a black box." | "Shows the numbers behind every flag" |
| 3 | 0:40-1:10 | Click the **Below self** card → patient **Overview**; zoom on the **Emotional trajectory** chart; open "View data table". | "Mood markers are compared with that person's own recent range, not a population average. A coral flag means a change from their usual. It prompts a look. It is not a diagnosis." | "Compared with self, not a population norm" |
| 4 | 1:10-1:35 | Scroll to **Follow-ups** → add "Check in about sleep routine", due tomorrow → Add. Then **Programs used**. | "The clinician leaves themselves a follow-up. Below it, which Reflect programs this person actually uses, and which they have not tried." | "Follow-ups and program use" |
| 5 | 1:35-1:55 | **Timeline** tab → set Program filter to one program → Apply. | "Every session comes from the official Reflect module list, on mobile and in VR, and can be filtered by program." | "Official Reflect module catalog" |
| 6 | 1:55-2:15 | Sidebar **Weekly digest** → pause on the sections. | "Monday morning, one printable page: who went quiet, whose mood dipped, what is due, and what people used." | "Weekly digest" |
| 7 | 2:15-2:35 | **Messages** → choose a client, pick the "Try a calm module" template, type a fake phone number into the text, Queue. Show the notice that identifiers were removed. | "Reaching out is a short nudge. Identifiers are stripped before anything is stored. This is a prototype outbox: nothing is delivered yet, and future pushes will carry no patient information." | "Prototype: queued, not delivered" |
| 8 | 2:35-2:50 | **Team** (admin): show Providers, Assign patients, Audit trail. Do not click Invite. | "Accounts are invite-only. An administrator assigns patients but sees only IDs, never names or sessions, and every sign-in, record view and export lands in a tamper-evident audit trail." | "Invite-only · audit trail" |
| 9 | 2:50-3:00 | **Privacy & trust** page, slow scroll through the three pills. End card. | "We are plain about what exists today and what is a production target. Reflect: care between sessions." | End card: "Reflect XR · Persistent Technology" |

## 5. Optional extra shots (extended cut or B-roll)
- **Invite flow:** Team → Create invite → copy link → open in a private window → set password → sign in → empty caseload → back as admin, assign a patient → refresh the second window to show it appear. (~45 s)
- **Two-step sign-in:** Settings → Sign-in security → show the setup page and recovery codes (blur the key). (~20 s)
- **Assistant:** click the round Reflect bubble bottom-right, click "What does Hope Tree do?" (prepared answer; say "prepared answers, no outside AI"). (~15 s)
- **Dark mode and large text:** Account & display → Accessibility → Extra large text, and the theme toggle. (~15 s)
- **Caseload map:** sidebar Caseload map; thicker lines mean more sessions. (~15 s)

## 6. Visual and brand guidance for titles
- Palette: indigo `#1F305E`, coral `#FF8282`, slate `#586F8A`, off-white `#FAFAFB`, pale coral `#FDE6D9`.
- Type: IBM Plex Sans Condensed Bold, uppercase, for titles; IBM Plex Sans Regular for captions. Keep captions short and high contrast
  (indigo on off-white, or off-white on indigo). Never put coral text on off-white.
- Intro title card (2 s): Reflect logo, "Provider Dashboard", "Synthetic data · Prototype". End card (4 s): logo, "Reflect XR by Persistent Technology".
- Include captions or burned-in subtitles for the whole voiceover. Calm background music at low volume, or none.

## 7. Export checklist
- 1920x1080, H.264 MP4, 30 fps, AAC audio, under 100 MB, captions included as a separate `.srt`.
- Watch it once muted: every shot must make sense from the captions alone.
- Check: no secrets, no real emails, no private host names, the "Synthetic demo" banner visible in most shots.
- Final two honesty lines on screen near the end: "Fictional data only" and "Prototype: messages are not delivered".

## 8. If something goes wrong while recording
- Data looks stale or odd: Team → Reset demo data, then restart the take.
- A page errors: refresh once; if it repeats, stop and tell the team. Do not edit around it silently.
- Assistant or AI behaviour: it is prepared answers only, so there is nothing to wait for. Do not imply a live model.
