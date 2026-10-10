# TB-TMAR-WISHLIST-AMSC-001-W3 — Certify (`tooba-architecture-certify`)

```text
TASK:          TB-TMAR-WISHLIST-AMSC-001-W3
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-certify
TARGET:        src/backend/Modules/Wishlist/Tooba.Wishlist.*
PARENT:        TB-TMAR-WISHLIST-AMSC-001-W2
PARENT COMMIT: 03871df6
STARTING HEAD: 03871df68d3204214680afec9aae7992dc481b2e
STATE:         WISHLIST_AMSC_001_CERTIFIED
VERDICT:       COMPLETE_REFERENCE_PATTERN
STRUCTURE:     STRUCTURE_CERTIFIED
LOCK VERSION:  ARCH-COMPLETE-002
STOP GATE:     USER_REVIEW_WISHLIST_AMSC_001_W3
```

Final objective stated up front: **Wishlist must be extractable as an independent microservice**, so the
certification gates are *zero invalid coupling* and an *exact path↔namespace* boundary. Wishlist was
already a member of `structureLock.certifiedModules` (**32**) from the earlier
`TB-TMAR-WISHLIST-AMC-001` lineage, so W3 **refreshes that entry in place** with the AMSC-001 W0→W3
truth and leaves the global lock count unchanged. No pre-cert duplicate was created and no prior guard
was weakened.

---

## 1. `HTTP_OWNING` applicability gate (certify §0b)

Wishlist owns **real, frontend-consumed** HTTP routes, so the endpoint/CQRS/validator gates apply in
full and are asserted rather than excused.

| | Value |
| --- | --- |
| Module-owned route groups | **1** |
| Module-owned routes | **4** |
| Host-owned Wishlist routes | **0** |
| Endpoint-reachable requests | **4** |
| Endpoint ownership state | `MODULE_OWNED` |

Route group and templates:

```text
/v1/customer/wishlist     GET /   POST /{productId:guid}   DELETE /{productId:guid}   POST /membership
```

`Host/Tooba.Host/Program.cs` maps the module boundary only (`MapWishlistModuleEndpoints()`) and declares
zero Wishlist route templates; `src/backend/Host/Tooba.Host/Wishlist` does not exist. Host owns **zero**
Wishlist routes (`hostOwnedRouteCount = 0`).

## 2. CQRS

**COMPLIANT 4/4.** Every endpoint-reachable request is a real `IRequest<Result<...>>` record with a
real `IRequestHandler<,>`, dispatched from a thin endpoint through `ISender`:

| # | Route + verb | Request | Kind | Validator |
| --- | --- | --- | --- | --- |
| 1 | `GET /v1/customer/wishlist` | `ListWishlistPageQuery` | Query | — |
| 2 | `POST /v1/customer/wishlist/{productId:guid}` | `AddWishlistItemCommand` | Command | `AddWishlistItemCommandValidator` |
| 3 | `DELETE /v1/customer/wishlist/{productId:guid}` | `RemoveWishlistItemCommand` | Command | `RemoveWishlistItemCommandValidator` |
| 4 | `POST /v1/customer/wishlist/membership` | `GetWishlistMembershipQuery` | Query | `GetWishlistMembershipQueryValidator` |

No endpoint business logic, no direct Directory/DbContext call from an endpoint, no custom dispatcher,
no Host bypass. MediatR registration goes through `AddToobaCqrsFoundation` (MediatR **12.5.0**,
`TracingBehavior` → `ValidationBehavior` → `LoggingBehavior`) for the `Tooba.Wishlist.Application`
assembly. Every handler routes its fault through the canonical `WishlistOperation` seam.

## 3. Certify §6a hard blocker — independent input-provenance / set-equality gate

The gate is **derived from the endpoint source itself**, not from the classification table: the guard
parses every `group.Map{Get,Post,...}("<template>", <Handler>)` mapping (with the module-owned group
prefix read from `WishlistEndpointModule.MapGroup`), brace-extracts the handler body, traces the
argument actually passed to `ISender.Send(...)` (inline construction or the traced local construction
of a variable), and compares the resulting **verb → path → request** map against the classified matrix
row by row. Count-only and set-only guards are explicitly proven insufficient by a mutation test.

```text
4 shipped routes == 4 ISender.Send call sites == 4 dispatched request types
                 == 3 VALIDATOR_REQUIRED + 1 NO_VALIDATOR_REQUIRED (disjoint, no orphan, no duplicate)
4 routes → 4 sends → 4 requests → exactly 1 record declaration + 1 IRequestHandler<,> each
```

- `Route-Request-Set-Equality-State`: `EXACT_SOURCE_DERIVED_4_OF_4_ENFORCED`.
- **Fail-closed**: an untraceable dispatch (a variable whose construction cannot be resolved, or a
  handler with no `Send`) throws rather than silently passing.
- **Mutation proof**: replacing the list dispatch with a request already present in the matrix keeps
  both the send count and the dispatched request *set* unchanged, yet the exact route→request map
  differs — and the guard fails. This is the anti-`COUNT_ONLY_GAP` evidence.

## 4. Validator matrix — `EXHAUSTIVE 4/4`

Set equality holds between the reachable request set and the classified set: **3
`VALIDATOR_REQUIRED`** (validators present) + **1 `NO_VALIDATOR_REQUIRED`**.

`NO_VALIDATOR_REQUIRED` reason (independently asserted, not assumed):

| Request | Reason |
| --- | --- |
| `ListWishlistPageQuery` | binds no client-controlled transport shape: its single argument is the server-derived actor resolved by the module actor-resolver seam and the route carries no parameter. The two `:guid` routes are routing-constrained so a malformed id never reaches a handler; the membership body collection carries a validator and is additionally clamped at the endpoint. |

The guard asserts that **every** route template's placeholder ends with `:guid`, that actors are
constructed from the resolver result, and that the resolver is a neutral seam over the canonical
authenticated principal (`ICurrentAuthenticatedUser` / `StorefrontGuestActor.ActorId`) — so no exemption
rests on an unproven claim. Validator discovery is proven **non-circularly**: the three validators are
resolved from a real container built by `AddToobaCqrsFoundation` for the Wishlist Application assembly,
the one exempt type resolves to `null`, and `ValidationBehavior<,>` is installed exactly once.
Validators emit stable `customer.wishlist.*` machine codes only — zero `WithMessage(` prose.

## 5. Stable codes, descriptors and localization

| | Value |
| --- | --- |
| Declared constants (`WishlistErrorCodes`) | **5** |
| Wishlist-owned codes (`KnownCodes` / `IsKnown`) | **4** |
| Foundation shared code (consumable, not owned) | **1** (`customer.session.required`) |
| Registered descriptors | **4** |
| EN resource keys | **4** |
| FA resource keys | **4** |
| Raw prose faults | **0** |

- Single canonical home: `Tooba.Wishlist.Contracts/Errors/WishlistErrorCodes.cs`
  (`Tooba.Wishlist.Contracts.Errors`) with the declared-code guard (`KnownCodes` HashSet + `IsKnown`).
- One descriptor per module-owned code, each `LocalizationKey == Code`, all resolving through the
  composed `ErrorDefinitionCatalog` with a unique owner. The guard proves the composed catalog's
  Wishlist-owned `customer.wishlist.*` + `wishlist.*` keyspace equals the four declared codes exactly.
- `customer.session.required` stays **Foundation-owned**: the constant is consumed at the customer
  endpoint `api.FromFailure` site but `IsKnown("customer.session.required") == false` and its descriptor
  is owned by `FoundationErrorCatalogContributor`. Duplicate usage allowed, duplicate descriptor
  ownership not.
- Bilingual resources carry **4 EN + 4 FA** keys == the 4 declared module codes, resolving through the
  canonical composed `ResourceErrorMessageLocalizer` with **real Persian text verified per code**, and
  the explicit `EmbeddedResource`/`LogicalName` pair is pinned in `Tooba.Wishlist.Contracts.csproj`.
- Registration happens **exactly once** by the module composition root (`WishlistModule`): one
  `IErrorCatalogContributor`, one `IErrorResourceSet`.

## 6. Typed-fault seam

`WishlistOperation` classifies only by typed code —
`catch (ContractOperationException ex) when (WishlistErrorCodes.IsKnown(ex.Code))` plus
`catch (SemanticException ex)` — in both the value and value-less overloads. Zero `ex.Message`, zero
`.Message.Contains(`/`StartsWith`/`==`, zero `InvalidOperationException("<literal>")`, zero
`Results.Json`/`Results.BadRequest`/`Results.Problem` and zero local `ProblemDetails` builder anywhere in
the production surface. The 4 endpoint responses all go through `api.From(...)` (1 of them the
canonical `api.Created(...)` on the add path, and 4 `api.FromFailure(...)` session guards).

## 7. Contracts-only microservice boundary

Every project edge is own-module layering or the `Tooba.BuildingBlocks` / `Tooba.ModuleContracts` /
`Tooba.Persistence` foundations, with only Contracts-only foreign edges:
`Tooba.Wishlist.Application` + `.Infrastructure` → `Tooba.Catalog.Contracts` (the canonical lookup
seams) and `Tooba.Wishlist.Endpoints` + `.Infrastructure` → `Tooba.Order.Contracts` (the Fulfillment
session seam). Zero foreign module `Application`/`Infrastructure`/`Domain`/`Endpoints` reference, zero
foreign DbContext, zero cross-module persistence reach-through, zero cross-module EF/SQL join, zero
`TypeForwardedTo`, zero namespace alias. `Endpoints` never reaches `Infrastructure`/`Host`; `Domain`
depends only on `BuildingBlocks` + its own `Contracts`. `microserviceExtractable = true`.

## 8. Structure (defense in depth; authority stays W2)

`PROFESSIONAL_SHALLOW` capability-first folders (`Customer/{Commands,Queries}/`, `Composition/`,
`Models/`, `Ports/`, `Validation/`); `Path-Namespace-State = EXACT` for every production file;
`Root-Allowlist-State = ENFORCED` (Contracts/Domain/Application empty; Endpoints
`WishlistEndpointModule.cs`; Infrastructure `WishlistModule.cs`); `Physical-Copy-State = CLEAN`;
`Solution-Explorer-State = CANONICAL` (`/Modules/Wishlist/` with exactly five projects);
`File-Cohesion-State = COHESIVE`; `God-File-State = NONE` (largest production file is the 90-LOC
`WishlistDirectory.cs`).

## 9. Schema / migrations preserved

The `wishlist` schema, the `wishlist_items` table plus the module outbox, and the single migration
`20260825162434_InitialWishlist` (+ designer + snapshot) are byte-identical to the W0 baseline. The
outbox `Translate`/`ResolveEventClrType` still return `null` (no external event this version). No
migration, designer, snapshot or DbContext file was touched by any wave.

## 10. Host final closure preserved

Zero Host production file added, moved or widened by W3. `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` /
`HOST_ROOT_FINAL_CERTIFIED` are untouched; `lastAcceptedTask` stays
`TB-TMAR-HOST-ROOT-FINAL-CERT-001` and `currentHostCheckpoint` stays `HOST_ROOT_FINAL_CERTIFIED`.

## 11. Manifest certification

The existing `modules[Wishlist]` entry was **refreshed in place** (not duplicated):

```text
structureCertified       : true
lockVersion              : ARCH-COMPLETE-002
structureAuthorityTask   : TB-TMAR-WISHLIST-AMSC-001-W2
structureAuthorityCommit : 03871df68d3204214680afec9aae7992dc481b2e
structureHandoffState    : READY_FOR_CERTIFY_CONSUMED_BY_W3
certificationState       : WISHLIST_AMSC_001_CERTIFIED
currentCertificationTask : TB-TMAR-WISHLIST-AMSC-001-W3
currentVerdict           : COMPLETE_REFERENCE_PATTERN
certificationNote        : AMSC-001 W0→W3 lineage
```

`preCertModules` and `uncertifiedHttpOwningModules` are both **empty**; `structureLock.certifiedModules`
stays **32** with `Wishlist` present exactly once. The patch script byte-compares every other module
entry and every manifest-level key before writing, so the surgical edit cannot rewrite unrelated
modules.

## 12. Guards strengthened, never weakened

- New `WishlistModuleAmsc001W3CertGuardTests` (**13 facts**) locks the manifest/SoT certification and
  wave lineage, the HTTP_OWNING four-route ownership with a zero Host route count, the §6a
  source-derived route→request provenance set equality (plus the count-preserving mutation and
  fail-closed proofs), the validator matrix and non-circular validator discovery, the canonical
  API-result/typed-fault/catalog mechanisms, the bilingual locked resources and composed-catalog
  uniqueness, the single-owner descriptors, the Contracts-only microservice boundary, the unchanged
  schema/migration set, and the preserved Host closure + evidence tree + Master Recovery checkpoint.
- W1 guard (**11/11**) and W2 guard (**8/8**) re-run green — W1/W2 semantics preserved, not weakened.
- No assertion was deleted, relaxed or repointed away from a real invariant; no baseline widened.

## 13. Verification

```text
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj   -> SUCCEEDED, 0 errors
dotnet test --filter FullyQualifiedName~Wishlist                        -> 49 passed / 1 skipped / 0 failed
             (includes WishlistModuleAmsc001W3CertGuardTests 13/13, W1 11/11, W2 8/8, the prior
              AMC W1/W2/W3/W4 + HostWishlistAmc + WishlistFoundation guards)
dotnet test --filter (Wishlist | ErrorCatalogUniqueCodeGuardTests | TmarCompleteReferenceStructureGateTests
             | TmarDurableGuardTests | HostWishlistAmcGuardTests | HostDevelopmentMigrationSeamGuardTests)
             -> 64 passed / 1 skipped / 3 failed (after W3)
             -> 51 passed / 1 skipped / 3 failed (at the W2 head 03871df6, isolated git worktree)
             -> NEW = 0, FIXED = 0
dotnet test --filter FullyQualifiedName~Architecture -> 1416 passed / 42 failed (after W3)
             -> 1416 passed / 42 failed (at the W2 head) ; set-difference of failing ids NEW = 0, FIXED = 0
```

**Baseline proof (`NEW = 0`).** The same filters were run in an isolated `git worktree` at the W2 head
`03871df68d3204214680afec9aae7992dc481b2e`:

| Filter | W2 head `03871df6` | After W3 | Δ |
| --- | --- | --- | --- |
| Focused Wishlist/cert set | 51 passed / 1 skipped / **3 failed** | 64 passed / 1 skipped / **3 failed** | +13 passed, failures unchanged |
| `~Architecture` | 1416 passed / **42 failed** | 1416 passed / **42 failed** | failures unchanged |

Set difference of the failing test **ids** is **empty in both directions** (`NEW_FAILURES = 0`,
`FIXED = 0`). The 42 repository-global failures are pre-existing and identical at both heads:

- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  — the documented out-of-scope `Tooba.Catalog.Contracts/Cart` + `Tooba.Cart.Contracts/{Checkout,Presentation}`
  project-level namespace deviation.
- `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` and
  `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
  — repository-global recovery pins unrelated to Wishlist.
- The remaining 39 are the Host/Admin `StoreAppearance`/PW count-drift guards and stale
  certified-module/SOT pins from earlier Catalog evacuation waves, unchanged by Wishlist.

**Zero new failures, zero regressions, zero Wishlist test failures.**

```text
Applicability               : HTTP_OWNING
Endpoint-Ownership-State    : MODULE_OWNED (4 routes / 1 group, Host 0)
CQRS-State                  : COMPLIANT_4_OF_4
Validator-Matrix-State      : EXHAUSTIVE_3_VALIDATOR_REQUIRED_1_NO_VALIDATOR_REQUIRED
Route-Request-Set-Equality  : EXACT_SOURCE_DERIVED_4_OF_4_ENFORCED
Stable-Error-Code-State     : SINGLE_CANONICAL_CONTRACTS_HOME_5_DECLARED_4_OWNED_1_FOUNDATION_SHARED
Declared-Code-Guard-State   : PRESENT
Typed-Fault-Seam-State      : WISHLISTOPERATION_TYPED_CODE_ONLY
Raw-Prose-Fault-State       : ZERO
Localization-State          : CANONICAL_4_EN_4_FA_EXPLICIT_LOCKED_LOGICAL_NAMES
Path-Namespace-State        : EXACT
Root-Allowlist-State        : ENFORCED
Alias-Workaround-State      : NONE
Folder-Granularity-State    : PROFESSIONAL_SHALLOW
Solution-Explorer-State     : CANONICAL
Physical-Copy-State         : CLEAN
Foreign-App-Infra-Domain    : ZERO
Cross-Module-Join-State     : ZERO
Schema-State                : UNCHANGED
Host-Final-Closure-State    : HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED
Microservice-Extractable    : TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE_EXACT_PATH_NAMESPACE
Guards-Weakened             : NONE
Baselines-Widened           : NONE
Verdict                     : COMPLETE_REFERENCE_PATTERN
```

## 14. Stop gate

`USER_REVIEW_WISHLIST_AMSC_001_W3`; `automaticNextImplementationTask = NONE`; own cert commit SHA
reported through the Bridge Result channel only.
