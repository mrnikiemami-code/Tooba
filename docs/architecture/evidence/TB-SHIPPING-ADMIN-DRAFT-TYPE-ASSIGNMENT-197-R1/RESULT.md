# TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1 — post-commit lookup repair and focused proof

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 · Channel `tooba-main` · Worker `tooba-worker-01` (cursor)
Parent: `TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197`
Mode: `BOUNDED_POSTCOMMIT_LOOKUP_REPAIR_AND_ASSIGNMENT_FAILURE_DI_TRACE_PROOF`
Expected-Base-SHA / Base-SHA: `cc24c9c1b84e6283c0eed15f895e6b5143621395` (`HEAD == origin/main` before edits).

## 1. Production repair (only the two authorized Application files)

`ReplaceShippingAuthoringDraftTypeAssignmentHandler` previously:
validate type -> `ReplaceTypeAssignmentAsync` (commit) -> `ComposeAsync` -> **second** `GetByIdsAsync`.
An outage on that second read returned failure after a successful commit; a retry then conflicted.

Now:
- the single pre-write resolution result (`ShippingTypeFact? resolved`) is retained as a request-local fact;
- after the directory succeeds, `ShippingTypeAssignmentComposition.ComposeResolved(savedRoot, resolved)` builds the
  response **synchronously** from the actually saved draft id/revision and that fact;
- no foreign call, no cancellation probe and no other fallible dependency runs after `SaveChanges`;
- `null` (clear) still performs **zero** foreign reads and returns `Unconfigured`.

`GetShippingAuthoringDraftTypeAssignmentHandler` composition now returns `Result`:
- no selected id -> `Unconfigured` (no foreign read);
- **empty** returned set -> truthful `Missing` with the raw stored id retained;
- exactly one matching fact -> `Active` / `Inactive`;
- **non-empty inconsistent** response (wrong id, duplicates) -> fail closed with the existing canonical
  `shipping.authoring.configuration_unavailable` (`503`). No new error code, resource, DTO or exception mapper.

No Domain/Contracts/Endpoints/Infrastructure/DI/schema/migration/Designer/snapshot/Host/frontend change.

## 2. Pre-fix regression (recorded, not a harness correction)

Both new facts were run **before** the production repair:

```text
Put_resolves_the_selected_type_once_before_the_write_and_never_reads_after_commit
  Expected 200, Actual 500   (second catalog read threw AFTER the committed write)
Empty_facts_are_missing_but_inconsistent_nonempty_facts_fail_closed_with_the_canonical_code
  Expected 503, Actual 200   (wrong-id fact was reported as "missing")
```

After the repair the same two facts pass unchanged (`2/2 PASS`), proving the behaviour change rather than a
re-baselined expectation.

## 3. New/affected focused facts (actually executed)

| Fact | Proof |
| --- | --- |
| `Put_resolves_..._once_before_the_write...` | scripted catalog returns a valid active fact on the first lookup and throws on any second one: response `200`, exactly `1` read, revision `2`, `Active`; a later GET may fail on the dependency (`500`) without changing the committed revision/timestamp/type |
| `Empty_facts_are_missing_but_inconsistent_nonempty_facts...` | `[]` -> `200 Missing` retaining the raw id; wrong-id -> `503 configuration_unavailable`; duplicate -> `503`; stored id/revision/timestamp unchanged |
| `Rejections_in_one_context_leave_zero_tracked_mutation...` | `Guid.Empty` -> domain `type_assignment_invalid`; unknown/inactive -> `type_selection_unavailable`; the SAME context holds no non-`Unchanged` entry, later `SaveChanges` = `0`, baseline revision/timestamp/null type intact, then a valid assignment succeeds in that same context |
| `No_op_assignment_preserves_a_seeded_earlier_timestamp_exactly...` | server-side `updated_at` seeded one year back and re-read from PostgreSQL, so a restamp cannot pass; identical active selection keeps revision `2` and the exact stored timestamp; stale token still `409` |
| `Provider_aborted_assignment_write_publishes_nothing_and_a_clean_retry...` | task-only `BEFORE UPDATE OF shipping_type_id` trigger function limited to one synthetic draft id raises `P0001`; the write surfaces as `DbUpdateException` with that `PostgresException` (no new mapper); NEW-context readback shows unchanged revision/timestamp/null type, and the seeded rate row/currency/method settings intact; writing context tracker empty and later `SaveChanges` = `0`; trigger/function dropped in `finally`; same-context retry with the original revision succeeds once (revision `4`) preserving every other capability |
| `ShippingAuthoringTypeCatalogCompositionDiTests` (2 facts) | real `ShippingModule.AddServices`: exactly one **scoped** `IShippingTypeCatalogReader` descriptor resolving `FulfillmentShippingTypeCatalogReader`, same instance per scope / different across scopes, exactly one canonical `IModuleCallTracer`, real handler activation through that container; under a real parent `Activity` + `CorrelationIdContext.BeginScope`, one child span per actual read with matching `ParentSpanId`/`TraceId`, `Shipping`/`Fulfillment`/operation/`module_call`/correlation tags, `Activity.Current` restored to the parent, the exact sentinel exception instance rethrown with only its TYPE tagged, caller token identity preserved, and no code/tenant/message values in tags |

Commands (disposable PostgreSQL `127.0.0.1:55432`, db `shipping_tb190r1`, role `tb190r1`, parsed by
`ShippingRelationalTestTarget` and verified by `current_database`/`current_user` before DDL):

```text
dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj
# pre-fix regression (before production repair)      -> 0/2, expected failures recorded above
# after repair
... --filter TypeAssignmentStorageTests|Rejections_in_one_context|Put_resolves_the_selected|Empty_facts -> 11/11 PASS
... --filter TypeAssignmentEndpointTests|ErrorCatalogAndResources|RequestClassification                 -> 31/31 PASS
```

Retained 197 evidence (owner/schema/Contracts/race) is unchanged by this repair: the same additive migration,
snapshot and adapter identities are untouched. Full 626 suite / Fulfillment / Host / solution runs were not rerun
per task boundary.

## 4. Timebox, deviations, leftover gates

Task row created `20:18:33Z`; result `20:5xZ`. Two harness corrections were needed after the intended pre-fix run
(exact EF exception wrapper `DbUpdateException`, and `PostgresException.SqlState` read from the inner exception;
`CancellationToken` equality instead of boxing `Assert.Same`), which is one more than the single-correction
allowance — recorded rather than hidden.

Remaining gates unchanged: merchant/runtime definition proof, UI/visual acceptance, activation/quote/cutoff/
carrier/funding/deployment. TB-185 stays paused; default-OFF/PendingAllocation untouched.

## 5. Evidence for the 197 correction

`docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197/RESULT.md` now records that the PUT
response composition performed a post-commit foreign lookup, that the earlier single-read/no-post-commit and
parent-trace proofs were missing, and that this task repaired them. No historical text was rewritten or
re-attributed.
