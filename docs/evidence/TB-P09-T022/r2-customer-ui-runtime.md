# TB-P09-T022-R2 — customer UI runtime

## Auth in browser

Real page `fetch` path (not fabricated Host headers):

1. `GET /api/auth/csrf` → `tooba_csrf`
2. `POST /api/auth/login` with CSRF → `tooba_session`
3. Navigate Customer Panel order detail

## Route

```text
http://127.0.0.1:3000/fa/customer-panel/orders/01a089a6-154f-7000-9586-bc1463ac6c8b
```

| Check | Result |
| --- | --- |
| Page / BFF | HTTP **200** |
| Primary tracking label | `کد پیگیری: CENTRAL-T022-R2` (pre-rebuild) |
| Member shipments | Member `TRK-…` tracking still listed |
| Runtime | No FE 500 / hydration crash on this path |

After package rebuild (see `r2-rebuild-customer-ui.md`), primary updates to `CENTRAL-T022-R2-REBUILT`.

`USER_VISUAL_ACCEPTED=NO` — Worker does not claim human visual acceptance.
