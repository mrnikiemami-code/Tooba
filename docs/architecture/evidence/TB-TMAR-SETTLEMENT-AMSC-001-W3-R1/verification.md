# TB-TMAR-SETTLEMENT-AMSC-001-W3-R1 — Set-Equality Guard Proof + W3 Starting-Head Provenance Repair

Skill: `tooba-architecture-certify` §6a (input-provenance / durable set-equality HARD GATE).
Mode: `BOUNDED_CERTIFICATION_PROOF_AND_SOT_LINEAGE_REPAIR`.
Parent: `TB-TMAR-SETTLEMENT-AMSC-001-W3` (historical certification, preserved unrewritten).

```text
STARTING HEAD
529b8054ec8cdfe43c0fadf7c2c6436ac8a4a4c2

AUTHORITIES
W0 Analyze           bac4dbe38a46788c8fe27f42820c087294584d6d
W1 Migrate           4ca4aafc0acc3b5bc62c6cc366a9e50d07b11d05
W2 Structure         86ebb4ddb0ba85da480f44a7e797cf1791233f2a
Historical W3 Certify 529b8054ec8cdfe43c0fadf7c2c6436ac8a4a4c2
```

## FINAL VERDICT

```text
COMPLETE_REFERENCE_PATTERN (preserved)
REPAIRED GAP 1: DURABLE_SET_EQUALITY_ENFORCEMENT
REPAIRED GAP 2: W3_STARTING_HEAD_PROVENANCE (4ca4aafc -> 86ebb4dd)
```

- `Production-Code-Changed-State` = `ZERO`
- `Guards-Weakened-State` = `NONE`
- `Baselines-Widened-State` = `NONE`

---

## 1. Precheck

| Check | Result |
|---|---|
| `git fetch origin` | origin/main = `529b8054ec8cdfe43c0fadf7c2c6436ac8a4a4c2` |
| `HEAD` | `529b8054ec8cdfe43c0fadf7c2c6436ac8a4a4c2` |
| `HEAD == origin/main` | YES (no advance; no STOP condition) |
| Worktree | clean of unrelated changes; only prior-wave untracked evidence directories present |
| Authority ancestry | `bac4dbe3`, `4ca4aafc`, `86ebb4dd`, `529b8054` all ancestors of HEAD |
| Production defect found | NONE (this task repairs verification/provenance only) |

Inspected before editing: current W3 SoT block, manifest Settlement entry, Master Recovery Settlement W3
entry, W3 `certification.md` / `validation.md` / `request-handler-validator-matrix.md`, all four actual
Settlement endpoint files, the four transport validators, `AdminPayoutGridQueryPolicy`, the historical W3
guard, the W1/W2 guards, and the module test guards.

## 2. The audit finding (accepted, reproduced, then repaired)

The historical `SettlementModuleAmsc001W3CertGuardTests` asserted:

1. `10` route mappings (`group.Map*` count across the two audience files),
2. `10` `ISender.Send` call sites,
3. presence of `10` hard-coded request names in `ValidatorRequired` + `NoValidatorRequired`.

None of those three assertions derives the **actual** dispatched request type of each shipped route from
the endpoint source. A changed or replaced dispatch can therefore preserve every count — and even the
overall set of dispatched types — while silently invalidating the provenance matrix. That is a real
violation of the certify §6a durable set-equality hard gate.

No production defect was found. The audit finding was a **verification gap**, not a code defect, and this
wave repaired only the gap.

## 3. Independent re-derivation from disk (not from SoT, not from the static arrays)

| Derived artifact | Source of truth used |
|---|---|
| Audience route prefixes | `SettlementEndpointModule.MapGroup("/v1/seller"｜"/v1/admin")` |
| Verb + path + handler | `group.Map{Get,Post}("<path>", <Handler>)` call sites in the two endpoint files |
| Dispatched request | the handler's `ISender.Send(...)` argument, traced to its construction |
| Validator targets | concrete `class *Validator : AbstractValidator<T>` definitions |

Dispatch extraction supports both shapes:

- inline: `sender.Send(new GetSellerSettlementBalanceQuery(sellerPartyId), ct)`
- variable: `var command = new RequestSellerPayoutCommand(...); sender.Send(command, ct)`

and **fails closed** (throws) when a dispatch is untraceable or ambiguous.

## 4. Derived route → request map (10 rows, exact)

| # | Verb + full path | Handler | Actual dispatched request | Class |
|---|---|---|---|---|
| 1 | `GET /v1/seller/settlement/balance` | `SellerBalanceAsync` | `GetSellerSettlementBalanceQuery` | NO_VALIDATOR_REQUIRED |
| 2 | `GET /v1/seller/settlement/entries` | `SellerEntriesAsync` | `ListSellerSettlementEntriesQuery` | NO_VALIDATOR_REQUIRED |
| 3 | `GET /v1/seller/settlement/statements` | `SellerStatementsAsync` | `ListSellerSettlementStatementsQuery` | NO_VALIDATOR_REQUIRED |
| 4 | `GET /v1/seller/settlement/payout-requests` | `SellerPayoutListAsync` | `ListSellerPayoutRequestsQuery` | NO_VALIDATOR_REQUIRED |
| 5 | `POST /v1/seller/settlement/payout-requests` | `SellerRequestPayoutAsync` | `RequestSellerPayoutCommand` (variable) | VALIDATOR_REQUIRED |
| 6 | `GET /v1/admin/settlement/balances` | `AdminBalancesAsync` | `ListAdminSettlementBalancesQuery` | NO_VALIDATOR_REQUIRED |
| 7 | `GET /v1/admin/settlement/payout-queue` | `AdminPayoutQueueAsync` | `ListAdminPayoutQueueQuery` | NO_VALIDATOR_REQUIRED |
| 8 | `POST /v1/admin/settlement/payout-queue/query` | `AdminQueryPayoutGridAsync` | `QueryAdminPayoutGridQuery` | VALIDATOR_REQUIRED |
| 9 | `POST /v1/admin/settlement/payout-requests/{payoutRequestId:guid}/process` | `AdminProcessPayoutAsync` | `ProcessAdminPayoutCommand` | VALIDATOR_REQUIRED |
| 10 | `POST /v1/admin/settlement/payout-requests/{payoutRequestId:guid}/retry` | `AdminRetryPayoutAsync` | `RetryAdminPayoutCommand` | VALIDATOR_REQUIRED |

```text
shipped routes                       = 10
ISender.Send call sites              = 10
actual distinct dispatched requests  = 10
handlers (IRequestHandler<T,_>)      = 10
VALIDATOR_REQUIRED                   = 4
NO_VALIDATOR_REQUIRED                = 6
orphan / duplicate / unclassified    = 0
```

## 5. Exemption provenance re-check (all six, from disk)

| Exemption | Real guarantee verified |
|---|---|
| `GetSellerSettlementBalanceQuery` | seller party from `ISettlementSellerAuthorizer.RequireAuthorizedAsync` (server-derived) |
| `ListSellerSettlementEntriesQuery` | same server-derived seller party; no client-shaped input |
| `ListSellerSettlementStatementsQuery` | same |
| `ListSellerPayoutRequestsQuery` | same |
| `ListAdminSettlementBalancesQuery` | parameterless (`NO_INPUT`) — no bound input exists |
| `ListAdminPayoutQueueQuery` | parameterless (`NO_INPUT`) — no bound input exists |

The three identifier routes carry the `:guid` constraint, so a malformed value is rejected by routing
before a handler runs. `ProcessAdminPayoutCommand` / `RetryAdminPayoutCommand` nonetheless stay
validator-required because a syntactically valid `:guid` route value can still be `Guid.Empty`, which the
empty-Guid rule rejects. No client-readable identity shortcut exists in the seller/admin endpoint files.

`QueryAdminPayoutGridQuery` (the only VALIDATOR_REQUIRED query) is validated twice: the transport
validator `QueryAdminPayoutGridQueryValidator` plus the module-owned `AdminPayoutGridQueryPolicy`
**actually executed inside the handler** (`AdminPayoutGridQueryPolicy.Instance.Normalize(request.Request)`),
whose `GridQueryValidationException` is mapped to `SemanticError(ex.ErrorCode)` with no message-text
classification.

## 6. Mutation negative proof (in memory and on disk)

Mutation: replace the seller entries dispatch with a request already in the matrix —
`new ListSellerSettlementEntriesQuery(sellerPartyId)` → `new ListSellerPayoutRequestsQuery(sellerPartyId)`.
This preserves the `ISender.Send` count **and** the dispatched type set.

Reproducible script: `docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W3-R1/mutation-proof.js`

```text
mutation applied: new ListSellerSettlementEntriesQuery(sellerPartyId)  ->  new ListSellerPayoutRequestsQuery(sellerPartyId)
ISender.Send count preserved: 5 -> 5
HISTORICAL_W3_GUARD : Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6      <-- blind
NEW_W3_R1_GUARD     : Failed!  - Failed: 4, Passed: 6, Skipped: 0, Total: 10     <-- caught
file restored byte-identically: true (sha256 33e613a77c70739a80bd0384c83f0862f5ab97e3703872d8163616461e194f41)
```

The temporary production edit was restored byte-identically and **never committed**; the mutation is also
reproduced purely in memory by
`Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate`.

## 7. New durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/SettlementModuleAmsc001W3R1SetEqualityGuardTests.cs` — 10 tests:

| Test | Locks |
|---|---|
| `Shipped_routes_and_actual_dispatched_requests_match_the_classified_matrix_exactly` | exact route→request equality over all 10 rows, derived from endpoint code |
| `Dispatched_request_set_equals_the_4_required_plus_6_exempt_partition_with_no_orphan_or_duplicate` | set equality + real partition + 1:1 handler mapping |
| `Validator_targets_derived_from_concrete_definitions_equal_the_four_required_requests` | validator targets from `AbstractValidator<T>`; none for exempt |
| `Exempt_requests_rest_on_route_guid_constraints_and_server_derived_actors_only` | `:guid` binding + server-derived parties + parameterless admin requests |
| `Grid_query_request_is_validated_by_the_validator_and_the_module_owned_runtime_policy` | policy executed + typed error mapping + allowlists |
| `Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate` | in-memory mutation negative proof |
| `Inline_dispatch_is_read_directly_and_an_untraceable_variable_send_fails_closed` | variable tracing + fail-closed behaviour |
| `Validators_are_discoverable_through_the_real_cqrs_registration` | non-circular runtime DI: 4 resolved, 6 null, one `ValidationBehavior<,>` |
| `Certification_truth_and_stable_code_split_are_preserved` | 17+1 codes, canonical `api.From(`, zero raw `Results.*` |
| `Manifest_root_rule_coverage_is_intentional_and_not_widened_for_numeric_equality` | 3-of-6 root-rule map intent |

## 8. W3 starting-head provenance correction

| Item | Before (historical W3) | After (current W3 provenance) |
|---|---|---|
| SoT `settlementAmsc001W3.startingHead` | `4ca4aafc0acc3b5bc62c6cc366a9e50d07b11d05` (W1) | `86ebb4ddb0ba85da480f44a7e797cf1791233f2a` (W2) |
| `settlementAmsc001W3.authorityAncestry` | `... W2 (structure) -> W3` | `... W2 86ebb4dd (structure) -> W3 (starting head 86ebb4dd)` |
| W3 guard assertion | `Assert.Equal("4ca4aafc0...", w3.startingHead)` | `Assert.Equal("86ebb4ddb0...", w3.startingHead)` |
| W2 SoT block | `startingHead: "4ca4aafc"` (abbreviated) | adds explicit `startingHeadFull: "4ca4aafc0acc3b5bc62c6cc366a9e50d07b11d05"` |

The historical W3 evidence directory and the historical W3 Master Recovery entry are **preserved
unrewritten**; the change is recorded as a W3-R1 correction note (this document plus the Master Recovery
`Settlement AMSC W3-R1 ...` entry) rather than a silent rewrite of history. No self-referential current
commit SHA is claimed in any as-yet-uncommitted file; the W3-R1 commit SHA is reported only in the Bridge
Result.

## 9. Manifest 3-of-6 project check (finding F)

The manifest Settlement entry lists three projects — `Tooba.Settlement.Application`,
`Tooba.Settlement.Endpoints`, `Tooba.Settlement.Infrastructure` — while the solution contains six
(`Domain`, `Contracts`, `Application`, `Infrastructure`, `Endpoints`, `Tests`).

This is intentional: the manifest is a **root-rule map** (`rootAllowlist` / `forbiddenRootFiles` /
`forbiddenTopLevelFolders`), not a project inventory. Only the three manifested projects carry project-root
`.cs` rules. `Domain` and `Contracts` hold no project-root `.cs` file (verified empty) and therefore need
no root rule; `Tests` is a test project. The manifest was deliberately **not** widened to force numeric
equality, and no project list or allowlist was changed by this wave (only the `certificationNote` text was
updated to reflect the W3-R1 authority and the route prefix correction).

## 10. Focused validation (exact counts)

| Command | Result |
|---|---|
| `SettlementModuleAmsc001` filter (W1 12 + W2 9 + W3 6 + W3-R1 10) | **37 passed / 0 failed / 0 skipped** |
| `Tooba.Settlement.Tests` (module suite) | **30 passed / 0 failed / 0 skipped** |
| `ErrorCatalogUniqueCodeGuardTests` | **3 passed / 0 failed / 0 skipped** |
| On-disk mutation proof (`mutation-proof.js`) | historical W3 blind (6/6), W3-R1 caught (4 failed), file restored byte-identically |

Broader global-structure/recovery failures were compared by **exact test-name set** against the W3
baseline and are unchanged:

- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  — known `Tooba.Catalog.Contracts.Cart` namespace deviation (documented in the gate source as out of
  scope for a module-local certification);
- `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` — stale repository-global
  recovery pin;
- `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
  — same stale pin.

Both JSON files (`tmar-current-state.json`, `tmar-module-structure-manifests.json`) parse successfully.

## 11. Scope and preservation

| Item | State |
|---|---|
| Production files changed | `ZERO` |
| Files moved / renamed | `ZERO` |
| Schema / migrations | `UNCHANGED` (3 migration files) |
| Contracts-only boundary | `CONTRACTS_ONLY` (no regression) |
| Host checkpoint / `lastAcceptedTask` / `workflowStop` | `PRESERVED` |
| `automaticNextImplementationTask` | `NONE` |
| Historical W3 evidence and certification | `PRESERVED_UNREWRITTEN` |
| Global certified-module list | `UNCHANGED` (`Settlement` still exactly once) |

Stop gate: `USER_REVIEW_SETTLEMENT_AMSC_001_W3_R1`.
