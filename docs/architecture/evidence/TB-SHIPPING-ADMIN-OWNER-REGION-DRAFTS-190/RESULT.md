# TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190 — evidence

**Task:** `TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190` (parent `TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189`)
**Channel:** `tooba-main` · **Worker:** `tooba-worker-01` · **Mode:** `BOUNDED_OWNER_ISOLATED_METHOD_DRAFT_AND_REGION_GROUP_PERSISTENCE`
**Expected base:** `676b7efcb9d4f402e662d5f955e82a33b252c835`
**Prepared by:** Cursor agent, 2026-10-22 (≈19 min of the 45 min box)
**Verdict scope:** PASS for the bounded persisted draft slice; real-PostgreSQL execution and the full editor remain **PENDING**.

---

## 1. What this slice delivers

The first **persisted** part of the Shipping authoring model:

* an owner-isolated `Draft` that binds an **existing** Fulfillment method identity (`ServiceCode` + optional `OptionCode`);
* multiple ordered region groups per draft, each selecting multiple destination nodes;
* geography stored **once per group** (never per future rate/packaging/schedule row);
* whole-tree create/read/replace with optimistic concurrency;
* additive Shipping-owned persistence (3 new tables), prepared migration only.

It is **not** activation, pricing, quotes, packaging, schedules, translations or the full editor.

---

## 2. Aggregate / table / owner map

| Domain | Table (`shipping` schema) | Identity / keys | Notes |
| --- | --- | --- | --- |
| `ShippingAuthoringDraft` (root) | `authoring_drafts` | `draft_id` PK | owner key, `service_code`, `option_code` (nullable = service level), `revision` (concurrency token), `created_at`, `updated_at` |
| `ShippingAuthoringRegionGroup` | `authoring_region_groups` | `group_id` PK, `draft_id` FK | `group_order` (explicit order); stable group identity is the future reference for rate rows |
| `ShippingAuthoringSelectedArea` | `authoring_selected_areas` | `selected_area_id` PK, `draft_id` + `group_id` FK | `node_kind` (Country/AdministrativeArea/City/ServiceArea) + `node_id` — typed identity, never a translated label |

Owner representation (`ShippingDraftOwner`, mirrored `ShippingAuthoringOwnerScope`):

| Owner kind | `owner_tenant_id` | `owner_seller_party_id` |
| --- | --- | --- |
| `Store` | real `TenantId` | `NULL` |
| `Marketplace` | `NULL` | `NULL` |
| `Seller` | `NULL` | real `SellerPartyId` |

Enforced by `ck_authoring_drafts_owner_coherent`; the adapter maps the **server-authorized** TB-189 scope into this representation. Domain has no reference to `Shipping.Application` and no authorization concept.

Binding uniqueness (PostgreSQL `NULLS NOT DISTINCT`, so service-level `option_code IS NULL` still conflicts):
`ux_authoring_drafts_owner_binding (owner_kind, owner_tenant_id, owner_seller_party_id, service_code, option_code)`.
Group/area uniqueness: `ux_authoring_region_groups_draft_order`, `ux_authoring_selected_areas_draft_node`.

Owner-scope filter is applied in **every** query (`ListAsync`, `GetAsync`, `CreateAsync`, `ReplaceRegionGroupsAsync`) — loading by `draftId` alone never trusts the caller. A group/area from another draft cannot be re-attached (`ShippingAuthoringDraft.ReplaceRegionGroups` rejects an unknown group id).

---

## 3. Trusted method + geography facts

* **Method binding**: reused the existing `IShippingMethodOptionValidator` (`FulfillmentShippingMethodOptionValidator` over Fulfillment Contracts). No second catalog, no display-code parsing, no hardcoded option codes, no translated labels as identity.
* **Ancestry proof**: `ShippingAuthoringDestinationResolver` walks the authoritative chain (country → administrative-area parents → city → service area) via `IGeographyLookup` snapshot reads (`ListCountriesAsync`, `ListAdministrativeAreasAsync`, `ListCitiesAsync`, `ListServiceAreasAsync`). `GetByIdsAsync` alone is never treated as proof of a complete chain.
  * The submitted administrative parent of a city/service area must agree with the real city fact.
  * Cycles are rejected (test drives Tehran→Karaj→Tehran).
  * Missing/inconsistent authority → `shipping.destination_scope.invalid`; the server never guesses.
* **Overlap rule** (`ShippingDestinationSelectionOverlap`, pure domain): on the trusted paths, any duplicate **or ancestor/descendant** pair across **all groups of the same draft** is refused (`shipping.authoring.region_selection_overlap`). Country ⇒ descendants conflict; province ⇒ its city/area conflicts; independent branches (Qom vs Isfahan, Golpayegan vs Qom) are allowed. Same GUID in **different node kinds** is a different destination (covered by a fixture).
* **Tracing**: each foreign Geography read goes through the canonical `IModuleCallTracer` span; tags carry source/target/operation only — no destination, owner, actor or user names.

---

## 4. HTTP + CQRS surface (8 new routes, 2 shared use cases per operation)

| Method | Admin | Seller | Permission | Validator |
| --- | --- | --- | --- | --- |
| POST | `/v1/admin/shipping-services/authoring-drafts` | `/v1/seller/shipping-services/authoring-drafts` | `fulfillment.manage` | required |
| GET (list) | `…/authoring-drafts` | `…/authoring-drafts` | `fulfillment.view` | not required |
| GET (one) | `…/authoring-drafts/{draftId:guid}` | same | `fulfillment.view` | required |
| PUT | `…/authoring-drafts/{draftId:guid}` | same | `fulfillment.manage` | required |

* POST returns **201 + canonical `Location`** for that audience (`/v1/admin/...` or `/v1/seller/...`) with the real new id/revision.
* Reads and writes authorize **independently** before dispatch; `view` never implies `manage`.
* SingleStore seller routes are `404 shipping.authoring.owner_scope_unavailable` before seller resolution or dispatch (no admin bypass, no browser-selected seller).
* Owner/mode/tenant/seller are never bound from body/query/route/headers.
* Missing **and** foreign draft ids return the same safe `404 shipping.authoring.draft_not_found`; a stale `expectedRevision` returns `409 shipping.authoring.draft_revision_conflict`.
* Request classification: 27 shipped routes / 22 reachable requests (19 validator-required, 3 `NO_VALIDATOR_REQUIRED`), guarded by `ShippingRequestClassificationTests` and the route-universe equality test.

New stable errors (en + fa resources): `draft_not_found` (404), `draft_revision_conflict` (409), `draft_binding_duplicate` (400), `region_selection_overlap` (400), `region_payload_invalid` (400). The 44 legacy 400 errors and TB-188/189 statuses are unchanged.

---

## 5. Atomicity / concurrency / retry matrix

| Scenario | Behavior | Proof |
| --- | --- | --- |
| Whole-tree create | one `SaveChanges` for root + groups + areas | `Real_create_is_owner_isolated_and_survives_a_new_context_readback` |
| Valid replace | revision advances, stable group ids preserved when submitted | `Replace_keeps_stable_group_identity_and_bumps_the_revision` |
| Late invalid group | nothing cleared/reparented; prior snapshot + revision intact | `A_late_invalid_group_leaves_the_previous_snapshot_and_revision_intact` |
| Stale `expectedRevision` | `409`, state unchanged | same test |
| Identical retry with current revision | no duplicated groups/areas (tree replacement, not append) | same test |
| Optimistic concurrency | `revision` is the EF concurrency token with a real relational predicate (`WHERE revision = @expected`) | `ShippingAuthoringDraftDirectory`, persistence-model guard |
| Unique-race | `DbUpdateException` translated to `draft_binding_duplicate`, never raw provider text | directory adapter + error catalog |

**InMemory limits (explicit):** the behavioural tests run on EF InMemory. They do **not** prove PostgreSQL transaction rollback, unique-index races or row-lock behaviour. Those are covered structurally by the generated relational model/script guard (`ShippingPersistenceModelTests` generates the Npgsql idempotent script and asserts the three `CREATE TABLE`s, five check constraints, `NULLS NOT DISTINCT` unique index, and draft→group→area cascade FKs) — the runtime gate stays **PENDING**.

---

## 6. Additive migration — prepared only

* `20261022055446_AuthoringDraftPersistence.cs` + `.Designer.cs` + updated `ShippingDbContextModelSnapshot`.
* Scope: **only** the three new tables, their checks, indexes and internal FKs. All six earlier migrations are byte-identical; no legacy table/index/constraint altered, no backfill, no cross-module FK.
* **No DDL was executed.** The Host was not started, `MigrationRunner` was not run, `:5432/tooba_alpha` was not touched, no deployment flag changed, no schema migrator registered. CLI-only application is preserved and the stale `ShippingDbContextFactory`/module remarks were corrected comment-only.
* Legacy policy/catalog/coverage/packaging/schedule rows and all default-OFF gates (`ShippingOperationalShadow`, `PrerequisitesConfirmed`, TB-185 pause, PendingAllocation) are untouched.

---

## 7. Commands and counts

```text
dotnet build  src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj   -> 0 errors
dotnet test   … --filter "FullyQualifiedName~ShippingAuthoringDraftEndpointTests"            -> 3 passed
dotnet test   … --filter "FullyQualifiedName~ShippingRequestClassificationTests|…StructureGuardTests" -> 18 passed
dotnet test   src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj   -> 526 passed, 0 failed
```

Focused coverage added in this task:

* `Authoring/ShippingAuthoringDraftTests.cs` (5): overlap across groups, stable identity/revision, owner-isolated real store roundtrip + new-context readback, rollback on late invalid group, deep ancestry + cycle.
* `Authoring/ShippingAuthoringDraftEndpointTests.cs` (3): real composition POST → 201 + Location, own GET 200, other-store GET/PUT 404 with unchanged tree, view-only 403 before dispatch with nothing persisted, SingleStore seller 404 with nothing persisted.
* Persistence/classification/structure/error guards extended for the new tables, routes, requests and resources.

---

## 8. Honest scope and remaining gates

| Gate | Status |
| --- | --- |
| Real authorized draft CRUD + store roundtrip (InMemory) | PASS (this task) |
| Trusted multi-region grouping + cross-group ancestry exclusion | PASS |
| Prepared additive migration + relational model/script review | PASS (prepared, not applied) |
| Real PostgreSQL migration/transaction/unique execution | **PENDING** |
| Full editor (rates, metadata, modes, schedules, translations) | NOT_IMPLEMENTED |
| Publish/activate/quote/customer consumer | NOT_IMPLEMENTED — drafts are incomplete configuration, never free/unrestricted coverage |
| Production functional acceptance | **PENDING** |

No shared database writes, no data/flag changes, no certification claim, no next task self-authorized.
