# TB-P10-T004-R24-R1-R4 — Network budget

Host `:5088` + FE `:3000`. Script `_r24-r1-r4-runtime.mjs`. Raw `_r24-r1-r4-runtime-raw.json`.

| Signal | Count | Notes |
| --- | --- | --- |
| `/api/auth/me` | 16 | one resolve per full document load / login invalidate; R3 was 96 |
| `/api/auth/me` 401 | 8 | anonymous full loads only; 0 growth while idle after logout |
| `/cart/merge` | 3 | 1+1 login transitions + 1 Q guest-while-auth |
| `/auth/logout` | 1 | single canonical logout |
| `/shipping/commit` | 2 | one 200, one injected 409 |

No interval. No simultaneous desktop/mobile duplicate `/me` after cache. No commit double-submit. No logout double-submit.
