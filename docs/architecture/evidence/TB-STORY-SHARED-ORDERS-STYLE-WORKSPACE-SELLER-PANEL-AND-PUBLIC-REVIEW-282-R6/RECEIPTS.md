# RECEIPTS — TB-282-R6

## Claim

- Bridge row `349f4942-a5da-4ac4-b2b9-ac327ee08280` Claimed
- Expected-Base `5a0b341ab5d3fcd7ab8539d3f5c488562529dd6b` (ancestor)
- Claim HEAD `1338272c7576d3e1d4f12eae34125f38bfd13aa5`
- Canonical TASK.md (issuance SHA256 `50d6c70ee6cde9d0365c13004452e14b64f86d23b34bbbab0c0ac048e9f82749`); Bridge content emptied after claim

## Frontend focused

```
story-management-282r6.mount.test.ts → 3/3
story-management-282r4.mount.test.ts + copy tests → 11/11 combined with R6 run 13/14 then R6 B/C fixed 3/3
story-management-282r5.mount.test.ts → 6/6
session-cookies.auth-secure.test.ts → 3/3
npx tsc --noEmit → 0
```

## Served

- Host `127.0.0.1:5088` PID `764` dll SHA256 `F42F3132837E7DEA841C829BFFBE3428E2ADE977E5EAC003CB43E31F32F50655`
- `/health` 200; `/health/ready` 200 messaging Healthy; `/__platform-commerce` store-alpha; `/v1/admin/dev-context` 200
- Next `127.0.0.1:3000` PID `19460` BUILD_ID `yq3D1_b-sYRUJXbreLqoj` `_buildManifest.js` 200
- Routes 200: `/fa/admin/stories`, `/new`, `/fa/vendor-panel/stories`, `/new`, `/fa`
- CSRF on HTTP: `tooba_csrf` Secure=false; BFF login 200 `admin-actor@tooba.local`
- Authenticated: create workspace both roles; list-only review filter; detail location ویترین اصلی; stored internal labeled فروشگاه; overflowX 0 at 390 and 1440
- Host Development without skip-bootstrap so owner login works (task D skip-bootstrap would 404 dev-context)

## Schema / live

- Migration/DDL/seed/reset: NONE
- Live Story/Media business write: ZERO

## Git

- User-Work-Preserved: YES
- Implementation `49b66122739a83eef5f69a69227990e49a17eea5`
- Origin parity after pin commit (result-contract)
