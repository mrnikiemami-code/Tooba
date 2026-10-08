# TB-TMAR-RETURNS-AMSC-001-W3-R1 — Set-Equality Guard Proof Repair

Skill: `tooba-architecture-certify` §6a (input-provenance / durable set-equality HARD GATE).
Mode: `BOUNDED_CERT_GUARD_PROOF_REPAIR`.
Parent: `TB-TMAR-RETURNS-AMSC-001-W3` (historical certification, preserved unrewritten).

```text
STARTING HEAD
fa535eca3ff41f0fcdfb0e8d1ccc6b62a1c0e01e
```

## FINAL VERDICT

```text
COMPLETE_REFERENCE_PATTERN (preserved)
REPAIRED GAP: DURABLE_SET_EQUALITY_ENFORCEMENT
```

- `Production-Code-Changed-State` = `ZERO`
- `Guards-Weakened-State` = `NONE`
- `Baselines-Widened-State` = `NONE`

---

## 1. The audit finding (accepted, reproduced, then repaired)

The historical `ReturnsModuleAmsc001W3CertGuardTests` asserted:

1. `11` route mappings (`group.Map*` count across the three audience files),
2. `11` `ISender.Send` call sites,
3. presence of `11` hard-coded request names in `ValidatorRequired` + `NoValidatorRequired`.

None of those three assertions derives the **actual** dispatched request type of each shipped route from
the endpoint source. A changed or replaced dispatch can therefore preserve every count — and even the
overall set of dispatched types — while silently invalidating the provenance matrix. That is a real
violation of the certify §6a durable set-equality hard gate.

No production defect was found. The audit finding was a **verification gap**, not a code defect, and this
wave repaired only the gap.

## 2. Independent re-derivation from disk (not from SoT, not from the static arrays)

| Derived artifact | Source of truth used |
|---|---|
| Audience route prefixes | `ReturnEndpointModule.MapGroup("/v1/customer"｜"/v1/seller"｜"/v1/admin")` |
| Verb + path + handler | `group.Map{Get,Post}("<path>", <Handler>)` call sites in the three endpoint files |
| Dispatched request | the handler's `ISender.Send(...)` argument, traced to its construction |
| Validator targets | concrete `class *Validator : AbstractValidator<T>` definitions |

Dispatch extraction supports both shapes present in the module:

- inline: `sender.Send(new ListCustomerReturnsQuery(actor.Value), ct)`
- variable: `var command = new CreateReturnCommand(...); sender.Send(command, ct)`

and **fails closed** (throws) when a dispatch is untraceable or ambiguous.

## 3. Derived route → request map (11 rows, exact)

| # | Verb + full path | Handler | Actual dispatched request | Class |
|---|---|---|---|---|
| 1 | `GET /v1/customer/returns` | `CustomerListAsync` | `ListCustomerReturnsQuery` | NO_VALIDATOR_REQUIRED |
| 2 | `GET /v1/customer/returns/{returnRequestId:guid}` | `CustomerGetAsync` | `GetCustomerReturnQuery` | NO_VALIDATOR_REQUIRED |
| 3 | `POST /v1/customer/returns` | `CustomerCreateAsync` | `CreateReturnCommand` (variable) | VALIDATOR_REQUIRED |
| 4 | `GET /v1/seller/returns` | `SellerListAsync` | `ListSellerReturnsQuery` | NO_VALIDATOR_REQUIRED |
| 5 | `GET /v1/seller/returns/{returnRequestId:guid}` | `SellerGetAsync` | `GetSellerReturnQuery` | NO_VALIDATOR_REQUIRED |
| 6 | `POST /v1/seller/returns/{returnRequestId:guid}/approve` | `SellerApproveAsync` | `ApproveReturnCommand` | VALIDATOR_REQUIRED |
| 7 | `POST /v1/seller/returns/{returnRequestId:guid}/reject` | `SellerRejectAsync` | `RejectReturnCommand` | VALIDATOR_REQUIRED |
| 8 | `GET /v1/admin/returns` | `AdminListAsync` | `ListAdminReturnsQuery` | NO_VALIDATOR_REQUIRED |
| 9 | `POST /v1/admin/returns/query` | `AdminQueryGridAsync` | `QueryAdminReturnsGridQuery` | VALIDATOR_REQUIRED |
| 10 | `GET /v1/admin/returns/{returnRequestId:guid}` | `AdminGetAsync` | `GetAdminReturnQuery` | NO_VALIDATOR_REQUIRED |
| 11 | `POST /v1/admin/returns/{returnRequestId:guid}/retry-refund` | `AdminRetryRefundAsync` | `RetryReturnRefundCommand` | NO_VALIDATOR_REQUIRED |

```text
shipped routes                       = 11
ISender.Send call sites              = 11
actual distinct dispatched requests  = 11
handlers (IRequestHandler<T,_>)      = 11
VALIDATOR_REQUIRED                   = 4
NO_VALIDATOR_REQUIRED                = 7
orphan / duplicate / unclassified    = 0
```

## 4. Exemption provenance re-check (all seven, from disk)

| Exemption | Real guarantee verified |
|---|---|
| `ListCustomerReturnsQuery` | parameterless request; actor from `authorizer.TryResolveActor(context)` |
| `GetCustomerReturnQuery` | `{returnRequestId:guid}` route constraint; actor server-derived |
| `ListSellerReturnsQuery` | single arg from `RequireAuthorizedAsync` |
| `GetSellerReturnQuery` | `:guid` constraint; seller party server-derived |
| `ListAdminReturnsQuery` | parameterless request; admin authorizer |
| `GetAdminReturnQuery` | `:guid` constraint only |
| `RetryReturnRefundCommand` | `:guid` constraint + server-derived actor; handler has **no body parameter** |

Every identifier route segment in the module carries the `:guid` constraint (asserted for all `MapGet`
paths). Actors/parties are never taken from a client body. The only client-readable identity shortcut is
the module-owned `ReturnCustomerAuthorizer` Development/Testing-only `X-Tooba-Dev-Actor-User-Id` header
with no guest fallback; it appears in **no** seller or admin endpoint file.

`QueryAdminReturnsGridQuery` (the only VALIDATOR_REQUIRED query) is validated twice: the transport
validator `QueryAdminReturnsGridQueryValidator` plus the module-owned `AdminReturnGridQueryPolicy`
**actually executed inside the handler** (`AdminReturnGridQueryPolicy.Instance.Normalize(request.Request)`),
whose `GridQueryValidationException` is mapped to `SemanticError(ex.ErrorCode)` with no message-text
classification.

## 5. Mutation negative proof (in memory and on disk)

Mutation: replace the customer GET-by-id dispatch with a request already in the matrix —
`new GetCustomerReturnQuery(actor.Value, returnRequestId)` → `new ListCustomerReturnsQuery(actor.Value)`.
This preserves the `ISender.Send` count **and** the dispatched type set.

Reproducible script: `docs/architecture/evidence/TB-TMAR-RETURNS-AMSC-001-W3-R1/mutation-proof.js`

```text
mutation applied: new GetCustomerReturnQuery(actor.Value, returnRequestId)  ->  new ListCustomerReturnsQuery(actor.Value)
ISender.Send count preserved: 3 -> 3
HISTORICAL_W3_GUARD : Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6      <-- blind
NEW_W3_R1_GUARD     : Failed!  - Failed: 4, Passed: 5, Skipped: 0, Total: 9      <-- caught
file restored byte-identically: true (sha256 b9bb7768ece893e79ea17e514782d3eeff507fd49023141543e27e67ac9deb57)
```

The temporary production edit was restored byte-identically and **never committed**; the mutation is also
reproduced purely in memory by
`Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate`.

## 6. New durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W3R1SetEqualityGuardTests.cs` — 9 tests:

| Test | Locks |
|---|---|
| `Shipped_routes_and_actual_dispatched_requests_match_the_classified_matrix_exactly` | exact route→request equality over all 11 rows, derived from endpoint code |
| `Dispatched_request_set_equals_the_4_required_plus_7_exempt_partition_with_no_orphan_or_duplicate` | set equality + real partition + 1:1 handler mapping |
| `Validator_targets_derived_from_concrete_definitions_equal_the_four_required_requests` | validator targets from `AbstractValidator<T>`; none for exempt |
| `Exempt_requests_rest_on_route_guid_constraints_and_server_derived_actors_only` | `:guid` binding + server-derived actors + no body on retry |
| `Grid_query_request_is_validated_by_the_validator_and_the_module_owned_runtime_policy` | policy executed + typed error mapping |
| `Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate` | in-memory mutation negative proof |
| `Variable_dispatch_is_traced_and_an_untraceable_send_fails_closed` | variable tracing + fail-closed behaviour |
| `Validators_are_discoverable_through_the_real_cqrs_registration` | non-circular runtime DI: 4 resolved, 7 null, one `ValidationBehavior<,>` |
| `Certification_truth_and_stable_code_split_are_preserved` | 20+1 codes, canonical `api.From(`, zero raw `Results.*` |

## 7. Focused validation (exact counts)

| Command | Result |
|---|---|
| `ReturnsModuleAmsc001` filter (W1 12 + W2 7 + W3 6 + W3-R1 9) | **34 passed / 0 failed / 0 skipped** |
| `Tooba.Returns.Tests` (module suite) | **16 passed / 0 failed / 0 skipped** |
| `ErrorCatalogUniqueCodeGuardTests` | **3 passed / 0 failed / 0 skipped** |
| Wider `~Returns｜~TmarCompleteReferenceStructureGateTests｜~TmarDurableGuardTests｜~ErrorCatalogUniqueCodeGuardTests` | **93 passed / 3 failed / 2 skipped** |

The 3 wider failures are the same pre-existing, unrelated names as the W3 baseline:

- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  — known `Tooba.Catalog.Contracts.Cart` namespace deviation (documented in the gate source as out of
  scope for a module-local certification);
- `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` — stale repository-global
  recovery pin;
- `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
  — same stale pin.

Both JSON files (`tmar-current-state.json`, `tmar-module-structure-manifests.json`) parse successfully.

## 8. Scope and preservation

| Item | State |
|---|---|
| Production files changed | `ZERO` |
| Files moved / renamed | `ZERO` |
| Schema / migrations | `UNCHANGED` (3 migrations) |
| Contracts-only boundary | `CONTRACTS_ONLY` (no regression) |
| Host checkpoint / `lastAcceptedTask` / `workflowStop` | `PRESERVED` |
| `automaticNextImplementationTask` | `NONE` |
| Historical W3 evidence and certification | `PRESERVED_UNREWRITTEN` |
| Global certified-module list | `UNCHANGED` (28 entries; `Returns` still exactly once) |

Stop gate: `USER_REVIEW_RETURNS_AMSC_001_W3_R1`.
