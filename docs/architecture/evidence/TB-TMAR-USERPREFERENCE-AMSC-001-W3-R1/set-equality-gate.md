# TB-TMAR-USERPREFERENCE-AMSC-001-W3-R1 — Route/Request Set-Equality Proof

Skill: `tooba-architecture-certify` §6a (independent input-provenance / durable set-equality HARD GATE).
Mode: `BOUNDED_ROUTE_REQUEST_SET_EQUALITY_PROOF`.
Parent: `TB-TMAR-USERPREFERENCE-AMSC-001-W3` (historical certification, preserved unrewritten).

```text
STARTING HEAD
1808ba553b99f4b3c81f0dc89f101152245a91cd
```

## FINAL VERDICT

```text
COMPLETE_REFERENCE_PATTERN (preserved)
REPAIRED GAP: DURABLE_ROUTE_REQUEST_SET_EQUALITY_ENFORCEMENT
```

- `Production-Code-Changed-State` = `ZERO`
- `Guards-Weakened-State` = `NONE`
- `Baselines-Widened-State` = `NONE`

---

## 1. The outstanding proof gap (reproduced, then closed)

`UserPreferenceModuleAmsc001W3CertGuardTests` asserted:

1. `6` route mappings (`group.Map*` count across the three endpoint files),
2. `6` `ISender.Send` call sites,
3. presence of the `4` hard-coded request names.

None of those assertions derives the **actual** dispatched request type of each shipped route from the
endpoint source. A replaced dispatch can therefore preserve every count — and even the overall *set* of
dispatched types — while silently invalidating the provenance matrix. That is a real violation of the
certify §6a durable set-equality hard gate.

No production defect was found. This was a **verification gap**, not a code defect, and this bounded task
repaired only the gap.

## 2. Independent re-derivation from disk (not from SoT, not from the static arrays)

| Derived artifact | Source of truth used |
|---|---|
| Audience route prefixes | `UserPreferenceEndpointModule.MapGroup("/v1/customer/preferences"｜"/v1/admin/operator/preferences"｜"/v1/admin/ui-preferences")` |
| Verb + path + handler | `group.Map{Get,Put}("<path>", <Handler>)` call sites in the three endpoint files |
| Dispatched request | the handler's `ISender.Send(...)` argument, traced to its construction |
| Validator targets | concrete `class *Validator : AbstractValidator<T>` definitions under `Application/**/Validators` |

Dispatch extraction supports both shapes present in the module:

- inline: `sender.Send(new GetUserPreferenceQuery(actor.Value), cancellationToken)`
- inline multi-line: `sender.Send(\n    new UpsertUserPreferenceCommand(actor.Value, body.Locale),\n    cancellationToken)`

and **fails closed** (throws) when a dispatch is untraceable or ambiguous.

## 3. Derived route → request map (6 rows, exact)

| # | Audience | Verb + full route | Handler | Actual dispatched request | Class |
|---|---|---|---|---|---|
| 1 | customer | `GET /v1/customer/preferences/` | `GetAsync` | `GetUserPreferenceQuery` | NO_VALIDATOR_REQUIRED |
| 2 | customer | `PUT /v1/customer/preferences/` | `PutAsync` | `UpsertUserPreferenceCommand` | VALIDATOR_REQUIRED |
| 3 | admin-operator | `GET /v1/admin/operator/preferences/` | `GetAsync` | `GetUserPreferenceQuery` | NO_VALIDATOR_REQUIRED |
| 4 | admin-operator | `PUT /v1/admin/operator/preferences/` | `PutAsync` | `UpsertUserPreferenceCommand` | VALIDATOR_REQUIRED |
| 5 | admin-ui | `GET /v1/admin/ui-preferences/{key}` | `GetAsync` | `GetUiPreferenceQuery` | VALIDATOR_REQUIRED |
| 6 | admin-ui | `PUT /v1/admin/ui-preferences/{key}` | `PutAsync` | `UpsertUiPreferenceCommand` | VALIDATOR_REQUIRED |

```text
shipped routes                       = 6
ISender.Send call sites              = 6   (exactly one per route)
actual distinct dispatched requests  = 4   <-- 6 distinct requests are NOT required
handlers (IRequestHandler<T,_>)      = 4   (one per distinct request)
VALIDATOR_REQUIRED                   = 3
NO_VALIDATOR_REQUIRED                = 1
orphan / duplicate / unclassified    = 0
```

The two locale requests are genuinely shared: `GetUserPreferenceQuery` and `UpsertUserPreferenceCommand`
are each dispatched by **two** routes (customer + admin-operator). The two UI requests are dispatched once
each. Each of the four request types is declared exactly once with exactly one handler and exactly one
`IRequest<Result<...>>` declaration.

## 4. Exemption provenance re-check (the single exemption, from disk)

| Exemption | Real guarantee verified |
|---|---|
| `GetUserPreferenceQuery` | server-derived actor only; the GET routes bind **no** client-controlled transport shape — no body and no route parameter (`group.MapGet("/", GetAsync)` in both the customer and admin-operator files) |

- Customer: `actorResolver.ResolveActor(httpContext)` → `new GetUserPreferenceQuery(actor.Value)`.
- Admin-operator: `await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken)` → `new GetUserPreferenceQuery(actor)`.
- The only client-readable identity shortcut is the module-owned `UserPreferenceCustomerActorResolver`
  Development/Testing-only `X-Tooba-Dev-Actor-User-Id` header (with an explicit `return null;` when not
  authenticated and not Development/Testing, plus a guest-actor fallback). It appears in **no** admin file.
- Contrast: `GetUiPreferenceQuery` is `VALIDATOR_REQUIRED` precisely because its route binds a
  non-`:guid` route parameter (`group.MapGet("/{key}", GetAsync)`) and the validator
  `GetUiPreferenceQueryValidator` enforces `RuleFor(x => x.Key).NotEmpty()`.

## 5. Mutation negative proof (pure in memory; no production file is modified or committed)

Mutation: replace the admin-operator GET dispatch with a request already in the matrix —

```text
new GetUserPreferenceQuery(actor)  ->  new UpsertUserPreferenceCommand(actor, "en")
```

This preserves the route count **and** the `ISender.Send` count **and** the dispatched type set.

```text
route count                    : 6 -> 6            (blind)
ISender.Send count             : 6 -> 6            (blind)
distinct dispatched type set   : 4 -> 4  identical (blind)
exact route -> request mapping : differs  -> CAUGHT
```

The mutation is exercised entirely in memory by
`Replacing_one_dispatched_request_while_preserving_route_and_send_counts_fails_the_exact_map`, which
asserts the count and set blindness and then the exact-map divergence. No production file is written.

Fail-closed proof: `Untraceable_dispatch_unknown_route_syntax_and_missing_send_all_fail_closed` asserts
that (a) an untraceable variable dispatch throws, (b) an unrecognised route-mapping syntax throws,
(c) a handler with no `ISender.Send` throws, and (d) a module whose route-group prefix set is not the
expected three groups throws.

## 6. New durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/UserPreferenceModuleAmsc001W3R1SetEqualityGuardTests.cs`
— 8 tests:

| Test | Locks |
|---|---|
| `Shipped_routes_and_actual_dispatched_requests_match_the_classified_matrix_exactly` | exact route→request equality over all 6 rows, derived from endpoint code; exactly one `Send` per route |
| `Dispatched_request_set_equals_the_3_required_plus_1_exempt_partition_with_no_orphan_or_duplicate` | set equality + real partition + 4 distinct requests + 1:1 handler mapping |
| `Validator_targets_derived_from_concrete_definitions_equal_the_three_required_requests` | validator targets from `AbstractValidator<T>`; none for the exempt request; stable codes only |
| `Exempt_request_rests_on_a_server_derived_actor_and_a_bodyless_route_only` | server-derived actor + bodyless/parameterless GET + admin has no dev header |
| `Replacing_one_dispatched_request_while_preserving_route_and_send_counts_fails_the_exact_map` | in-memory mutation negative proof (count/set blind, exact map catches) |
| `Untraceable_dispatch_unknown_route_syntax_and_missing_send_all_fail_closed` | fail-closed behaviour for all four ambiguity classes |
| `Validators_are_discoverable_through_the_real_cqrs_registration` | non-circular runtime DI: 3 resolved, 1 null, one `ValidationBehavior<,>` |
| `Certification_truth_and_the_w3_wave_evidence_are_preserved` | W3 guard/evidence intact, 6 module routes, Host zero routes, canonical `api.From(` |

## 7. Focused validation (exact counts)

| Command | Result |
|---|---|
| `UserPreferenceModuleAmsc001W3R1SetEqualityGuardTests` | **8 passed / 0 failed / 0 skipped** |
| `UserPreferenceModuleAmsc001*` + `UserPreferenceModuleAmc*` + `HostPreferencesAmcGuardTests` + `ErrorCatalogUniqueCodeGuardTests` | **48 passed / 0 failed / 0 skipped** |
| Wider `~TmarCompleteReferenceStructureGateTests｜~TmarDurableGuardTests｜~ErrorCatalogUniqueCodeGuardTests｜~UserPreference｜~HostPreferencesAmcGuardTests｜~TaxModuleAmsc001` | **79 passed / 3 failed / 0 skipped** (82 total) |
| Same wider filter at the starting head `1808ba55` (isolated `git worktree`) | **71 passed / 3 failed / 0 skipped** (74 total) |

`NEW = 0`. The 3 failures are byte-identical at both heads and are repository-global pre-existing pins
unrelated to UserPreference:

- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  — the documented out-of-scope `Tooba.Catalog.Contracts/Cart` + `Tooba.Cart.Contracts/{Checkout,Presentation}`
  project-level namespace deviation, and the stale `certifiedModules` list pin;
- `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` — stale repository-global
  `structureLock.certifiedModules` pin (the repository has 31 certified modules);
- `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
  — stale repository-global Master Recovery `Current Grid work checkpoint` pin.

No UserPreference test and no UserPreference guard fails.

## 8. Scope and preservation

| Item | State |
|---|---|
| Production files changed | `ZERO` |
| Files moved / renamed | `ZERO` |
| Schema / migrations | `UNCHANGED` |
| Endpoints / Contracts / validators | `UNCHANGED` |
| Shared foundations / Skill files | `UNCHANGED` |
| Host checkpoint / `lastAcceptedTask` / `workflowStop` | `PRESERVED` |
| `automaticNextImplementationTask` | `NONE` |
| Historical W3 evidence and certification | `PRESERVED_UNREWRITTEN` |
| Global certified-module list | `UNCHANGED` (31 entries; `UserPreference` still exactly once) |

Stop gate: `USER_REVIEW_USERPREFERENCE_AMSC_001_W3_R1`.
