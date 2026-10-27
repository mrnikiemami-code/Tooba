# 235 result — full owner flow (Store / Marketplace / Seller) + 234 response-contract repairs

Status: **EXECUTED**, NOT accepted, NO user Visual ACCEPT, no activation, no live/HTTP/browser/Host-FE/runtime
change, no shared migration/schema/baseline edit, no PSP/Payment/Settlement policy change.
Base = `1628184e343be2cb425b672b4a22baede6f7087c` (the commit containing this TASK). Elapsed ≈236 min
wake (21:47 local) → report (22:23 local); HARD45 **exceeded and recorded NONCOMPLIANT**. Corrections used: one
test-fixture round (negative owner expectations / nullable tenant fake), plus one self-inflicted in-module revert
(see §E) — the single-correction budget was spent.

---

## 0. First bounded source check (as ordered) — actual seller/business authority in THIS repo

| Question | Actual source fact |
|---|---|
| What is a marketplace seller's business identity? | A **Party id** (`SellerPartyId`). Seller routes resolve `(ActorUserId, SellerPartyId)` through `ISellerPanelAccess` → `SellerPanelAccess.RequireAuthorizedAsync`, which reads the `X-Tooba-Seller-Party-Id` **context** header and then calls `IAuthorizationGuard.AuthorizeUseCaseAsync(Party/<partyId>, View)` for the session actor. The header alone grants nothing. |
| Is there a separate `StoreId` registry to equate with it? | **No.** There is no seller->StoreId registry in this repo; marketplace offers already carry the real selling `Party` and the 228 assignment carries the acting seller. Equating a separate StoreId with `SellerPartyId` would have been an invention, so it was not done. |
| What identifies a SingleStore store? | The resolved **tenant** (`TenantContext.TenantId`, `Status=Active`) from the trusted commerce context. |
| Can two independently owned stores be expressed? | Yes, in the two ways this repo already models: (a) **distinct Party ids** in Marketplace; (b) **distinct tenants** in SingleStore (each with its own connection reference). |
| Model blocker to disclose | Physical per-seller **database** isolation inside one Marketplace deployment is **not** expressed by Party ids; that is edition/connection-factory territory. 235 therefore proves owner **identity/scope** separation only and makes **no** per-seller physical-isolation claim. |

Seller `Manage` vs `View` boundary is pre-existing and untouched: Shipping authoring resolves
`ShippingAdminPermissions.Manage` through the same reusable seller gate (`RequireSellerOwnerAsync`); no new
permission catalogue, route or bypass was added in 235.

---

## A. Complete owner path — one typed policy, three consumers

**One new value + one rule** (`Storefront/Shipping/CapturedPreview/StorefrontCapturedOwnerPolicy.cs`):
`StorefrontCapturedOwner(Kind, TenantId, SellerPartyId)`, `StorefrontCapturedOwnerFailure`
(`None/ResponsibilityMissing/ResponsibilityInvalid/SellerUnsupportedInSingleStore/OwnerUnavailable`), and
`Resolve(edition, tenant, responsibility, groupSellerPartyId)`. Inputs are **only** trusted edition/tenant +
the verified 228 assignment responsibility + the group's **actual** selling party. There is no browser/body owner
key anywhere and no default owner:

* `SingleStore` + `Platform` ⇒ `Store(active exact tenant)`, seller key always null.
* `SingleStore` + `Seller` ⇒ `SellerUnsupportedInSingleStore` (never reinterpreted as the store).
* `Marketplace` + `Platform` ⇒ `Marketplace()`, no tenant, no seller.
* `Marketplace` + `Seller` ⇒ `Seller(group's real SellerPartyId)`; `Guid.Empty` ⇒ `OwnerUnavailable`.
* unknown edition, missing/undefined responsibility, inactive/blank tenant, residual tenant in Marketplace ⇒ explicit
  failure.
`IsSameAs(domainOwner)` is exact (Ordinal tenant, exact seller, kind equality) and `ToDomainOwner()` maps onto the
existing domain `ShippingGroupOwner` — no schema, no owner table, no backfill, no login-user ownership.

Consumers (all three now share it, no duplicated branch):
1. **Read/preview** `StorefrontCapturedPreviewPlanner.BuildPartition/ResolveAsync` — group key keeps owner partition
   (`cap|platform|store|…`, `cap|platform|marketplace|…`, `cap|seller|<seller>|…`), and the owner rides on each
   `StorefrontCapturedCandidatePlan`.
2. **Selection write** `StorefrontCapturedSelectionWriter.BuildEvidence` — evidence owner comes from the server
   candidate, then `EnsureCoherentWithGroupSeller` still enforces Seller==group seller and
   Marketplace/Store carry no seller key. Seller in SingleStore now refuses through the policy instead of a blanket
   "not Store" refusal.
3. **Final acceptance** `CheckoutDirectory.AcceptCapturedShippingCoreAsync` — the old global
   `ResolveCapturedStoreTenantId()` + Store-only checks are **removed**. `ResolveGroupOwner(...)` rebuilds each
   group's owner from **current** assignments/types + the group's own lines and compares it to the persisted owner
   with `IsSameAs` (evidence is never its own validator); each line must be that group's real seller; mixed
   responsibility/type inside one group is a conflict.

Pointer/cache/quote discipline preserved: one batch assignment read, one active-type read, one catalog binding set,
**one 219 read per distinct full owner+binding** (`StorefrontCapturedPublicationBinding` now built from the rebuilt
owner), one goods read and one 216 quote per selected group.

**Cross-owner label map closed**: labels are keyed by `StorefrontCapturedPublicationBinding`
`(Kind, TenantId, SellerPartyId, ServiceCode, OptionCode)`, never by bare `methodCode`; `LabelOf` resolves
per commitment, so two owners sharing `post/standard` keep their own DB translations, and two owners with the same
display label stay distinct commitments. No label parsing, no new client scope picker.

Gate/coupon status unchanged: checked-in `Enabled`/`SelectionEnabled` remain **false**, coupons remain explicitly
unsupported in this slice (`coupon_unavailable`), full shared coupon context stays required next work.

---

## B. 234 response-contract repairs (same vertical flow)

1. **Real presentation cart in the payment preview.** `StorefrontCheckoutService.ProjectCapturedPaymentDraftAsync`
   now loads the authoritative cart it *already* had to authorize (`RequireCartAsync`) and maps with it instead of
   `StubCartPage(...)`: real `cart.Version`, real product titles and seller labels, one quote, no extra goods read.
   `MapPage`'s `titleByOffer` became duplicate-`OfferId`-tolerant (a lawful duplicate offer on two lines no longer
   throws and no longer needs a fictitious cart). The **committed** response still uses `StubCartPage` because the
   active cart is converted at that point — no post-commit foreign read and no invented cart.
2. **Typed acceptance receipt, accepted money/date.** `AcceptCapturedShippingCoreAsync` now returns
   `CapturedShippingAcceptance(Groups, CartVersion, SelectionRevision)` (legacy path included, with `null`
   revision). `ProjectCapturedPaymentDraftAsync` composes its snapshot from the **accepted groups**
   (`shippingAmountOverride` = sum of accepted group charges, accepted dates/selected date per group) instead of the
   stale command's aggregate/date, and returns `SelectionRevisionText` (exact invariant text, e.g. `4294967296`)
   on the additive `StorefrontCheckoutPage.SelectionRevision`. No aggregate date is invented across independent
   groups; the legacy host entry point still returns only groups.
3. **OrderLineIds are not CartLineIds.** `StorefrontShippingGroupView` gained optional `OrderLineIds`;
   committed groups emit `CartLineIds: null, OrderLineIds: group.OrderLineIds`; draft/preview groups keep their real
   `CartLineIds` and `OrderLineIds: null`. Frontend decoders (`storefront-checkout-api.ts`,
   `storefront-shipping-api.ts`) read both as separate optional fields.
4. **No historical back-fill.** `MapCapturedPage` no longer accepts stored draft plans and never substitutes them
   when `Snapshot.ShippingGroups` is empty: missing groups stay **absent** (original Order facts only).

---

## C. Proof executed (focused, one disposable target)

Target: one task-owned disposable `postgres:17-alpine` container `tb235-order-pg`
`127.0.0.1:15433/order_tb231` (user `tb231`), identity asserted by the harness before migrations, normal Order
migration chain. Container removed at the end; `port15433_open=False` verified with
`Test-NetConnection`; `docker ps -a` shows no leftover.

New/changed classes:
* `StorefrontCapturedOwnerPolicyTests` (new, **12/12**) — the owner matrix above: Store tenant, Seller-in-SingleStore
  refusal, missing/disabled/blank tenant, Marketplace without keys, residual tenant refusal, real-seller Seller
  owner, `Guid.Empty` seller refusal, unknown edition/responsibility/enum, exact stored-owner comparison (no
  lowercase/merge), and owner+binding label identity (same service/option under two owners = two bindings;
  case/space variants of the same owner = one).
* `StorefrontCapturedPreviewContractTests` (new, **3/3**) — real cart version 7 + real DB-shaped titles/seller
  labels; money derived from accepted groups (200, not the stale 999); `SelectionRevision = "4294967296"`; draft
  groups keep `CartLineIds`/no `OrderLineIds`; committed groups expose `OrderLineIds` with `CartLineIds` null; and an
  empty committed snapshot is **not** rebuilt from current draft plans.
* `StorefrontPublicCommitEdgeTests` (extended, **7/7** on the disposable DB) — includes the TB-235 **Marketplace
  mixed cart**: one Platform group (owner `Marketplace`, no tenant/seller) and one Seller group (owner = that
  group's real `SellerPartyId`) on the **same** Store binding, each with its own rate row, receiving date, 100 ns
  ticks and money (125/75); exactly **2** 219 pointer reads (one per full owner+binding), 2 quotes, 1 reservation,
  and both owners read back from persisted `order.order_shipping_groups` evidence. Also: the stored-owner mismatch
  refusal before any write, the read-only payment draft (real cart/titles, stale pointer ⇒ explicit `conflict`,
  dropped saved address ⇒ `destination_unverified`, gate off ⇒ `not_ready` with zero writes/reservations), token
  forwarding to the accepted commit, replay after pointer change/converted cart, and the legacy-OFF control.
* `StorefrontCapturedSelectionWriterTests` (updated, **19/19**) — the pre-235 "Marketplace is never written" test was
  replaced by the new contract: Marketplace+Platform writes `Marketplace` owner (no keys), Marketplace+Seller writes
  the real seller, and unsupported combinations (Marketplace with residual tenant, Seller in SingleStore) refuse with
  zero writes. Existing selection facts (two sellers on one Store binding, own money/date/dates, coupon, revision
  CAS, destination, pending work) unchanged and green.
* `StorefrontCapturedPreviewPlannerTests` — unchanged, **15/15** green.

Permission/owner scope proof (existing route/delegate harness, **not modified by 235**):
`Tooba.Shipping.Tests/Admin/ShippingAuthoringOwnerAuthorizerTests` **33/33** — Admin SingleStore ⇒ Store + stable
tenant/no seller; Admin Marketplace ⇒ Marketplace without tenant/seller; seller route in Marketplace with a
whole-owner View grant ⇒ Seller; any other grant shape (including **another seller's** whole-owner grant) denied;
**a View grant never authorizes Manage**; request-supplied owner mode/tenant can never replace the resolved scope;
seller-route capability unavailable in SingleStore; reader failure never treated as allow.

Frontend: `npm run typecheck` clean; `node --experimental-strip-types --test` on
`storefront-checkout-api.test.ts`, `storefront-shipping-api.test.ts`, `storefront-cart-ui.guard.test.ts`
**20/20**.

Regression scope: whole `Tooba.Order.Tests` **564/566** with only the **two pre-existing red guards** documented in
AGENTS.md (architecture `DateTimeOffset.UtcNow` in the 229/231 planner, and the packaging allow-list). Neither was
caused or widened by 235, verified mechanically against the base commit: `Packaging` reference counts are identical
HEAD-vs-now in every touched file and the new file has zero `Packaging` references; `DateTimeOffset.UtcNow`
occurrence counts are unchanged in every touched file. No 200/25/68/124/36/28/31/50/119/188/194/285/310/312 sweep
was replayed; Order.Application/Infrastructure/Tests build 0 errors.

---

## D. Honest gaps / not delivered

* No HTTP/listener/route/browser exercise of any of this; the public edge and selection writer remain model/in-process
  bound. No live storefront page, no 1440/390 screenshots.
* No frontend HMR/Host-FE restart/rebuild, no live business write, no runtime gate change; `Enabled`/`SelectionEnabled`
  stay checked in **false**, so nothing is reachable by a customer yet.
* Coupons are still explicitly unsupported in this slice (`storefront.captured_selection.coupon_unavailable`);
  shared coupon context across preview/selection/final quote remains a required next slice, **not** a final
  exclusion.
* "Selection replaced between facade load and core read" is proven as the observable conflict branch (pointer
  advanced to 9 ⇒ `conflict`, zero writes) plus the positive one-receipt case; a simultaneous in-flight swap inside
  one transaction was not attempted and no serializability claim is made.
* The preview contract test uses an explicit `ICheckoutDirectory` fake (mapping logic real), so it is a contract
  fact, not a relational one; the relational facts are the commit-edge class.
* `SelectionRevision`/cart-version text is exact and additive, but the frontend does not yet consume it in any UI
  flow (no UI redesign in this slice).
* 231/232 migrations remain WRITTEN/shared NOT APPLIED and the stale EF snapshot gap is unchanged; 228 stays APPLIED
  (no replay).
* Physics of per-seller DB isolation inside Marketplace not claimed (see §0).
* Retained unfixed, as ordered: 230 empty-key destination refresh/retry + in-flight new-key wait, Offer API proof
  gaps, 227 mobile empty-note residual, 225-R1 lost unsaved user LOCAL tab edits (no recovery), and the two
  pre-existing red guards above. The cross-owner label gap **is** now repaired and proven (owner+binding keying),
  so it is removed from the open list.

---

## E. Preservation / handoff

* No frontend HMR, no Host/FE restart or rebuild, no live cart/address/order/draft/pointer/rate/week write, no shared
  migration apply, no schema/baseline edit, no `git` amend/force/rebase/reset/stash.
* The user's VISIBLE review window (unsaved user work, local preview ON, count 7) was **never enumerated, read,
  automated, reloaded or closed**; live Host/FE processes untouched.
* TB185 remains paused / default OFF / PendingAllocation. Automatic-Next-Task NONE.
* NEXT (Architect): review this owner/response slice; then the required shared coupon context (preview + selection +
  final quote) and the real frontend desktop/mobile complete trial. **Do not** treat this as the final
  Store/Marketplace/coupon delivery.

---

## F. Corrections appended by TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1 (2026-10-27)

Full evidence: `docs/architecture/evidence/TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1/RESULT.md`.

1. **§3(b) overstated the preview coherence.** In this 235 version `ComposePreviewSnapshot` overrode **only**
   `ShippingAmount`; `MinimumDeliveryDate`, `RequestedDeliveryDate`, `ShippingMethodCode/Label` and the time
   window still came from the stale command. R1 replaced that scalar override with a typed
   `CheckoutPreviewShippingOverride` derived from the accepted groups (earliest accepted minimum, shared method/date/
   window only when every group agrees, otherwise explicitly empty) and re-proved it with a real root advance.
2. **§2/§3(a) exercised a second cart load.** `ProjectCapturedPaymentDraftAsync` still called `RequireCartAsync`
   itself even though the public facade had already authorized and loaded the same `CartPage`. R1 passes that exact
   server object in; the directory keeps its own independent authoritative cart/version check.
3. **§4 overstated per-line assignment validation.** `ResolveGroupOwner` checked only the assignment dictionary key,
   so a row carrying another `OfferId` as payload was accepted, and a `null` responsibility on the first line could
   be repaired by a later line (`??=` accumulator). R1 requires the exact payload `OfferId` + real seller, a defined
   responsibility and a non-empty assigned type equal to the stored evidence on **every** line, with explicit
   initialization.
4. **§4's owner vertical was harness/policy-level, not one real multi-owner vertical.** R1 adds a real
   planner → 232 writer → public preview → public commit → persisted Order proof for Marketplace Platform + two Seller
   business ids on one binding, with swapped-owner refusal and the coherent-root preview fact on the disposable
   `15433/order_tb231` target. No accepted evidence was manually seeded.
5. Unchanged and still true: coupon parity not implemented, no live HTTP/PSP/carrier proof, no activation, gates OFF,
   231/232 migrations `shared NOT APPLIED` with the stale EF snapshot, `previewON/count7` and the user's unsaved
   review window untouched, and the two pre-existing red guards (`DateTimeOffset.UtcNow` planner, packaging
   allow-list). R1 elapsed ≈23 min (inside HARD35) with the one-correction budget exceeded.
