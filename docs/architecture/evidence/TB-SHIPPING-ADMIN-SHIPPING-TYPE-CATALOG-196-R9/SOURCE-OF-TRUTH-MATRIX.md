# TB-196-R9 — Source-of-truth matrix: Fulfillment historical schema vs canonical model

Analysis-only artifact. Nothing here is implemented, applied or approved.

## 0. Reviewed baseline and how the set was re-derived

| Item | Fact |
| --- | --- |
| Expected/actual base | `0e53f4603b299b2f5e61f6d4604ac9957a0c109f` == `origin/main` (fast-forward only, no reset/stash) |
| Historical manifest | `docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6/raw/02-historical-legacy-manifest.txt` (192 objects) — disposable PostgreSQL 17 DB built by applying the real chain up to and including `20261016120000_AddFulfillmentShippingGroups` |
| Model manifest | `.../196-R6/raw/03-model-expected-legacy.txt` (216 objects) — relational projection of the then-current design-time metadata |
| Raw object diff | `.../196-R6/raw/04-historical-vs-model-raw-diff.txt` — **16 `missing-in-actual` + 17 `unexpected-in-actual`** (recounted in this task, not taken from prose) |
| Classification files | `04a` substantive (3), `04b` defaults (5), `04c` names (10) |
| Post-R8 revalidation | All 10 model-side names in `04c` were re-checked verbatim against the **R8-aligned** `FulfillmentDbContextModelSnapshot.cs` (lines 141/145/272/276/551/587/684/688/718/753/960) — they are unchanged, so the model-side facts survive R8 and the stale-metadata 191 rename set is **not** a database difference |
| Accounting check | 5 defaults × 2 sides + 10 names × 2 sides + 3 substantive (1 absence + 2 presence-only) = 33 = 16 + 17 ✔ |

**Derived set: 3 substantive + 5 defaults + 10 naming = 18 distinct discrepancies.** No other object difference exists in the saved manifests (no table/column/type/nullability/FK-action/unique/filter difference beyond these).

## 1. Substantive — actually absent or actually present

### S1. `fulfillment.outbox_messages` pending index is missing from the historical database

| Dimension | Fact |
| --- | --- |
| Object | index `fulfillment.ix_outbox_messages_pending` on `fulfillment.outbox_messages (occurred_at)` `WHERE processed_at IS NULL AND dead_lettered_at IS NULL` |
| Historical | **absent** (`02-…`: only `pk_outbox_messages` exists on that table) |
| Canonical model | **present**, declared by the shared mapping `src/backend/BuildingBlocks/Tooba.Persistence/OutboxMessageMapping.cs:43-45` (`HasDatabaseName("ix_outbox_messages_pending")`, filter) which is applied to **every** module that maps an outbox table |
| Provenance | Fulfillment's `20260827010000_InitialFulfillment.cs` creates `outbox_messages` + PK only; every other module's initial migration does create the index (Wishlist `20260825162434:59`, Pricing `20260823085546:67`, CustomerProfile `20260825225000:63`, Geography `20261014113949:318`, Promotion, Catalog …) |
| Behavioral significance | The dispatcher polls with exactly this predicate (`NpgsqlOutboxDispatcherStore` — `WHERE processed_at IS NULL`, ordered by `occurred_at`). Without the partial index every poll degrades to a scan/sort over the whole outbox; the cost grows with processed history. No correctness loss (rows are still selected) but a real latency/load regression and a divergence from the shared outbox contract that all sibling modules honour |
| Confidence | **HIGH** — both sides are raw manifests, provenance is a single missing statement |
| Missing proof | Whether any environment's live DB has the index out-of-band is **UNKNOWN_PENDING_EXECUTION** (no DB allowed in this task) |

### S2/S3. Two `shipping_methods` CHECK constraints exist only in the database

| Dimension | Fact |
| --- | --- |
| Objects | `fulfillment.shipping_methods.ck_shipping_methods_code_canonical`, `…ck_shipping_methods_option_code_canonical` |
| Historical | **present**, created by raw SQL in `20261015120000_AddShippingOneLevelCatalog.cs:31-42` together with the table (`CREATE TABLE IF NOT EXISTS … CONSTRAINT … CHECK`) |
| Canonical model | **absent** — `FulfillmentDbContext` defines no `HasCheckConstraint` for `ShippingMethod`; only the new `ShippingType` tables have modelled checks (`ck_shipping_types_code_canonical`, `ck_shipping_type_translations_name_nonblank`) |
| Exact semantics | code: `length(method_code) BETWEEN 1 AND 32 AND method_code = lower(method_code) AND method_code !~ '[^a-z0-9_:]'`; option: `legacy_option_code IS NULL OR (length(btrim(legacy_option_code)) BETWEEN 1 AND 64 AND legacy_option_code = lower(legacy_option_code))` |
| Application-side equivalent | `ShippingMethod.TryNormalizeCode` (`Domain/Aggregates/ShippingMethod.cs:100-138`): trim + lowercase, `MaxCodeLength = 32`, at most one `:` separator, every segment non-empty and `[a-z0-9_]`. It is **stricter** than the DB check (which tolerates a leading/trailing `:`), so domain-accepted codes always satisfy the DB check |
| Behavioral significance | The DB checks are defence-in-depth for write paths that bypass the domain (raw SQL, backfill, legacy tooling): they guarantee canonical storage even if a caller skips normalization. Dropping them would silently widen what the database accepts; it would not change any existing row |
| Confidence | **HIGH** (constraint text is captured verbatim in the manifest) |
| Missing proof | Whether any production write path bypasses the domain is **UNKNOWN_PENDING_EXECUTION** |

## 2. Defaults — same column, server-side default present only in the database

| Object | Historical | Canonical model | Origin of the DB default | Significance |
| --- | --- | --- | --- | --- |
| `fulfillment.items.quantity_packed numeric(18,6)` | `default 0` | no default | `20260907090000_AddFulfillmentItemQuantityPacked.cs:23` (`defaultValue: 0` on a NOT NULL `AddColumn` over existing rows); later widened to `numeric(18,6)` by `20260909130500` without dropping the default | Insert without the column succeeds and silently stores 0 on historical DBs; on a DB created from the model it would fail NOT NULL |
| `fulfillment.items.quantity_processing numeric(18,6)` | `default 0` | no default | `20260908120000_AddFulfillmentItemQuantityProcessing.cs:23`, same pattern | same |
| `fulfillment.shipments.provider_metadata_version integer` | `default 0` | no default | `20260907121000_AddShipmentProviderMetadata.cs:19-29` — raw `ADD COLUMN IF NOT EXISTS … NOT NULL DEFAULT 0`, required to add a NOT NULL column to a populated table replay-safely (this migration exists precisely because a real environment already had the columns) | same |
| `fulfillment.shipments.shipping_method_code varchar(64)` | `default ''::varchar` | no default | same migration, `DEFAULT ''` for the same replay-safe reason | same |
| `fulfillment.shipments.shipping_method_label varchar(128)` | `default ''::varchar` | no default | same migration | same |

Notes:
* The defaults are **in the migration chain**, so every migrated database (including a fresh one built from migrations) has them. They are invisible to the model, which is why the model-vs-database comparison reports them.
* EF never omits these columns: the properties are ordinary mapped CLR value/reference properties with no `HasDefaultValue`/`ValueGeneratedOnAdd`, so each INSERT sends an explicit value. Today's application behaviour is therefore identical on both shapes; the difference only materialises for callers that omit the column.
* The direction of the difference matters: the historical DB is **more permissive** (silently substitutes 0 / `''`) while the model-only shape is stricter. Neither is automatically "correct"; the model currently describes the stricter contract while the deployed DBs are permissive.

## 3. Naming-only differences — shape verified, identity differs

Each pair below was compared on **full shape**, not just a similar name.

| # | Object kind | Historical name | Canonical model name | Shape (columns / uniqueness / filter / action) | Provenance of the historical name |
| --- | --- | --- | --- | --- | --- |
| N1 | FK `shipping_method_translations` | `fk_shipping_method_translations_method` | `fk_shipping_method_translations_shipping_methods_shipping_meth` (63-char truncated) | identical: `(shipping_method_id)` → `shipping_methods(shipping_method_id)` `ON DELETE CASCADE` | explicit `CONSTRAINT` in `20261015120000:68` |
| N2 | index `consolidated_package_members` | `ix_consolidated_package_members_package_id` | `ix_consolidated_package_members_consolidated_package_id` | identical: `(consolidated_package_id)`, non-unique, no filter | `20260910120000:56` |
| N3 | index `consolidated_package_members` | `ix_consolidated_package_members_shipment_active` | `ix_consolidated_package_members_shipment_id` | identical: `(shipment_id)`, UNIQUE, `WHERE released_at IS NULL` | `20260910120000:59` |
| N4 | index `shipping_groups` | `ix_shipping_groups_fulfillment_group` | `ix_shipping_groups_fulfillment_id_shipping_group_id` | identical: `(fulfillment_id, shipping_group_id)`, UNIQUE, no filter | `20261016120000:39` |
| N5 | index `shipping_method_translations` | `ix_shipping_method_translations_method_lang` | `ix_shipping_method_translations_shipping_method_id_language_id` | identical: `(shipping_method_id, language_id)`, UNIQUE | `20261015120000:72` |
| N6 | index `shipping_methods` | `ux_shipping_methods_legacy_binding` | `ix_shipping_methods_legacy_service_code_legacy_option_code` | identical: `(legacy_service_code, legacy_option_code)`, UNIQUE, `WHERE legacy_option_code IS NOT NULL` | `20261015120000:50` |
| N7 | index `shipping_service_option_translations` | `ix_shipping_service_option_translations_option_lang` | `ix_shipping_service_option_translations_shipping_service_optio` (63-char truncated) | identical: `(shipping_service_option_id, language_id)`, UNIQUE | `20260910170000:76` |
| N8 | index `shipping_service_options` | `ix_shipping_service_options_service_id` | `ix_shipping_service_options_shipping_service_id` | identical: `(shipping_service_id)`, non-unique | `20260910170000:64` |
| N9 | index `shipping_service_options` | `ix_shipping_service_options_service_code` | `ix_shipping_service_options_shipping_service_id_code` | identical: `(shipping_service_id, code)`, UNIQUE | `20260910170000:61` |
| N10 | index `shipping_service_translations` | `ix_shipping_service_translations_service_lang` | `ix_shipping_service_translations_shipping_service_id_language_` (63-char truncated) | identical: `(shipping_service_id, language_id)`, UNIQUE | `20260910170000:46` |

Common cause: those migrations created the objects with **explicit short names in raw SQL**, while the owning entity configurations never pin a name, so EF falls back to the canonical convention name `<ix|fk>_<table>_<columns>`. Both sides are internally consistent; only the identities differ.

Not naming-only (explicitly checked and excluded):
* no column type/nullability difference,
* no ordered-column difference,
* no uniqueness or partial-filter difference,
* no FK target column or delete-action difference,
* no table/schema difference.

Runtime/behavioural impact of N1–N10: **none for query execution** — PostgreSQL resolves plans by OID, and no production code or test matches these names. Two secondary effects:
1. Constraint names surface in driver error details (`23503`/`23505`), so a rename changes error text seen by operators/logs (not by API contracts, which map error codes).
2. Because the model snapshot now records the convention names, the model is *stable* (no churn when future migrations are added); but any future re-scaffold from a live database would emit the short names, and any manual materialisation of the R6 full-schema gate will keep reporting 10 name rows until either the DB or the model is aligned.

## 4. Explicitly not discrepancies

* The **191 rename operations** repaired in R8 were stale *metadata* naming, not historical database objects. They must not be counted here.
* The `quantities`/`shipments` columns exist on both sides with identical type/nullability; only their defaults differ (§2).
* `shipping_methods` itself, its unique `ix_shipping_methods_method_code`, `ix_shipping_methods_legacy_service_code`, `ix_shipping_groups_fulfillment_id`, `ix_items_shipping_group_id` and all other manifest objects are equal and are not listed.
