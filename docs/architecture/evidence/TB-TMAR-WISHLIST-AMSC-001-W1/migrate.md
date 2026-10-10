# TB-TMAR-WISHLIST-AMSC-001-W1 — Migrate (`tooba-architecture-migrate`)

```text
TASK:          TB-TMAR-WISHLIST-AMSC-001-W1
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-migrate
TARGET:        src/backend/Modules/Wishlist/Tooba.Wishlist.*
PARENT:        TB-TMAR-WISHLIST-AMSC-001-W0  (commit e80045ca)
STARTING HEAD: e80045ca
STATE:         MIGRATE_COMPLETE
VERDICT:       READY_TO_STRUCTURE
LOCK VERSION:  ARCH-COMPLETE-002
```

W1 closes the four W0 blockers (B1–B4) with the smallest possible production delta: the module's
single canonical stable-code home gains a declared-code guard, the Application seam gains the
`ContractOperationException` arm, the one raw fault literal becomes a typed contract fault with a
declared code + descriptor + bilingual resource, and the bilingual logical names are locked. Behaviour
(routes, verbs, success payloads, wire-visible codes, domain invariants, schema, DI lifetimes, outbox
semantics, the four validators already present) is preserved exactly.

---

## 1. Stable-code home (B1 retired)

```text
BEFORE  Tooba.Wishlist.Contracts/Errors/WishlistErrorCodes.cs   3 module codes + Foundation SessionRequired, NO guard
AFTER   Tooba.Wishlist.Contracts/Errors/WishlistErrorCodes.cs   4 module codes + Foundation SessionRequired + KnownCodes + IsKnown
```

`KnownCodes` is an ordinal `HashSet<string>` over the four **Wishlist-owned** codes:

```text
customer.wishlist.product_unavailable
customer.wishlist.product_id_required
customer.wishlist.product_ids_required
wishlist.outbox.unmapped_event_type
```

`IsKnown(string? code)` is null/empty/whitespace-safe and is the only classifier the seam uses.
`customer.session.required` (`SessionRequired`) deliberately stays **out** of `KnownCodes`: its
descriptor and both-culture resources are Foundation-owned (`FoundationErrorCatalogContributor`) and
it is not a Wishlist use-case fault. The constant stays in the module so the HTTP boundary can
consume it, exactly as `Support`/`Returns`/`Wallet` do.

The home stays in `Tooba.Wishlist.Contracts` because the certified Host guards
(`WishlistModuleAmcW1/W2/W3/W4CertGuardTests`, `HostWishlistAmcGuardTests`, `WishlistFoundationTests`)
pin that location — recorded in W0 §18 as an architecture constraint, not a preference.

## 2. Typed-fault seam (B3 retired)

`Tooba.Wishlist.Application/Composition/WishlistOperation.cs` now maps **both** typed, code-carrying
fault mechanisms by declared code only:

```text
Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
Task<Result>    ExecuteAsync(Func<Task> action)
```

```text
catch (ContractOperationException ex) when (WishlistErrorCodes.IsKnown(ex.Code))  -> Result failure (new)
catch (SemanticException ex)                                                     -> Result failure (unchanged)
```

An unknown contract code, and every unrelated exception, is **rethrown** and reaches the canonical
global exception boundary — never hidden as a business error. There is zero message-text
classification: no `ex.Message`, no `.Message.Contains(`, no `StartsWith("wishlist.` heuristic.
`ArgumentNullException.ThrowIfNull(action)` was added on both overloads (defensive, non-observable on
the real call paths, matching the certified `*Operation` seam shape).

## 3. Raw fault literal retired (B2 retired)

```text
BEFORE  Tooba.Wishlist.Infrastructure/Persistence/WishlistOutboxRegistration.GetEventTypeName
        throw new InvalidOperationException("Wishlist integration event is not registered.");
AFTER   throw new ContractOperationException(WishlistErrorCodes.OutboxUnmappedEventType);
```

`wishlist.outbox.unmapped_event_type` is declared in the Contracts home, registered exactly once by
`WishlistErrorCatalogContributor` (`ErrorClassification.Platform`, HTTP 500), and localized in both
cultures. `Translate(...)` and `ResolveEventClrType(...)` still return `null`, so outbox
observability and the message-bus surface are unchanged. Zero raw `InvalidOperationException("…")`
literals remain anywhere in Wishlist production.

## 4. Bilingual resource coverage + locked logical names (B4 retired)

```text
WishlistErrors.resx     3 keys -> 4 keys (EN)
WishlistErrors.fa.resx  3 keys -> 4 keys (FA)
```

Every resx key is a real declared code — there is no orphan localization key and no declared
Wishlist-owned code without a translation. `WishlistErrorResourceSet.Owns` is prefix-based over the
two Wishlist-owned keyspaces:

```text
customer.wishlist.
wishlist.
```

`Tooba.Wishlist.Contracts.csproj` now pins the explicit `EmbeddedResource` + `LogicalName` pair so the
bilingual logical names are **locked** rather than left to SDK convention:

```text
Tooba.Wishlist.Contracts.Resources.WishlistErrors.resources
Tooba.Wishlist.Contracts.Resources.WishlistErrors.fa.resources
```

This matters for microservice extraction: a resource-set that resolves by convention can silently
stop resolving once the assembly is renamed or the folder is folded differently.

## 5. Validator matrix (unchanged — W0 already proved it exhaustive)

W0 proved `4 routes == 4 endpoint-reachable requests == 3 VALIDATOR_REQUIRED + 1
NO_VALIDATOR_REQUIRED`, and W1 verified the three transport validators are present and discoverable:

| Request | Validator | State |
| --- | --- | --- |
| `AddWishlistItemCommand` | `AddWishlistItemCommandValidator` | present |
| `RemoveWishlistItemCommand` | `RemoveWishlistItemCommandValidator` | present |
| `GetWishlistMembershipQuery` | `GetWishlistMembershipQueryValidator` | present |
| `ListWishlistPageQuery` | — | proven `NO_VALIDATOR_REQUIRED` (server-derived paging/actor, no caller-controlled input) |

The validators emit stable machine codes (`WishlistErrorCodes.*`) and emit **no** `WithMessage(...)`
prose. `WishlistModuleAmsc001W1MigrateGuardTests` proves the three validators resolve from a real
container built by the canonical `AddToobaCqrsFoundation` registration for the Wishlist Application
assembly, and that the exempt request resolves to `null` — a non-circular proof of the matrix.

## 6. Boundary / coupling

```text
Cross-Module-Coupling-State = NONE
Cross-Module-Join-State     = NONE
```

- Zero foreign `Tooba.<Other>.{Application|Infrastructure|Domain|Endpoints}` using-directive in any
  Wishlist production file.
- Zero foreign `*DbContext` reference in any Wishlist production file.
- `Tooba.Wishlist.Endpoints` references `Wishlist.Application` + BuildingBlocks only — never
  `Wishlist.Infrastructure`, never `Tooba.Host`.
- `Tooba.Wishlist.Domain` references BuildingBlocks + its own `Tooba.Wishlist.Contracts` only.
- The customer endpoint consumes `WishlistErrorCodes.SessionRequired`; no Wishlist-owned duplicate of
  the session code exists.
- Persistence stays a single `WishlistDbContext` over schema `wishlist` (one table + outbox).

## 7. Durable W1 guard — `WishlistModuleAmsc001W1MigrateGuardTests`

```text
1  Stable_codes_live_in_the_single_canonical_contracts_errors_home
2  Declared_code_catalog_has_the_declared_code_guard                (5 declared = 4 owned + SessionRequired)
3  Typed_fault_seam_maps_declared_codes_and_never_parses_message_text
4  Raw_outbox_fault_is_retired_for_a_typed_contract_code            (throws + code identity proven at runtime)
5  Error_localization_resource_set_is_wishlist_owned_and_bilingual  (4 EN + 4 FA + locked logical names)
6  Transport_validators_cover_the_three_caller_controlled_requests
7  Validators_are_discoverable_through_the_real_cqrs_registration   (real container, non-circular)
8  Customer_endpoints_use_the_foundation_session_code_and_host_has_no_wishlist_authority
9  Wishlist_never_references_foreign_application_infrastructure_or_domain
10 Wishlist_production_never_classifies_faults_by_message_text_or_raw_literal
11 Migration_did_not_change_the_wishlist_schema_migrations
```

Assertions locate files **by name** (not by technical-axis path), so W2 can folder the module without
weakening this guard. The guard never asserts an absolute file count, so W2 may split files freely.

## 8. Behaviour-preservation baseline (unchanged)

| Surface | State |
| --- | --- |
| 4 routes / verbs / group prefix `/v1/customer/wishlist` / templates | unchanged |
| Success response shapes | unchanged |
| The three client-observable outcome codes | byte-identical |
| Domain invariants | unchanged |
| Authorization + actor-resolution order | unchanged |
| Schema `wishlist` + `20260825162434_InitialWishlist` | unchanged (no migration/designer/snapshot touched) |
| Outbox `Translate`/`ResolveEventClrType`/`Schema`/`TableName` | unchanged |
| DI lifetimes | unchanged |

The only observable delta is that the previously **untyped** outbox fault now carries a declared code
(`wishlist.outbox.unmapped_event_type`) instead of prose. That path is unreachable through the four
HTTP routes (it fires only on an unregistered integration event type), so no wire-visible response
changes.

## 9. Verification

```text
dotnet build  src/backend/Host/Tooba.Host.Tests                                      SUCCEEDED 0 errors
dotnet test   Tooba.Host.Tests --filter WishlistModuleAmsc001W1MigrateGuardTests      11 passed / 0 failed
dotnet test   Tooba.Host.Tests --filter Wishlist                                      28 passed / 1 skipped / 0 failed
```

## 10. Structured state fields

```text
StableCodeHomeState          : CONTRACTS_OWNED_4_DECLARED_4_OWNED_1_FOUNDATION_SHARED
DeclaredCodeGuardState       : PRESENT_KNOWNCODES_ISKNOWN
TypedFaultSeamState          : WISHLISTOPERATION_CONTRACTOPERATIONEXCEPTION_ARM_PLUS_SEMANTICEXCEPTION_ARM
MessageClassificationState   : ZERO
RawFaultState                : ZERO_RAW_INVALID_OPERATION_EXCEPTION_LITERALS
CatalogOwnershipState        : 4_DESCRIPTORS_REGISTERED_EXACTLY_ONCE_SESSION_REQUIRED_NOT_REGISTERED
LocalizationState            : CANONICAL_4_EN_4_FA_EXPLICIT_LOCKED_LOGICAL_NAMES
ResourceSetOwnershipState    : PREFIX_BASED_CUSTOMER_WISHLIST_AND_WISHLIST_KEYSPACES
ValidatorMatrixState         : EXHAUSTIVE_3_OF_3_REQUIRED_PLUS_1_NO_VALIDATOR_REQUIRED
CrossModuleCouplingState     : NONE
CrossModuleJoinState         : NONE
PersistenceOwnershipState    : SINGLE_WISHLIST_SCHEMA_ONE_TABLE_PLUS_OUTBOX
SchemaState                  : UNCHANGED
BehaviorPreservation         : PRESERVED
MicroserviceExtractable      : TRUE_PENDING_W2_STRUCTURE
GuardWeakened                : NONE
ProductionCodeChanged        : TRUE
HostTouched                  : FALSE
StopGate                     : USER_REVIEW_WISHLIST_AMSC_001_W1
```

## 11. Destination-integrity check

`HEAD == origin/main == e80045ca` at W1 start. W1 changes only
`src/backend/Modules/Wishlist/**`, one new guard file under
`src/backend/Host/Tooba.Host.Tests/Architecture/**`, and the SoT/evidence for this task. No Host
production file was modified. No manifest mutation, no schema change, no baseline widening, no
`structureLock` mutation (Wishlist stays a member of the 32-module certified set).
