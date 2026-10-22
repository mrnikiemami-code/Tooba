# TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191 — evidence

Parent: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2
Mode: BOUNDED_OWNER_GROUP_MANUAL_RATE_ROW_AUTHORING_PERSISTENCE
Base / HEAD at execution: `ebe9d10d3db0cf406f604fef4f2d650a15e2a554` (`HEAD == origin/main == Expected-Base-SHA`)

This slice adds the persisted manual condition/cost/delivery row collection under the ALREADY ACCEPTED
owner/region draft aggregate. It adds no cart/parcel evaluator, no quote, no publish/activate and no
frontend. The runtime merchandise basis stays an OPEN integration gate (section 8).

## 1. Model

| Concern | Decision |
| --- | --- |
| Owning aggregate | `ShippingAuthoringDraft` (root). `ShippingAuthoringRateRow` is a dependent of `ShippingAuthoringRegionGroup`; no new aggregate, no new bounded context. |
| Row identity | Server-created `rate_row_id` (`Guid`, `ValueGeneratedNever`, assigned in the domain factory). A retained id is accepted only when it is a real row of the SAME group and appears once. |
| Geography | Zero geography columns/inputs on a row. `group_id` is the sole geographic reference; a row can never be loaded by row id alone. |
| Row order | No ordering column and no unique ordering index: rows are alternatives, so ordering would create the same fixed-swap problem R2 removed and has no pricing priority. |
| Condition axes | Weight `bigint` grams, item count `int`, merchandise amount `numeric(18,2)`; each optional half-open `[min, max)`, `min` present when the axis is conditioned, `max` optional (unbounded). |
| Costs | `freight_amount` and `packaging_amount`, both `numeric(18,2)`, non-negative, independent. |
| Duration | `min_delivery_days`/`max_delivery_days` integers, `0 <= min <= max <= 365`, equal allowed. Separate from preparation time, carrier hours and customer promise. |
| Currency | `authoring_region_groups.currency_code` (`varchar(3)`, nullable, canonical upper-case when present). Bound on the first NON-EMPTY save, never cleared when rows are removed. |
| Limits | `MaxRowsPerGroup = 200` (payload limit), weight `<= 1_000_000_000` g, count `<= 1_000_000`, amount `<= 1_000_000_000`, days `<= 365`. No commercial cap is invented. |

## 2. API

| Method + route (also under `/v1/seller/...`) | Permission | Request | Response |
| --- | --- | --- | --- |
| `GET /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/region-groups/{groupId:guid}/rate-rows` | `fulfillment.view` | `GetShippingAuthoringGroupRateRowsQuery` (route values only) | `ShippingAuthoringGroupRateRowsItem`: draft id, group id, root revision, stored currency (nullable, truthful), rows |
| `PUT` same path | `fulfillment.manage` | `ShippingAuthoringRateRowsReplaceRequest` (`expectedRevision` + `rows`) | Same item for the persisted state |

- One Application query and ONE command/handler serve both audiences; endpoints only build the server-authorized
  `ShippingAuthoringOwnerScope` and dispatch through `ISender`.
- Owner scope, draft and group are resolved together in Infrastructure; a foreign/invented draft or group is the
  same canonical 404 (`shipping.authoring.draft_not_found` / `shipping.authoring.region_group_not_found`).
- `rows: null` is rejected by the handler (defense in depth) and by FluentValidation; `rows: []` is a valid
  incomplete configuration, never zero/free/unrestricted coverage.
- SingleStore seller keeps the existing 404 capability outcome before any seller resolution.

## 3. Range and overlap matrix

| Case | Expected | Proven by |
| --- | --- | --- |
| weight `[0,1000)` + `[1000,5000)` | not an overlap (adjacent half-open) | `Half_open_adjacent_splits_on_every_axis_are_not_overlaps` |
| count `[1,4)` + `[4,11)` on the same weight band | accepted | same |
| amount `[5000,10000)` + `[10000,20000)` | accepted | same |
| identical rows (even with equal costs) | `shipping.authoring.rate_overlap` | `Full_tuple_overlap_identical_rows_and_unconditioned_overlap_are_refused` |
| overlapping weight + count + amount product | `shipping.authoring.rate_overlap` | same |
| one unconditioned row + any other row in the group | `shipping.authoring.rate_overlap` | same |
| overlap on only ONE or TWO axes | accepted (alternatives) | same |
| max-only / `min == max` / inverted / negative integer axis | `shipping.authoring.rate_condition_invalid` | `Invalid_rows_are_refused_before_any_mutation_of_this_aggregate` |
| count minimum `0` | rejected (count min must be `>= 1`) | same |
| fractional / over-precision money | `shipping.authoring.rate_amount_invalid` | same |
| negative freight or packaging | `shipping.authoring.rate_amount_invalid` | same |
| `min_delivery_days > max_delivery_days` or `> 365` or `< 0` | `shipping.authoring.rate_duration_invalid` | same |
| `min_delivery_days == max_delivery_days` | accepted | `Equal_delivery_duration_is_allowed_and_stays_separate_from_preparation` |
| foreign / unknown / duplicate retained row id | `shipping.authoring.rate_row_payload_invalid` | `Foreign_unknown_and_duplicate_retained_row_ids_are_refused` |
| `> 200` rows | `shipping.authoring.rate_row_payload_invalid` | same |

## 4. Currency and precision

- The trusted currency is read from `IShippingAuthoringContextSource` (TB-188 Contracts-only seam over
  StoreContext) BEFORE any tracked write; no route/body/query/locale/country value can supply it, and no IRR
  literal exists in the path.
- Edition not SingleStore/Marketplace, or no trusted currency: `shipping.authoring.configuration_unavailable`
  (503) and nothing is written.
- First non-empty save binds the group currency; a later write with a different trusted currency is
  `shipping.authoring.rate_currency_mismatch` (409). Old amounts are never reinterpreted or converted, even
  after the collection is cleared.
- Money is decimal end to end with the canonical `numeric(18,2)` storage; the canonical domain money rule
  rejects a value that PostgreSQL would otherwise round into a different threshold or fee.

## 5. Atomicity, revision and interaction with the region tree

- `ReplaceGroupRateRowsAsync` uses ONE implicit EF `SaveChangesAsync` boundary: no `BeginTransaction` is added
  in this path (R2's reviewed transaction site remains the only one in the module).
- Preflight runs inside the aggregate BEFORE the first mutation; a failure leaves rows, currency, `updated_at`
  and the root revision untouched in the same context, and the tracker is cleared on any failure so a later
  `SaveChangesAsync` cannot publish a rejected edit.
- A row write and the region-tree replacement share the SAME root `revision` concurrency token, so neither can
  silently overwrite the other; loss gives the canonical 409 and leaves no pending edit.
- Region replacement preserves rows and currency of retained groups across reorder/selection edits because the
  stable `group_id` is untouched. Explicit group removal deletes only that group's dependent rows via the own
  cascade FK.

## 6. Migration (additive, prepared only)

`20261022065333_AuthoringGroupRateRows` (+ Designer + snapshot): adds `shipping.authoring_rate_rows`
(`pk_authoring_rate_rows`, `fk_authoring_rate_rows_authoring_region_groups_group_id ON DELETE CASCADE`,
`ix_authoring_rate_rows_group`), the seven storage guards mirroring the domain rules
(`ck_authoring_rate_rows_{weight_interval,count_interval,merchandise_interval,freight_non_negative,packaging_non_negative,amount_within_max,duration_range}`)
and the nullable `shipping.authoring_region_groups.currency_code` with
`ck_authoring_region_groups_currency_canonical`. No legacy table is altered, no historical migration/designer is
edited, no external FK, no backfill. DDL was applied ONLY to the disposable task database; no shared database,
MigrationRunner or Host startup migrator was executed and no deployment flag changed.

## 7. Commands and counts

| Command | Result |
| --- | --- |
| `dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj` | 0 errors |
| `dotnet test ... --filter "FullyQualifiedName~Authoring&FullyQualifiedName!~Relational"` | 130 passed / 0 failed |
| `dotnet test ... --filter "FullyQualifiedName~Relational"` (live PostgreSQL) | 21 passed / 0 failed |
| `dotnet test src/backend/Modules/Shipping/Tooba.Shipping.Tests` (whole Shipping suite, live DB still up) | 574 passed / 0 failed / 0 skipped |

New coverage added for this slice: 8 domain facts (`ShippingAuthoringGroupRateRowTests`), 4 HTTP/pipeline facts
(`Group_rate_rows_roundtrip_...`, `Missing_null_overlapping_and_invalid_last_rows_...`,
`Trusted_currency_change_fails_closed_...`, `Foreign_owner_view_only_and_single_store_seller_denials_...`) and 4
relational facts (`Rate_rows_roundtrip_bind_the_currency_...`, `The_rate_row_write_is_one_atomic_boundary_...`,
`A_same_revision_rate_row_race_...`, `Retained_group_rate_rows_survive_a_region_reorder_...`).

Relational facts executed against the real provider, real module migrations and the real directory: roundtrip
through a NEW context (ids/costs/currency/bounds/duration), retained-id identity, currency binding without
rebinding after clearing, mismatch fail-closed, injected failure inside the single `SaveChanges` (nothing
published, retry succeeds once), deterministic two-reader pre-write barrier race on the shared root revision
(one winner, loser holds no pending edit), retained rows across reorder and cascade deletion on group removal.

Classification manifest updated to the real universe: 31 shipped routes, 24 reachable requests, 21 request
validators, 6 nested transport validators, 27 declared validators, with a TB-191 amendment recording the
Contracts-only currency provenance. Error catalog tests now pin 49 legacy 400 codes and exactly three 404
(draft, owner-scope capability, region group) plus the two 409 codes; no legacy status was re-classified.

## 8. Open gate (explicitly NOT closed here)

`Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED`. The persisted amount/count bounds are
threshold CONFIGURATION only. Which authoritative before/after-discount/tax basis and which basket-to-group
allocation fills them at runtime is unresolved; `CartSubtotalExclusive`, whole-cart totals and
`ShippingMeasuredParcel.ItemCount` (physical parcel units) are NOT proven mappings. Any future evaluator,
customer quote or activation must stay blocked until that gate is resolved by the Architect.

## 9. Isolation and cleanup

Disposable task-owned container `tb190r1-postgres` (`postgres:17-alpine`) published only on
`127.0.0.1:55432`, database `shipping_tb190r1`, role `tb190r1`; the suite re-parses the connection string with
`NpgsqlConnectionStringBuilder` into test-owned constants and asserts live `current_database()`/`current_user`
before any write. Shared `:5432`/`tooba_alpha`/`store-alpha`, shared apps/ports, Redis/RabbitMQ and other
people's dirty/untracked work were not touched. The container is removed after the runs; no listener remains on
`55432` and no credentials were logged.

## 10. Honest limits

- No quote/evaluator, no free/zero-rate activation, no preparation/schedule/carrier coupling, no publish, no
  frontend editor, no module recertification and no manifest promotion.
- `Worker PASS` is technical evidence only; Architect ACCEPT is not claimed.
- Whole-repository `Tooba.Host.Tests` was also executed once for unrelated regression awareness: 2540 passed /
  132 failed / 2672 total. Every failure is outside the Shipping module (Architecture guards for
  Catalog/Content/StoreAppearance/Pricing/Wallet/Wishlist certification state, plus Content/Fulfillment/Order/
  Settlement/Product suites) and none references the changed Shipping files or routes; they are pre-existing
  environment/manifest-state failures, not a Shipping regression. They are reported here rather than hidden.
