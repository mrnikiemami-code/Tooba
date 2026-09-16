# Runtime Availability

| Surface | Status |
|---------|--------|
| Host :5088 | up (HTTP 200 /health) during capture |
| FE :3000 | up (node listen; /template-preview/fashion 200; /admin/landing-pages/new 200) |
| /admin/landing-pages/new | reachable; Fashion template + Store source used for iframe banner |
| /template-preview/fashion | reachable Sample + Store |
| /template-preview/fashion/full | reachable Store full-page banner |
| Canonical Store preview | `?source=store` (iframe + full) |

Capture: `capture.mjs` → `runtime-report.json` (`ok: true`).
