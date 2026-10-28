# TB-SHIPPING-TEST-SESSION-HANDOFF-240-R6 — RECEIPTS

Sanitised, no password / token / cookie / guest-secret value is reproduced. No images.

## Environment (actual, unchanged)

| Item | Value |
| --- | --- |
| Host | `http://127.0.0.1:5190` — `tb239-marketplace`, Development, DB `tooba_marketplace_tb239`, PID **43548** (started 14:25:54, **never restarted**) |
| FE | `http://127.0.0.1:3103` — `D:\temp\tb240r5-fe` worktree dev server, **not restarted** |
| Visible window | Chrome PID **15632**, profile `D:\temp\tb240r5-chrome`, CDP **9343**, title «اطلاعات ارسال \| توبا» |
| Buyer | `tb240r5-150115@example.invalid` → `userId 01a18247-54d3-7000-9f2f-219ac7de9e6d`, `Marketplace` |

## 1 — Arrival state (before recovery)

```
GET  http://127.0.0.1:3103/fa/shipping   -> 200
GET  http://127.0.0.1:5190/health        -> 200
GET  /fa/shipping (browser)              -> redirect /fa/login?returnTo=%2Ffa%2Fshipping   (phone wall)
GET  /api/auth/me                        -> 500
POST /api/auth/login (BFF, csrf ok)      -> 500
POST /v1/auth/login (Host direct)        -> 500 t=2.07s
     {"title":"An unexpected error occurred.","status":500,"detail":"InvalidOperationException",
      "errorCode":"platform.unexpected","traceId":"c244799cb02cb7a57ad6d1903deaf1e3"}
GET  /v1/storefront/cart/current         -> 500
GET  /v1/storefront/cart/01a18251-…/payment-draft -> 500
```

Root cause in the Host's own log (`D:\temp\tb240r4-host.log`, 150 MB, tail):

```
Npgsql.NpgsqlException (0x80004005): Failed to connect to 127.0.0.1:5432
  ---> System.Net.Sockets.SocketException (10061): No connection could be made
Tooba.Host.Outbox.OutboxDispatcher  Outbox claim failed for one module … Schema=promotion|reviews|wishlist
MassTransit                         PgSql notification faulted / PostgreSQL Maintenance Faulted
```

Container engine state:

```
docker version        -> 500 Internal Server Error (/v1.54/version)
docker ps             -> 500 Internal Server Error (/v1.54/containers/json)
docker desktop status -> Status running   (engine wedged)
Test-NetConnection 127.0.0.1:5432 -> False
```

## 2 — Recovery (one bounded step, no application restart)

```
docker desktop restart  -> "✓ Starting Docker Desktop"
docker ps               -> rabbitmq Up 24 seconds | postgres-db Up 24 seconds
POST /v1/auth/login     -> 200 t=0.20s  userId 01a18247-54d3-7000-9f2f-219ac7de9e6d (access+refresh issued)
Host PID 43548          -> unchanged (reconnected on its own)
```

## 3 — Visible handoff (existing routes, app origin, real profile)

```
GET  /api/auth/csrf                          -> 200 (tooba_csrf cookie set)
POST /api/auth/login  X-Tooba-Csrf           -> 200 {"userId":"01a18247-54d3-7000-9f2f-219ac7de9e6d"}
GET  /api/auth/me                            -> 200 same userId, authenticated true, edition Marketplace
navigate http://127.0.0.1:3103/fa/shipping    -> 200
  title «اطلاعات ارسال | توبا» ; [data-testid="shipping-page"] present
  /login redirect: none ; phone/OTP wall: none ; «شماره موبایل خود را وارد کنید» absent
```

Final window snapshot (last read before handoff):

```
url        http://127.0.0.1:3103/fa/shipping
me         status 200  userId 01a18247-54d3-7000-9f2f-219ac7de9e6d
GET /v1/storefront/cart/current -> 200
  cartId 01a18251-2042-7000-8b37-bec94335eab5  version 5  itemCount 2
  line «شال مردانه لینن» × 2 — فروشگاه آرمان     totals IRR 2,900,000
priced card «ارسال فروشگاه آرمان ۲۴۰ — کرایه ۶۸,۰۰۰ ریال · بسته‌بندی ۳,۰۰۰ ریال» + its lawful dates
[shipping-continue] «ادامه به پرداخت»، disabled=true (recipient/date not chosen yet — normal)
```

Same cart identity as R5: `01a18251-2042-7000-8b37-bec94335eab5`, owned by the same buyer ⇒
**same buyer, same cart, no transfer claim needed**.

## 4 — Not done (deliberately)

No source file touched, no build, no test, no `tsc`, no migration/seed/reset/reseed, no direct business SQL,
no account reset, no new account, no auth/OTP policy change, no admin/elevation. No screenshots.
Untouched surfaces: USER `3000`/`5088`, handoff `3100`/`5188` + `tb238-handoff-chrome`,
R4 `tb240r4-review-chrome`/`9342`, user browser/tabs. After handoff nothing was enumerated, automated,
reloaded or closed.
