# OpenDesign — real execution record (TB-294)
Daemon: http://127.0.0.1:7455  (GET /api/health → {"ok":true,"version":"0.25.1"})
Design direction (OpenDesign): `od tools directions --id tech-utility --json` → saved to design-direction.json
  (Datadog/GitHub utility posture: dense hairline tables, inline status pills, tabular numerics).
Artifact authored in project workspace: index.html (4 views + pickers + create/edit/error/mobile).
OpenDesign anti-slop lint (agent path): `od lint index.html --fail-on none --json`
  BEFORE: p0=0, p1=1 (raw-hex x21 outside :root), p2=1 (4 sections without data-od-id)
  FIX (designer): tokenized all colors into :root (var()), added data-od-id to 4 sections.
  AFTER: {p0:0,p1:0,p2:0} — CLEAN.
Rendered verification (Chromium): dependency matrix all true; overlap=0; minFont=13; contrast 16.4/6.99.
Limitations this session: OpenCode MCP tool `open-design` is not bound in this session's toolset; `POST /api/projects`
returned HTTP 400 for all tested bodies, so the OpenDesign *project object* wasn't auto-registered. The artifact,
direction and screenshots are preserved here for import/open in OpenDesign UI (http://127.0.0.1:7456).
