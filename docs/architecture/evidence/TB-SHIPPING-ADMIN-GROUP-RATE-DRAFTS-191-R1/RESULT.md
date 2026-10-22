# TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1 — evidence

Parent: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191
Mode: BOUNDED_RATE_STORAGE_CHECK_PARITY_AND_CROSS_OPERATION_RACE_REPAIR
Base: `b77492052bb2926c522bb23c8cb7e62dfc717675` (`HEAD == origin/main == Expected-Base-SHA` before edits)

Claim UTC `2026-10-22T07:53:21Z` · deadline `08:28:21Z` (35 min HARD) · result UTC in the result block.
No new implementation/test started after minute 30. No Host/other-module build or test was run.

## 1. R191-01 proof: the old predicate really was accepted

Executed on the disposable PostgreSQL 17 instance (read-only truth probe, no data written to a real table):

```sql
SELECT ((NULL::bigint IS NULL AND 500::bigint IS NULL)
        OR (NULL::bigint >= 0 AND NULL::bigint <= 1000000000
            AND (500::bigint IS NULL OR (500::bigint > NULL::bigint AND 500::bigint <= 1000000000))))::text;
--  (empty)  => NULL / UNKNOWN
```

A temporary table carrying exactly the old TB-191 shape then accepted the max-only row, because a
PostgreSQL CHECK rejects only FALSE and tolerates UNKNOWN:

```sql
CREATE TEMP TABLE probe_old(v bigint, c bigint);
ALTER TABLE probe_old ADD CONSTRAINT ck_old
  CHECK ((v IS NULL AND c IS NULL) OR (v >= 0 AND (c IS NULL OR c > v)));
INSERT INTO probe_old VALUES (NULL, 500);   -- INSERT 0 1
SELECT count(*) FROM probe_old;             -- 1
```

## 2. Defect → correction matrix

| # | Old predicate (TB-191) | Accepted-invalid example | Corrected predicate |
| --- | --- | --- | --- |
| 1 | `(min IS NULL AND max IS NULL) OR (min >= 0 AND ...)` | weight `min NULL / max 500` | `(min IS NULL AND max IS NULL) OR (min IS NOT NULL AND min >= 0 AND min <= 1e9 AND (max IS NULL OR (max > min AND max <= 1e9)))` |
| 2 | same shape for count | count `min NULL / max 5` | `... OR (min_item_count IS NOT NULL AND min_item_count >= 1 AND min_item_count <= 1e6 AND (max IS NULL OR (max > min AND max <= 1e6)))` |
| 3 | same shape for amount, no upper cap | amount `min NULL / max 1000` | `... OR (min_merchandise_amount IS NOT NULL AND min >= 0 AND min <= 1e9 AND (max IS NULL OR (max > min AND max <= 1e9)))` |
| 4 | `amount_within_max` ignored the minimum | `min_merchandise_amount = 2e9, max NULL` | `... AND (min_merchandise_amount IS NULL OR min_merchandise_amount <= 1e9) AND (max_merchandise_amount IS NULL OR max_merchandise_amount <= 1e9)` |
| 5 | `currency_code IS NULL OR (char_length = 3 AND currency_code = upper(currency_code))` | `'123'`, `'A$!'`, `'IR '` | `currency_code IS NULL OR (octet_length(currency_code) = 3 AND currency_code COLLATE "C" ~ '^[A-Z]{3}$')` |

`octet_length` + `COLLATE "C"` makes the three-character rule byte-wise ASCII, so a locale cannot widen the
range and a 3-byte multibyte sequence can never satisfy it. No price/currency policy, registry, cap or
conversion was added: the corrected expressions state exactly the rules the aggregate already enforces.

## 3. Live constraint truth after the migration

```
ck_authoring_rate_rows_weight_interval :: CHECK ((((min_weight_grams IS NULL) AND (max_weight_grams IS NULL)) OR ((min_weight_grams IS NOT NULL) AND (min_weight_grams >= 0) AND (min_weight_grams <= 1000000000) AND ((max_weight_grams IS NULL) OR ((max_weight_grams > min_weight_grams) AND (max_weight_grams <= 1000000000))))))
ck_authoring_rate_rows_count_interval :: CHECK ((((min_item_count IS NULL) AND (max_item_count IS NULL)) OR ((min_item_count IS NOT NULL) AND (min_item_count >= 1) AND (min_item_count <= 1000000) AND ((max_item_count IS NULL) OR ((max_item_count > min_item_count) AND (max_item_count <= 1000000))))))
ck_authoring_rate_rows_merchandise_interval :: CHECK ((((min_merchandise_amount IS NULL) AND (max_merchandise_amount IS NULL)) OR ((min_merchandise_amount IS NOT NULL) AND (min_merchandise_amount >= 0) AND (min_merchandise_amount <= 1000000000) AND ((max_merchandise_amount IS NULL) OR ((max_merchandise_amount > min_merchandise_amount) AND (max_merchandise_amount <= 1000000000))))))
ck_authoring_rate_rows_amount_within_max :: CHECK (((freight_amount <= 1000000000) AND (packaging_amount <= 1000000000) AND ((min_merchandise_amount IS NULL) OR (min_merchandise_amount <= 1000000000)) AND ((max_merchandise_amount IS NULL) OR (max_merchandise_amount <= 1000000000))))
ck_authoring_region_groups_currency_canonical :: CHECK (((currency_code IS NULL) OR ((octet_length(currency_code) = 3) AND ((currency_code COLLATE "C") ~ '^[A-Z]{3}$'))))
```

## 4. Executed direct-storage matrix (bypasses the aggregate and transport)

`Raw_storage_rejects_every_shape_the_domain_rejects_and_accepts_the_valid_boundaries` inserts/updates with
parameterized SQL on the real provider. Each rejected attempt owns a transaction that is rolled back, so one
aborted statement cannot poison the next; every rejection asserts SQLSTATE `23514` **and** the owned
constraint identity.

| Attempt | Expected | Result |
| --- | --- | --- |
| weight `min NULL / max 500` | 23514 `ck_authoring_rate_rows_weight_interval` | rejected |
| count `min NULL / max 5` | 23514 `ck_authoring_rate_rows_count_interval` | rejected |
| amount `min NULL / max 1000` | 23514 `ck_authoring_rate_rows_merchandise_interval` | rejected |
| amount `min 2e9 / max NULL` | 23514 merchandise interval or amount cap | rejected |
| amount `min 0 / max 2e9` | 23514 merchandise interval or amount cap | rejected |
| weight `min 5000 / max 5000` | 23514 weight interval | rejected |
| count `min 0 / max 5` | 23514 count interval | rejected |
| currency `'123' 'A$!' 'irr' 'IR ' ' I' 'IR'` | 23514 `ck_authoring_region_groups_currency_canonical` | all rejected |
| weight both NULL | accepted | persisted |
| weight `min 0 / max NULL` (min-only) | accepted | persisted |
| weight `[0,1000)` + `[1000,5000)` (adjacent) | accepted | persisted |
| count `[1,4)` + amount `[5000,10000)` | accepted | persisted |
| amount `min 1e9 / max NULL` (at limit) | accepted | persisted, stored verbatim |
| currency `'IRR'` (canonical) | accepted | read back as `IRR` |

The row `min_merchandise_amount == 1_000_000_000` is re-read from storage to prove no silent rounding.

## 5. R191-02 executed cross-operation races (same root revision, two different operations)

Both facts use independent `Npgsql` contexts, the existing bounded `TwoReaderGate` (20 s) and the
`TwoReaderBarrierInterceptor` that arrives after each participant loaded revision 1 and before either writes;
a bare BEGIN holds no row lock, so no participant waits while holding a conflicting lock. No sleeps.

**`A_same_revision_rate_write_racing_a_region_edit_leaves_exactly_the_winner_state`**
A = `ReplaceGroupRateRowsAsync` (rows on the first group), B = `ReplaceRegionGroupsAsync` (reorder + add a
selection). Asserted: both arrivals, exactly one success, canonical `draft_revision_conflict` for the loser,
stored revision exactly 2, and either
- A wins → tree byte-equal to the seeded tree, exactly the one row (`1111`), currency `IRR`; or
- B wins → order `[0,1]`, first group is the second group with `{Isfahan, Qom}`, second is the first group
  with `Tehran`, zero rate rows, currency NULL.
Plus: no staged orders, and the ACTUAL losing context has zero non-Unchanged entries, so a later
`SaveChanges` cannot publish the rejected edit.

**`A_same_revision_rate_write_racing_a_region_removal_never_leaves_orphan_rows`**
A writes rows to the group that B explicitly removes. Either A wins (group + row survive, tree `[first,
second]`) or B wins (tree `[second]` only and **no** row for either group). Exactly one success, loser gets the
same canonical conflict, no staged residue, loser tracker clean.

## 6. Migration and historical parity

- New: `20261022075711_AuthoringRateRowConstraintParity` (+Designer) drops and re-adds only the five OWN
  authoring checks; `Down` restores the exact previous expressions. No table, column, index, FK or data change.
- `ShippingDbContextModelSnapshot.cs` updated (the authorized new migration only).
- `git status`/`git diff --stat` over `Persistence/Migrations` shows exactly the snapshot plus the two new
  files: every TB-191 and older migration/designer is byte-identical, and no legacy table or constraint was
  weakened.
- Generated forward SQL: `correction.sql` in this folder (verified against the live database).
- DDL applied only to the disposable task database by the existing test/module migration authority.

## 7. Commands and counts

| Command | Result |
| --- | --- |
| `dotnet build .../Tooba.Shipping.Tests` | 0 errors |
| `dotnet test ... --filter FullyQualifiedName~Relational` (live PostgreSQL) | 24 passed / 0 failed |
| whole `Tooba.Shipping.Tests` once, live DB still up | **577 passed / 0 failed / 0 skipped** |

Affected guards re-run inside that suite: persistence model (9 migrations, 4 intra-module FKs, CHECK names),
request classification, compatibility/transaction guards, authoring domain/HTTP facts.

## 8. TB-191 Host-failure attribution

`TB191-HostFailure-Attribution: UNVERIFIED_NO_HOST_RERUN`. The earlier 132 `Tooba.Host.Tests` failures were not
re-run and are not attributed here; no Host/other-module command was executed in this repair.

## 9. Isolation and cleanup

Fresh task-owned container `tb191r1-postgres` (`postgres:17-alpine`) on `127.0.0.1:55432`, database
`shipping_tb190r1`, role `tb190r1`; port was verified free before start (`Get-NetTCPConnection` returned 0).
Parsed target plus live `current_database()`/`current_user` verified before writes. Shared `:5432`,
`tooba_alpha`, `store-alpha`, shared apps/ports, Redis/RabbitMQ, deployment flags and other people's
dirty/untracked files were untouched. Container removed after the runs; no listener remains on 55432 and no
credentials were logged.

## 10. Honest limits

No API/handler/validator/Domain policy, endpoint, DTO, evaluator, activation, frontend, financial or
certification change. Runtime merchandise basis stays `OPEN_NOT_IMPLEMENTED`; drafts stay inactive.
`Worker PASS` is technical evidence only; Architect ACCEPT is not claimed.
