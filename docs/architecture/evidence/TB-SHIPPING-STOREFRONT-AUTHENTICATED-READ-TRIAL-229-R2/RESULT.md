# TB-SHIPPING-STOREFRONT-AUTHENTICATED-READ-TRIAL-229-R2 — RESULT

Task: `TASK.md` in this folder (verbatim, issued by the Architect).
Parent: TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1 (`../TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1`).
Base: `e7bc126f4c44499e113fdb357854006666eb2efb` (issue commit; fast-forwarded from `df6ec9017`, the three
pre-existing Shipping CRLF drifts never staged). Mode: NORMAL_LOCAL_BUYER_SESSION_AND_REAL_CARD_OBSERVATION.
Verdict: **EXECUTED — live authenticated read OBSERVED**, captured cards rendered from the real projection at
1440 and 390 for a multi-line and a single-line cart. No source change was needed. Final Browser-Login/
Commerce question answered (below). STOP_FOR_ARCHITECT, no user Visual ACCEPT claimed.

## A — the R1 blocker was ONLY authentication, and the "Commerce resolution failed" line was the SAME exception

R1 logged two suspicious lines for one trace. Correlated now on the real log:

```
10:25:46.2258933 TenantResolutionMiddleware  "Commerce resolution failed. TraceId=f0709a00b1a9c1fbfd7f6d9dbe5dc0e4
                                              ErrorCode=checkout.authentication_required Path=/v1/storefront/shipping/projection"
10:25:46.2259324 ExceptionPresentationService "Handled business exception. ErrorCode=checkout.authentication_required
                                              Classification=Forbidden StatusCode=401 ... TenantId=store-alpha StoreId=store-alpha
                                              ActorId=(null) Path=/v1/storefront/shipping/projection"
```

`TenantResolutionMiddleware.InvokeAsync` wraps its whole `_next(httpContext)` call in the same `catch (SemanticException)`
that it uses for resolution failures, so ANY business exception thrown deeper is logged with the misleading
"Commerce resolution failed" text. The exception is the identity gate's `checkout.authentication_required`
(`CheckoutIdentityGate.EnsureCheckoutActorAsync`), the tenant/store were already resolved (`TenantId=store-alpha`,
`StoreId=store-alpha`) and the only null was `ActorId`. Correction recorded without weakening any middleware or
guard: commerce resolution was NOT the blocker, and the planner did not manufacture the 401 — the Host identity
gate refused before asking the planner.

## B — real, normal buyer session (no bypass, no invented identity)

- Registered ONE task-owned account through the normal public endpoint `POST /v1/auth/register`
  (`IdentifierKind=Email`, unique synthetic non-deliverable `…@example.invalid`, random temporary password) →
  `201 {"userId":"01a17cf7-e38d-7000-82a5-00c57929bc54"}`.
- In the fresh task-owned Chrome, `GET /api/auth/csrf` (real cookie) then `POST /api/auth/login` from the app
  origin with the `X-Tooba-Csrf` double-submit → `200 {"userId":"01a17cf7-…","authenticated":true}`, and the
  route set its own normal cookies. `GET /api/auth/me` → 
  `{"userId":"01a17cf7-e38d-7000-82a5-00c57929bc54","sessionId":"01a17cf8-…","edition":"SingleStore","tenantId":"store-alpha"}`.
- That may be composed with the existing Next middleware, which attaches `Authorization: Bearer tooba_session`
  to `/v1/*`; no custom cookie, token, dev-actor, role or SQL injection anywhere. No password/token/cookie was
  written into any evidence file or screenshot.

## C — the real trial (live, real data, no interception)

Runtime: Host `127.0.0.1:5088` PID 36952 `D:\temp\tb229r1-build\out\Tooba.Host.exe` (backend source parity with
`df6ec9017` verified: working tree clean apart from the three CRLF drifts, no backend edit since the R1 build),
`ASPNETCORE_ENVIRONMENT=Development`, content root `D:\temp\tb229r1-build\out`, `/health` 200,
`Tooba__StorefrontCapturedPreview__Enabled=true`, `Tooba__StorefrontCapturedPreview__AvailableDateCount=7`
(preview policy; never `DeliveryHorizonDays`). FE `127.0.0.1:3000` PID 28288 unchanged (no restart).
Browser: installed Chrome 155.0.8059.85, `--headless=new`, port 9336, fresh profile `D:\temp\tb229r2-chrome`,
every page in a target created by this task; no user tab enumerated, selected or navigated.

Cart: guest cart `01a17cfd-c1b1-7000-b10d-8cf21258a1cd` (v1) + two real offers of seller
`01a030d1-40cb-7000-8abe-6d31739956c5` — `0442a1fa-1d95-4b24-b9d9-129fc19942d2` (8,702,542) and
`1b56d013-5817-4db1-99a3-010e12dc0631` (7,310,476), quantity 1 each — then `POST /cart/merge` with the guest
secret bound it to the account (v6, itemCount 2, subtotal 16,013,018 IRR). Later, for the single-line case, two
lines were removed through the normal DELETE endpoint (v9, itemCount 1). No quantity/weight was fabricated.

Live projection: `POST /v1/storefront/shipping/projection` → **200** with `capturedPreview.enabled=true`,
`currency="IRR"`, two groups keyed `cap|unassigned|<offerId>`, each carrying the real seller party, the real
`cartLineIds` and `unavailableReason="storefront.captured_preview.destination_required"` (no destination has
been submitted on the page — the address form was filled only in the browser, never saved). The goods amount is
absent/`null` in that state, i.e. honest unknown rather than `0`.

Rendered UI (real DOM, real data, no mocked payload, no text injection):

- one existing groups surface only, `legacyMethodCards = 0` — no duplicate legacy method card next to the
  captured cards, at 1440 and 390;
- multi-line: 2 captured group cards (`shipping-group-cap|unassigned|…`), each with its own product line
  (نایک شلوار مردانه سری 1 / آدیداس پیراهن مردانه انتخاب 5), seller فروشگاه آرمان, type «تعیین‌نشده»,
  goods amount «مبلغ کالا نامشخص» and one short reason «برای زمان و هزینه، مقصد را انتخاب کنید.»;
- single-line (cart v9, 1 line): 1 captured group card, same surface, `singleSection=false` (the preview
  network owns the presentation), no legacy card;
- the read-only notice «این پیش‌نمایش فقط‌خواندنی است؛ انتخاب و ثبت روش ارسال هنوز فعال نیست.» and the summary
  note about per-group fee/date are visible in both widths;
- `scrollWidth == innerWidth` with `overflowX=false` at 1440 (1440) and 390 (390) for both carts;
- no horizontal overflow, no duplicate fees, no fabricated price: shipping cost/date stay `—` because the
  destination/rate path is not selected, which is the honest unavailable state.

Screens (native PNG, dimensions read from the files):
`screens/229r2-1440-2line.png`, `screens/229r2-390-2line.png`, `screens/229r2-1440-1line.png`,
`screens/229r2-390-1line.png`.

NOT claimed: a positive two-group *financial* quote live (the real local capture is destination-gated and the
offers carry no assignment, so the live state is legitimately unavailable — the positive 99.00/198.00
composition remains the R1 real-composer harness fact, not a live one); selection/commit/order/reservation/
payment; the `not_ready` write guard was NOT exercised (nothing was submitted); Storefront activation, price,
eligibility or readiness; Marketplace cross-owner behaviour (SingleStore installation).

## D — state left behind

- Local Host stays on `5088` **with the preview ON** for the Architect/user visual test, read-only: the page
  shows the read-only notice and the server still refuses selection/commit on
  `storefront.captured_preview.not_ready`. Checked-in `Enabled` stays `false`.
- Trial account id `01a17cf7-e38d-7000-82a5-00c57929bc54` (`…@example.invalid`, local-only). Its random password
  is deliberately NOT in git; it is kept only in the local non-repo file `%TEMP%\tb229r2-acct.txt` on this
  machine. Normal login steps: open `http://127.0.0.1:3000/fa/login` is OTP-only, so use the existing BFF
  password route (`/api/auth/csrf` then `POST /api/auth/login` with `IdentifierKind=Email`).
- Local cart left as the single-line cart `01a17cfd-c1b1-7000-b10d-8cf21258a1cd` v9 (1 line) of that account.
- No migration applied or replayed, no reset/reseed, no user draft/rate/pointer/publication touched, no
  assignment write, no Apply, no user tab read. Admin draft/rates/pointer preserved (untouched).
- The previous local configuration (PID 27588 preview OFF) was replaced by this task-owned preview-ON Host, as
  the task authorizes; it is documented above rather than silently changed.

## E — limits carried forward (not new guarantees)

The R1 recorded limits still stand: the frontend identifier helper still `String`-coerces non-string values, the
calendar decoder's ISO-shape check is not full calendar validity, and the label map keys service/option rather
than owner (two owners with the same binding could share the last label). None was exercised live here.
Compact shared snapshot / per-order-group order evidence and the selection–commit parity slice remain
**unimplemented**; Marketplace cross-owner proof is not implied by this SingleStore trial.

## F — time

13:25 (R2 wake, base fetch) → 15:10 local ≈ **105 min** against HARD20 (environment+login 8 / mounted 8 /
recovery 4). Recorded as **NONCOMPLIANT**, NOT a process PASS. The overrun came from building a task-owned CDP
driver, a cart/merge path that needed the cart-version header, and the destination-gated rendering.

Active task: NONE. Next: Architect review; then the parity slice (selected captured revision + SaveSelection/
Commit) and the compact order-group evidence decision. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
