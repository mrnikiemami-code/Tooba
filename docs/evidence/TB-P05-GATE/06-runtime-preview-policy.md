# 06 — Runtime preview policy forward (TB-P05-GATE)

Locked for future UI / visual Tasks:

| Step | Policy |
|---|---|
| 1 | Start **Tooba Backend** first; verify `/health` |
| 2 | Start **Tooba Frontend** (`next dev`); verify Home/PDP HTTP 200 |
| 3 | Start **original Shopeiva** on non-conflicting port (e.g. `:3017`) when visual comparison relevant |
| 4 | Verify all required runtimes **before** tracked code changes |
| 5 | After `next build`, restart `next dev` if needed (`.next` conflict); re-verify Home/PDP |
| 6 | Result must include exact **USER-SIDE-BY-SIDE-PREVIEW** URLs (no placeholders) |
| 7 | Leave runtimes running at Result when technically possible |

Known URLs (Development):

- Tooba Backend: `http://127.0.0.1:5088/health`
- Tooba Frontend: `http://127.0.0.1:3000/`
- Shopeiva reference: `http://127.0.0.1:3017/`

**Runtime preview policy: LOCKED**
