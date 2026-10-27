# TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229-R1 — RESULT

Task: `TASK.md` in this folder (verbatim, issued by the Architect).
Parent: TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229 (`../TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229`).
Reviewed source: `ad599cbcd` (Architect review of 229); base for this work: `304ac4da8059e91673cd54306e0930b9bfd94378`.
Mode: BOUNDED_DEFECT_REPAIR + REAL LOCAL TRIAL.
Verdict: **EXECUTED — PARTIAL**. All four source-confirmed defects are repaired with focused facts and a
single UI surface, but the REQUIRED live desktop/390 observation of the existing cards **was not obtained**:
the storefront shipping route needs an authenticated buyer session this task does not have.

## A — money/strict decode (defect 1)

`storefront-shipping-api.ts` now decodes `capturedPreview` with the same strictness the rest of the projection
uses, and never launders money through `Number`:

- amounts are read as **exact decimal wire text** (`"99.00"`, `"198.00"`, `"0"`, `"2000.50"`) and kept as text;
  no `parseFloat`, no numeric round-trip, no reformatting;
- an absent key and an explicit `null` stay distinguishable; **absent goods/money stays `null`** (unknown), it is
  never rendered as `0`;
- an **invalid** amount (empty, non-numeric, exponent, sign-only, separated junk) makes the whole preview
  `untrusted` — it is never coerced to `0` and never silently turned into another amount;
- an **explicit `0`** is lawful and decodes as `"0"` (distinct from `null`);
- currency comes only from the payload; a missing/foreign currency is `untrusted`, never a hardcoded `IRR`;
- malformed/duplicate/missing group identity, duplicate method identity inside one group, and duplicate or
  malformed dates are `untrusted` rather than silently dropped.

`storefront-shipping-format.ts` only formats that already-decoded text for display.

## B — method identity is not a label (defect 2)

The planner and the decoder now key a method by its **stable catalog binding** (`serviceCode` + `optionCode`,
OrdinalIgnoreCase casing-insensitive, one pointer read per distinct binding) and keep `methodLabel`
**separately** as presentation text. Two distinct methods that happen to share one translated label therefore
both survive and stay independent; a duplicate of the *same* binding inside one group is untrusted. The
request-wide publication identity stays the typed `StorefrontCapturedPublicationBinding` (owner kind +
exact tenant + seller + canonical service/option) — destination, display label and group key are still
not publication identity.

## C — one existing UI presentation (defect 3)

The parallel `storefront-shipping-captured-preview.tsx` component was **deleted**. The captured preview is
rendered **inside the existing `StorefrontShippingGroupCards`** through one optional prop, so there is one
card surface, not two:

- single-line and multi-line carts both use the same cards (`isGroupedCart || projection.capturedPreview !== null`);
  the legacy duplicate summary/row is suppressed while the captured surface is active (`—` in the old
  amount cell instead of a second number), so no duplicate legacy cards;
- unknown goods amounts render an honest missing marker, never `0`; explicit `0` renders `0`;
- per group the card shows its own seller/type, its post-discount tax-inclusive goods amount, that method's own
  freight + packaging, that method's own dates, and on date change **that date's own stored interval**;
- an unavailable group stays visible with one short localized reason; an `untrusted` preview shows one
  localized warning and no numbers rather than a misleading half-rendered card.

## D — plan honesty (defect 4)

- `StorefrontCapturedPreviewOptions.AvailableDateCount` is an **explicit validated preview policy** (`0..90`,
  required when `Enabled`); it is never re-derived from `ShippingMethodsOptions.DeliveryHorizonDays`. Enabled
  with a missing/out-of-range count refuses **before any read**. Checked-in `Enabled` stays `false`.
- an unknown/undefined `FulfillmentResponsibility` value is **refused**, never defaulted into the Store/Platform
  partition branch;
- contradictory assignment/type rows and out-of-request rows fail closed;
- a known business composition refusal surfaces a precise `Refused` view (group stays visible with its reason)
  instead of erasing the cart into an empty success; dependency exceptions and the caller's cancellation
  **propagate** unchanged instead of being recast as missing assignment;
- `SaveSelectionAsync` and `CommitAsync` both refuse on `storefront.captured_preview.not_ready` as their first
  statement when the option is ON, before cart read/selection/draft/order/reservation/payment/cart conversion;
  with the option OFF both keep the existing path (no preview refusal).

The 218 composer is **not** relaxed: empty `Candidates` stays lawful, groups are never removed, and there is
still ONE 217 goods read, ONE 218 composition, one 219 read per distinct typed binding and one 216 quote per
candidate. No new price/working-day algorithm.

## E — executed proof

Focused only (no full Offer/194/188/solution/Host confidence loop).

- `dotnet test … --filter "FullyQualifiedName~StorefrontCapturedPreview"` → **18/18 passed**
  (15 `StorefrontCapturedPreviewPlannerTests` + 3 `StorefrontCapturedPreviewWriteGuardTests`), including the
  required **positive composition** driven through the REAL 218 composer and the REAL 217
  `CheckoutDirectory` harness: two groups with real goods amounts **99.00 / 198.00**, independent fees and
  weeks, a third unassigned group still visible without erasing the quoted groups, two distinct bindings that
  share one translated label both surviving, case variants collapsing to ONE pointer read for ONE candidate,
  and dependency failure propagating instead of an empty success.
- `node --experimental-strip-types --test app/storefront/storefront-shipping-captured-preview.test.ts`
  → **11/11 passed** (invalid amount never 0, explicit 0 lawful, exact decimal text, absent ≠ untrusted,
  unknown goods stay null, missing/foreign currency untrusted, malformed/duplicate group·method·date untrusted,
  two bindings sharing a label both survive).
- `npx tsc --noEmit` → clean (exit 0), run once on final source.
- Backend compile: the test project build that ran these facts compiled the changed Order projects with
  **0 errors**.

HONEST: the write-guard facts exercise the real service methods (not source regex), but they are **not** an
end-to-end HTTP/order test.

## F — REAL LOCAL TRIAL (executed, blocked by buyer authentication)

- Runtime identities verified before touching anything: Host on `127.0.0.1:5088` = PID 27588 from
  `D:\temp\tb221-build\out\Tooba.Host.exe` (preview OFF), FE `127.0.0.1:3000` = PID 28288 (node), `/health` 200.
- Current source was built once into an isolated output (`D:\temp\tb229r1-build\out`) and that Host was run on
  the same `127.0.0.1:5088` with `Tooba__StorefrontCapturedPreview__Enabled=true` and an explicit
  `AvailableDateCount` (preview policy value, not the method horizon). No migration, no reset, no reseed.
- What the live page actually did (real traffic, receipts in `D:\temp\tb229r1-build\host-out.log`):
  the storefront shipping page redirects to `/fa/login?returnTo=/fa/shipping` (the trial browser ended on the
  login page, tab titles «ورود | توبا»), and the page's
  `POST /v1/storefront/shipping/projection` returned **401 `application/problem+json`** with
  `ErrorCode=checkout.authentication_required` (also logged: `Commerce resolution failed` for the same trace).
  That is the planner's documented honest refusal for an unauthenticated cart, but it means the captured cards
  could **not** be observed live: no authenticated buyer session exists for this task, and no credentials were
  used or invented.
- Therefore **no desktop/390 screenshot of the existing cards exists** and the live duplicate-card /
  no-horizontal-overflow confirmation is **NOT obtained**. Zero screenshots are claimed for this task.
- The original runtime was restored: the 229-R1 Host was stopped and PID 27588
  (`D:\temp\tb221-build\out`, preview OFF) is serving `5088` again, `/health` 200. The checked-in default stays
  OFF; no local preview is left enabled.
- No Apply/publication/order/reservation/payment/cart conversion, no user draft/rate/pointer edit, no
  assignment write (none was needed to reach the blocker), no user tab was read or reloaded.

## G — not claimed

Not claimed: Architect acceptance; any user Visual ACCEPT; a live positive quote or live captured cards;
projection/selection/commit parity; the compact shared-snapshot order evidence; seller-admin entry; any
price/eligibility/activation/readiness statement. The single UI surface and strict decode are proven by
focused facts and source, not by a live render.

## H — time

Start 13:25 → 14:04 local ≈ **39 min** against the HARD35 box (source stop 20, final 15). Recorded as
**NONCOMPLIANT**, NOT a process PASS. Overall 229 lineage remains unaccepted.

Active task: NONE. Next: Architect review only — the blocking gap is a live authenticated-buyer observation
desktop+390 (needs a task-scoped buyer session or explicit credentials), then the parity slice
(selected captured revision + SaveSelection/Commit) which may replace the `not_ready` refusal, and the
compact order-group evidence decision. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
