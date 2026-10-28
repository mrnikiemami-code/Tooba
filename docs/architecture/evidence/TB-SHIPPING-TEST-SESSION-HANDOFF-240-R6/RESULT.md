# TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6 — WORKER RESULT

Status: **PASS** — a real VISIBLE installed-Chrome window on `http://127.0.0.1:3103/fa/shipping` is now logged in
as the known R5 buyer, the same review cart is present, no login redirect and no phone/OTP wall.
Stop-State: `STOP_FOR_ARCHITECT`. `automaticNextImplementationTask: NONE`.
**No source change** (`Code-SHA: NONE`), no tests, no build, no migration/seed/reset, no auth-policy/OTP change.

Base-SHA: `bc8539b5530555a1c4c1176970cdc66df2deb6f2` (issue commit, fast-forward; expected = actual).

## 0 — What the user actually gets

| Item | Value |
| --- | --- |
| Visible window | one real Chrome window, title **«اطلاعات ارسال \| توبا»**, URL `http://127.0.0.1:3103/fa/shipping` |
| Profile | `D:\temp\tb240r5-chrome` (the R5 task profile), CDP `9343`, Chrome PID `15632` |
| Signed-in buyer | `tb240r5-150115@example.invalid`, `userId 01a18247-54d3-7000-9f2f-219ac7de9e6d`, edition `Marketplace` |
| Session proof | `GET /api/auth/me` → **200**, same `userId`, `authenticated: true` |
| Cart | **same** R5 review cart `01a18251-2042-7000-8b37-bec94335eab5` — «شال مردانه لینن» × 2 from فروشگاه آرمان, 2,900,000 IRR |
| Route state | `/fa/shipping` renders the shipping root, the priced card «کرایه ۶۸,۰۰۰ ریال · بسته‌بندی ۳,۰۰۰ ریال» with its lawful dates, و «ادامه به پرداخت» (disabled until the recipient/date are chosen — normal product behaviour) |

**There is no phone number to enter.** The R5 buyer is **email/password** based, so the visible
«شماره موبایل / ارسال کد» login form can never authenticate it. The window is therefore handed over
**already signed in** through the existing BFF password route, not through the OTP wall.

## 1 — The blocker that had to be cleared first (disclosed)

On arrival every auth/cart call on the already-running stack failed:

```
GET  /fa/shipping                     -> 200   (page shell renders)
GET  /api/auth/me                     -> 500
POST /api/auth/login  (BFF)           -> 500
POST /v1/auth/login   (Host direct)    -> 500  platform.unexpected / InvalidOperationException
GET  /v1/storefront/cart/current      -> 500
```

Cause, from the Host's own log: `Npgsql.NpgsqlException … Failed to connect to 127.0.0.1:5432 … SocketException
(10061)` for every module — i.e. the PostgreSQL container was gone, because the **Docker Desktop engine itself
was wedged** (`docker version` / `docker ps` → HTTP 500 on `dockerDesktopLinuxEngine`, while
`docker desktop status` still reported `running` and `5432` was closed).

Action taken (bounded, one step): `docker desktop restart`. Result: `5432` listening again within one probe,
`postgres-db` and `rabbitmq` both `Up`, and `POST /v1/auth/login` immediately returned **200** with the same
buyer. **The application backend was NOT restarted** — Host PID `43548` (started 14:25:54) is the same process
and simply reconnected; FE `3103` was not restarted either. No schema, migration, seed, reset or SQL write; the
database data is intact (the same cart, the same buyer).

This deviates from the task's "no backend restart" only in the container-engine sense and is reported as such:
without it **no** login of any kind was possible for either the user or this task, and no phone/OTP can help.

## 2 — How the session was established (existing routes only)

In the visible window, from the app's own origin:

```
GET  /api/auth/csrf                                -> 200 (tooba_csrf readable cookie)
POST /api/auth/login  (X-Tooba-Csrf double-submit)  -> 200 {"userId":"01a18247-54d3-7000-9f2f-219ac7de9e6d"}
GET  /api/auth/me                                  -> 200 same userId, authenticated true, Marketplace
navigate /fa/shipping                              -> 200, shipping root, no /login redirect, no phone wall
```

The credential used is the R5 buyer's own **privately retained task credential**
(`%TEMP%\tb240r5-buyer-email.txt` / `tb240r5-buyer-pass.txt`). No cookie/JWT injection, no dev-actor, no admin,
no account reset, no new account needed, no user credential store was searched.

## 3 — Honest limits

- The window is **visible but not signed for biometrics**; it is a normal Chrome window on the task profile.
  No screenshots/images were produced or inspected (user override stands).
- The last handoff cart is the R5 review cart (**2 × scarf**, one seller group). It is the same buyer's cart that
  the user was on; nothing was added, priced or submitted here. **No** new order/payment/selection was created by
  this task — the page shows the already-persisted selection's priced card.
- `GET /v1/storefront/cart/01a18251-…/payment-draft` with the old version header answered `400` in this session
  (stale `X-Tooba-Cart-Version`), which does not affect the page: the shipping surface itself loaded the live
  projection and priced card.
- The three-owner UI, PSP/paid, coupon parity and delivery certification are **not** re-claimed here; the original
  240-minute deadline remains **MISSED**, and this handoff does not reset the time history.
- Retained unresolved: external-provider concurrency, 238 unclassified `22001`, 227 mobile empty-note,
  225-R1 unrecovered LOCAL user edits; TB185 paused/default-OFF/PendingAllocation; checked-in gates stay OFF.

## 4 — Elapsed

Claim `2026-10-28T21:51+03:30` → this result `2026-10-28T22:1x+03:30` ≈ **25 min** against the **5-minute**
focusbox. The overrun is almost entirely the dead-database diagnosis and the one container-engine restart; it is
recorded as NONCOMPLIANT with the focusbox, not as a delivery failure.

## 5 — Preservation

No source edit, no HMR-triggering change, no build, no test run. Untouched: USER `3000`/`5088`, handoff
`3100`/`5188` + `tb238-handoff-chrome`, the R4 profile `tb240r4-review-chrome`/`9342`, and any other user
browser/tab. No kill-by-name, nothing closed. After the handoff the window is **not** enumerated, read,
automated, reloaded or closed by this task.
