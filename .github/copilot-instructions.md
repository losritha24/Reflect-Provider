# Reflect Provider Dashboard

Read `docs/NEXT-STEPS.md` for current priorities and acceptance criteria. This is a
synthetic ASP.NET Core MVC/.NET 10 competition demo, with Razor views and local
IBM Plex fonts. Preserve the Reflect coral/indigo brand and existing area routing.

- Make bounded changes in the assigned files. Coordinate shared Program.cs,
  controllers, views and test runner edits with the lead agent.
- Patient access must use the authenticated, assigned-provider repositories.
  Never query the global seed directly from controllers or views.
- Keep hosted registration closed, antiforgery on mutations, no-store on private
  HTML, and logout available even when login attempts are rate-limited.
- Never trust raw forwarded headers or add a blanket authentication bypass.
- This shared demo uses fictional records only. Do not describe its demo audit,
  local storage or identity design as clinical production readiness.
- Keep filter state through navigation. Use typed local routes; encode user text.
  Test keyboard behavior, empty results, future dates and authorization failures.
- No secrets in source, logs, browser storage, Docker build args or screenshots.
  The documented Development demo credential is deliberately public.
- Run `python3 tests/verify.py` after integration. For UI changes also run
  `python3 tests/verify.py --browser` after installing tests/requirements-browser.txt
  and Chromium. Record actual test outcomes and limitations.
- Do not commit, push, publish or create paid resources unless the user requests it.
