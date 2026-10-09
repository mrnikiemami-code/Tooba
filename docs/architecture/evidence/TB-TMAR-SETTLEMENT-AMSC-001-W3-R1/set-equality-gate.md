# TB-TMAR-SETTLEMENT-AMSC-001-W3-R1 — Durable set-equality gate (detail)

Companion to `verification.md`. Records the exact §6a gate mechanics so the Architect can re-derive them
without re-reading the guard source.

## 1. Why counts are not set equality

```text
guard assertion type                                  mutation it can catch
--------------------------------------------------    ------------------------------------------
route mapping count == 10                             adding/removing a route
ISender.Send call-site count == 10                    adding/removing a Send
presence of 10 hard-coded request names               deleting a request declaration/handler
ACTUAL route -> ACTUAL dispatched request equality    replacing one dispatch with another   <-- §6a
```

The first three were already enforced by the historical W3 guard. The fourth was not, which is the gap this
wave repaired. The mutation proof in §6 of `verification.md` demonstrates the gap concretely: the
historical guard passes 6/6 on a corrupted dispatch that the new guard rejects (4 failed of 10).

## 2. Parser contract (fail-closed)

```text
input : endpoint source text + handler name
output: dispatched request simple type name, or throw InvalidOperationException

step 1  locate handler body by name; brace-balance the block
step 2  find the ISender.Send( call inside that body      -> throw if absent
step 3  read the first argument
          "new T(...)"   -> simple name of T
          "identifier"   -> step 4
          anything else  -> throw
step 4  trace "identifier = new T(...)" in the same body  -> throw if untraceable
```

The guard never falls back to a default, an empty string or the historical static arrays. Any new dispatch
shape that the parser cannot prove must fail the build rather than pass silently.

## 3. Gate inventory

| Gate | Enforcement |
|---|---|
| Route → request exact equality | `Shipped_routes_and_actual_dispatched_requests_match_the_classified_matrix_exactly` |
| Dispatched set == 4 + 6 partition | `Dispatched_request_set_equals_the_4_required_plus_6_exempt_partition_with_no_orphan_or_duplicate` |
| No orphan / duplicate / unclassified | same test (`Except` both directions, distinct count) |
| Validator targets == required set | `Validator_targets_derived_from_concrete_definitions_equal_the_four_required_requests` |
| Exemption provenance | `Exempt_requests_rest_on_route_guid_constraints_and_server_derived_actors_only` |
| Grid policy runtime execution | `Grid_query_request_is_validated_by_the_validator_and_the_module_owned_runtime_policy` |
| Mutation negative proof | `Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate` |
| Variable dispatch + fail-closed | `Inline_dispatch_is_read_directly_and_an_untraceable_variable_send_fails_closed` |
| Runtime DI discoverability | `Validators_are_discoverable_through_the_real_cqrs_registration` |
| Stable-code split / canonical results | `Certification_truth_and_stable_code_split_are_preserved` |
| Manifest root-rule intent (3-of-6) | `Manifest_root_rule_coverage_is_intentional_and_not_widened_for_numeric_equality` |

## 4. Runtime DI proof (non-circular)

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddToobaCqrsFoundation(typeof(RequestSellerPayoutCommand).Assembly);
using var provider = services.BuildServiceProvider();
```

- `IValidator<RequestSellerPayoutCommand>` / `ProcessAdminPayoutCommand` / `RetryAdminPayoutCommand` /
  `QueryAdminPayoutGridQuery` → resolved (not null)
- `IValidator<GetSellerSettlementBalanceQuery>` / `ListSellerSettlementEntriesQuery` /
  `ListSellerSettlementStatementsQuery` / `ListSellerPayoutRequestsQuery` /
  `ListAdminSettlementBalancesQuery` / `ListAdminPayoutQueueQuery` → **null**
- exactly one `ValidationBehavior<,>` in the resolved pipeline behaviors

This proves discoverability through the real registration path instead of reflecting over the validator
source file, which would be circular.

## 5. Reproducing the mutation proof

```text
node docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W3-R1/mutation-proof.js
```

Expected output:

```text
mutation applied: new ListSellerSettlementEntriesQuery(sellerPartyId)  ->  new ListSellerPayoutRequestsQuery(sellerPartyId)
ISender.Send count preserved: 5 -> 5
HISTORICAL_W3_GUARD : Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6
NEW_W3_R1_GUARD     : Failed!  - Failed: 4, Passed: 6, Skipped: 0, Total: 10
file restored byte-identically: true (sha256 33e613a77c70739a80bd0384c83f0862f5ab97e3703872d8163616461e194f41)
```

The script restores the endpoint file in a `finally` block and verifies the restored SHA-256, so the
repository is never left mutated.
