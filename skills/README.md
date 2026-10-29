# Shared Codex and Claude skills

Installed October 29, 2026. Canonical copies live in `skills/vendor/`.
Both `~/.codex/skills/<name>` and `~/.claude/skills/<name>` were linked to these
same directories without replacing existing entries. Codex discovers them on the
next turn; start a new Claude session if its skill list is already cached.
These personal symlinks depend on this checkout's absolute location. On another
machine, link each directory from that machine's checkout into both skill folders.

| Skill | Source | Applied here |
| --- | --- | --- |
| sharp-edges | [Trail of Bits](https://github.com/trailofbits/skills/tree/main/plugins/sharp-edges/skills/sharp-edges) | Host configuration and authentication boundary misuse analysis |
| variant-analysis | [Trail of Bits](https://github.com/trailofbits/skills/tree/main/plugins/variant-analysis/skills/variant-analysis) | Repository-wide search for variants of the original assignment bypass |
| webapp-testing | [Anthropic](https://github.com/anthropics/skills/tree/main/skills/webapp-testing) | Playwright browser checks, screenshots, focus and encoding verification |
| frontend-design | [Anthropic](https://github.com/anthropics/skills/tree/main/skills/frontend-design) | Brand-constrained design direction, tokens, wireframes and implementation plan |

These are portable instruction packages, not security scanners or proof that an
application is secure. The third-party plugin agents/commands are not installed;
this assessment followed the narrow manual workflows and invoked our own tests.
The originally searched `insecure-defaults` package was absent from the current
upstream tree and was not installed. No extra skill was fabricated to replace it.

The installer downloaded `main` snapshots. `SHA256SUMS.json` fingerprints the
installed files; it is not an upstream commit pin. Preserve supplied licenses;
Trail of Bits' repository license is in `vendor/TRAILOFBITS-LICENSE`.
Do not execute arbitrary bundled helpers without review. No external target was
attacked, and no source code was uploaded to an audit service.

## Project-specific constraints for both agents

Read `docs/UI-IMPLEMENTATION-PLAN.md` before design work and follow its linked
Reflect XR guideline. The upstream frontend-design skill's aesthetic suggestions
must not replace the required fonts, uppercase primary typography, logos or palette.
Retain ASP.NET MVC/Razor and the Provider area; do not introduce React or a new
component library merely because a reference product uses it.

For repeatable tests, read `docs/SECURITY-TEST-REPORT.md`. Browser checks need
Playwright; installed skill instructions alone do not install that dependency.
