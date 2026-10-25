# TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218-R1 — RESULT

Mode: BOUNDED_REQUEST_WIDE_CAPTURED_REVISION_CONSISTENCY_REPAIR
Base: `3a583ab2c8a173bd4021046036074465218d75d1` (fetch verified, `HEAD == origin/main` before work)
Parent: TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218 (`6b3f3d2e…`, review in
`218/ARCHITECT-REVIEW.md`)

## Verdict

`BOUNDED_CORRECTION_EXECUTED / STILL_NOT_WIRED / STILL_NOT_LIVE`.
The ONE source-confirmed defect is repaired: one selected captured revision per
owner + canonical service/option **across the whole request**, enforced by a typed identity key before
any goods/Cart/Pricing/Shipping call. The accepted composition, its financial/calendar behavior, the 217
reader, the 216 quote seam, the 213 calculator, `OwnerResult`, one clock read and scoped DI are unchanged.

Task text is persisted verbatim in `TASK.md` in this folder.

## Defect (as reviewed)

`StorefrontCapturedGroupProjectionComposer.ValidateCandidates` owned a **per-group** `seenBindings`
dictionary, so group `g1` could request owner X / `express` / `std` at draft D revision 1 while `g2`
requested the same binding at revision 2 — both passed validation and both performed financial/Shipping
work. Its keys were also delimiter-concatenated raw strings, **Ordinal**, and included the group
destination, while the accepted binding comparison (`IsBoundToCandidate`) is trim + `OrdinalIgnoreCase`;
a casing change or a different group destination therefore evaded consistency.

## Repair (only this)

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/CapturedGroupProjection/`

- **New** `StorefrontCapturedPublicationBinding.cs` — one cohesive typed key
  (`StorefrontCapturedPublicationBinding : IEquatable<…>`) over coherent owner kind + tenant + seller +
  canonical service + canonical option. Tenant equality stays **exact** (`Ordinal`; no lowercasing, only
  trim/blank→null through the single `Canonical(...)` policy); service/option use trim +
  `OrdinalIgnoreCase` — the same policy the composer already used to accept an owner answer; an absent
  optional code is the same absent identity. Destination, `MethodCode` display label and group key are
  **not** part of publication identity. `GetHashCode` matches the equality semantics.
- `StorefrontCapturedGroupProjectionComposer.cs` — revision consistency ownership moved out of the
  per-group traversal into `ValidatePlan`, which now holds ONE request-wide
  `Dictionary<StorefrontCapturedPublicationBinding, (Guid DraftId, long Revision)>` and passes it to
  `ValidateCandidates`. `ValidateCandidates` keeps its per-group checks (empty/untrimmed/duplicate method
  code, exact duplicate candidate identity, enum/owner-key coherence, `DraftId`/`SourceRevision`/
  `AvailableDateCount` bounds) and now only consults the request-wide map: same key with a different
  `DraftId` **or** `SourceRevision` ⇒ existing `storefront.captured_group.plan_invalid` before any goods
  reader / Cart / Pricing / Shipping call; same key with the **same** exact draft+revision across groups
  is lawful and simply continues (each group keeps its own facts, destination, and its own
  once-per-candidate quote/calendar). `NormalizeOwnerText` now delegates to the key's single
  `Canonical`, so there is no second normalization policy.

Nothing else changed: no public API/route, no Contracts change, no schema/migration, no publication
pointer/activation, no frontend, no flag, no measurement source, no shared DB, no draft read/write.

## Executed regressions (new focused class)

`Tooba.Order.Tests/Checkout/StorefrontCapturedGroupProjectionRevisionConsistencyTests.cs` — red first,
green after, via the existing harness and the same `ComposeAsync`:

| # | Scenario | Pre-fix | Post-fix |
|---|----------|---------|----------|
| 1 | `g1` owner tenant-a / express / std / rev 1 + `g2` same binding rev 2 | **FAILED** (no rejection; calls happened) | `plan_invalid`, 0 Shipping, 0 Cart reads |
| 2 | Same conflict with `"Express"`/`"STD"` vs `" express "`/`"std"` and different group destinations | **FAILED** | `plan_invalid`, 0 calls |
| 3 | Same binding, different draft at the same revision | **FAILED** | `plan_invalid`, 0 calls |
| 4 | Control: same binding **and** exact draft+revision in `g1` and `g2` | passed | **accepted**, real 99 / 198 facts passed separately, exactly 2 quote calls (not merged), per-group results kept, 1 whole-cart read |
| 5 | Independent owner (tenant-a vs tenant-b) or distinct option (`std` vs `fast`) keeps independent revisions | passed | accepted |

Pre-fix run: `Failed: 3, Passed: 2, Total: 5`. Post-fix: **5/5 passed**.
Full composer area after the repair: **16/16 passed** (11 218 facts + 5 R1 facts), one `Tooba.Order.Tests`
build, 0 errors.

Four 218 fixtures (three in `StorefrontCapturedGroupProjectionComposerTests` and one in the new class) had
been built over two **different drafts of the same owner+service/option binding** — precisely the input the
reviewed rule now forbids. Those fixtures were moved to genuinely independent bindings (distinct service
or option, responses echoing the requested identity); every accepted assertion (99/198 amounts, row and
snapshot provenance, per-day hours, UTC↔local dates, per-candidate isolation, all calendar refusal codes,
token/dependency propagation, DI) is unchanged and still green.

## Limits (unchanged by this repair)

- **NOT WIRED / NOT LIVE**: no Storefront/checkout consumer, no public route/`ISender`, no flag, no
  projection/selection/commit. This primitive is not a live shipping price.
- The plan is still caller-supplied trusted server data: the **selected captured revision** and the
  fulfillment owner/type/membership must come from a reviewed server publication/Offer/fulfillment
  authority (never a browser). No such authority exists in this slice.
- Physical weight/item count remain unknown (`GroupItemCount` always `null`); measurement-gated rows stay
  `MeasurementRequired`.
- No cutoff/slot/holiday/capacity/reservation policy; `MaxDeliveryDays` stays metadata.
- No 194/119/9/43/18/8 battery replay, no solution/Host/backend-wide sweep, no provider/browser/HTTP.
- TB185 paused, default-OFF, PendingAllocation unchanged; §29 decisions and 211–217 not reopened; the real
  user draft/revision/unsaved tab, Host `:5088` and FE `:3000` untouched.

## Time

Actual ≈ 26 min against the 12-min box / 7-min implementation stop (implementation itself finished near
minute 20; the remainder was the forced fixture correction and evidence). Recorded honestly — not a
process PASS.

Next after this closes: server-selected captured revision + actual fulfillment owner/type/membership
authority, then real projection/selection/commit wiring.
`STOP_FOR_ARCHITECT`. Automatic-Next-Task NONE. No 219 envelope.
