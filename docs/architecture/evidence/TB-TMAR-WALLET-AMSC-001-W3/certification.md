# TB-TMAR-WALLET-AMSC-001-W3 — Certify (`tooba-architecture-certify`)

```text
TASK:          TB-TMAR-WALLET-AMSC-001-W3
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-certify
TARGET:        src/backend/Modules/Wallet/Tooba.Wallet.*
PARENT:        TB-TMAR-WALLET-AMSC-001-W2
PARENT COMMIT: 779cd2f4
STARTING HEAD: 779cd2f4f08cbc5bd90741384d8cef7d4a049e96
STATE:         WALLET_AMSC_001_CERTIFIED
VERDICT:       COMPLETE_REFERENCE_PATTERN
STRUCTURE:     STRUCTURE_CERTIFIED
LOCK VERSION:  ARCH-COMPLETE-002
STOP GATE:     USER_REVIEW_WALLET_AMSC_001_W3
```

Final objective stated up front: **Wallet must be extractable as an independent microservice**, so the
certification gates are *zero invalid coupling* and an *exact path↔namespace* boundary. Wallet was the
**last outstanding** `uncertifiedHttpOwningModules` entry; W3 promotes it into the certified `modules[]`
array with `structureCertified: true` and adds it to `structureLock.certifiedModules` (**32**). No
pre-cert duplicate was created and no prior guard was weakened.

---

## 1. `HTTP_OWNING` applicability gate (certify §0b)

Wallet owns **real, frontend-consumed** HTTP routes, so the endpoint/CQRS/validator gates apply in full
and are asserted rather than excused.

| | Value |
| --- | --- |
| Module-owned route groups | **3** |
| Module-owned routes | **11** |
| Host-owned Wallet routes | **0** |
| Endpoint-reachable requests | **11** |
| Endpoint ownership state | `MODULE_OWNED` |

Route groups and templates:

```text
/v1/customer/wallet                        GET /   GET /ledger   POST /gift-cards/redeem
/v1/admin/gift-cards                       GET /   POST /   GET /{cardId:guid}   POST /{cardId:guid}/revoke
/v1/admin/wallets/{customerActorUserId:guid}   GET /   GET /ledger   POST /adjustments
/v1/admin/wallet/demo-preview              GET /
```

`Host/Tooba.Host/Program.cs` maps the module boundary only (`MapWalletEndpoints()`) and declares zero
Wallet route templates; `src/backend/Host/Tooba.Host/Wallet` does not exist. Host owns **zero** Wallet
routes (`hostOwnedRouteCount = 0`).

## 2. CQRS

**COMPLIANT 11/11.** Every endpoint-reachable request is a real `IRequest<Result<...>>` record with a
real `IRequestHandler<,>`, dispatched from a thin endpoint through `ISender`:

| # | Route + verb | Request | Kind | Validator |
| --- | --- | --- | --- | --- |
| 1 | `GET /v1/customer/wallet` | `GetCustomerWalletSummaryQuery` | Query | — |
| 2 | `GET /v1/customer/wallet/ledger` | `ListCustomerWalletLedgerQuery` | Query | — |
| 3 | `POST /v1/customer/wallet/gift-cards/redeem` | `RedeemCustomerGiftCardCommand` | Command | `RedeemCustomerGiftCardCommandValidator` |
| 4 | `GET /v1/admin/gift-cards` | `ListAdminGiftCardsQuery` | Query | `ListAdminGiftCardsQueryValidator` |
| 5 | `POST /v1/admin/gift-cards` | `IssueAdminGiftCardCommand` | Command | `IssueAdminGiftCardCommandValidator` |
| 6 | `GET /v1/admin/gift-cards/{cardId:guid}` | `GetAdminGiftCardQuery` | Query | — |
| 7 | `POST /v1/admin/gift-cards/{cardId:guid}/revoke` | `RevokeAdminGiftCardCommand` | Command | — |
| 8 | `GET /v1/admin/wallets/{customerActorUserId:guid}` | `GetAdminWalletQuery` | Query | — |
| 9 | `GET /v1/admin/wallets/{customerActorUserId:guid}/ledger` | `ListAdminWalletLedgerQuery` | Query | — |
| 10 | `POST /v1/admin/wallets/{customerActorUserId:guid}/adjustments` | `AdjustAdminWalletCommand` | Command | `AdjustAdminWalletCommandValidator` |
| 11 | `GET /v1/admin/wallet/demo-preview` | `GetWalletDemoPreviewQuery` | Query | — |

No endpoint business logic, no direct Directory/DbContext call from an endpoint, no custom dispatcher,
no Host bypass. MediatR registration goes through `AddToobaCqrsFoundation` (MediatR **12.5.0**,
`TracingBehavior` → `ValidationBehavior` → `LoggingBehavior`) for the `Tooba.Wallet.Application`
assembly. Every handler routes its fault through the canonical `WalletOperation` seam.

## 3. Certify §6a hard blocker — independent input-provenance / set-equality gate

The gate is **derived from the endpoint source itself**, not from the classification table: the guard
parses every `app.Map{Get,Post,...}("<template>", <Handler>)` mapping, brace-extracts the handler body,
traces the argument actually passed to `ISender.Send(...)` (inline construction or the traced local
construction of a variable), and compares the resulting **audience → verb → path → request** map
against the classified matrix row by row. Count-only and set-only guards are explicitly proven
insufficient by a mutation test.

```text
11 shipped routes == 11 ISender.Send call sites == 11 dispatched request types
                  == 4 VALIDATOR_REQUIRED + 7 NO_VALIDATOR_REQUIRED (disjoint, no orphan, no duplicate)
11 routes → 11 sends → 11 requests → exactly 1 record declaration + 1 IRequestHandler<,> each
```

- `Route-Request-Set-Equality-State`: `COUNT_ONLY_GAP` → `EXACT_SOURCE_DERIVED_11_OF_11_ENFORCED`.
- **Fail-closed**: an untraceable dispatch (a variable whose construction cannot be resolved, or a
  handler with no `Send`) throws rather than silently passing.
- **Mutation proof**: replacing the customer-summary dispatch with a request already present in the
  matrix keeps both the send count and the dispatched request *set* unchanged, yet the exact
  route→request map differs — and the guard fails. This is the anti-`COUNT_ONLY_GAP` evidence.

## 4. Validator matrix — `EXHAUSTIVE 4/4`

Set equality holds between the reachable request set and the classified set: **4
`VALIDATOR_REQUIRED`** (validators present) + **7 `NO_VALIDATOR_REQUIRED`**.

`NO_VALIDATOR_REQUIRED` reasons (each independently asserted, not assumed):

| Request | Reason |
| --- | --- |
| `GetCustomerWalletSummaryQuery`, `ListCustomerWalletLedgerQuery` | actor server-derived by the module authorizer seam; `page`/`pageSize` bounded by the executed canonical directory clamp |
| `GetAdminGiftCardQuery`, `RevokeAdminGiftCardCommand`, `GetAdminWalletQuery`, `ListAdminWalletLedgerQuery` | route-constrained `:guid` identifiers (a non-guid path segment never reaches the handler) |
| `GetWalletDemoPreviewQuery` | parameterless; additionally `Development`-gated at the endpoint (`Results.NotFound()` outside Development) |

The guard asserts that **every** route template's placeholder ends with `:guid`, that actors/parties are
constructed from the authorizer result, and that the Development gate is present — so no exemption
rests on an unproven claim. Validator discovery is proven **non-circularly**: the four validators are
resolved from a real container built by `AddToobaCqrsFoundation` for the Wallet Application assembly,
the seven exempt types resolve to `null`, and `ValidationBehavior<,>` is installed exactly once.
Validators emit stable `wallet.validation.*` machine codes only — zero `WithMessage(` prose — and no
validation code is a catalog descriptor.

## 5. Stable codes, descriptors and localization

| | Value |
| --- | --- |
| Declared codes (`WalletErrorCodes`) | **50** |
| HTTP-reachable (client-observable) | **10** |
| Domain invariants (typed fault, no HTTP descriptor) | **40** |
| Registered descriptors | **10** |
| EN resource keys | **50** |
| FA resource keys | **50** |
| Raw prose faults | **0** |
| Legacy base64 codes | **0** |

- Single canonical home: `Tooba.Wallet.Contracts/Errors/WalletErrorCodes.cs`
  (`Tooba.Wallet.Contracts.Errors`) with the declared-code guard (`HttpReachableCodes` /
  `DomainInvariantCodes` HashSets + `IsKnown` / `IsHttpReachable` / `IsDomainInvariant`).
- One descriptor per module-owned HTTP-reachable code, each `LocalizationKey == Code`, all resolving
  through the composed `ErrorDefinitionCatalog` with a unique owner (repo-global
  `ErrorCatalogUniqueCodeGuardTests` stays green). The guard proves the composed catalog's Wallet-owned
  `wallet.*` + `giftcard.*` keyspace equals `WalletErrorCodes.HttpReachable` exactly, and that **no**
  domain-only invariant is catalogued.
- `customer.session.required` stays **Foundation-owned**: the constant is consumed at the customer
  endpoint `api.FromFailure` site via `FoundationErrorCodes.CustomerSessionRequired` but
  `IsKnown("customer.session.required") == false` and its descriptor is never claimed. Duplicate usage
  allowed, duplicate descriptor ownership not.
- Bilingual resources carry **50 EN + 50 FA** keys == the 50 declared codes, resolving through the
  canonical composed `ResourceErrorMessageLocalizer` with **real Persian text verified per code**, and
  the explicit `EmbeddedResource`/`LogicalName` pair is pinned so the contract is locked rather than
  implicit.
- Registration happens **exactly once** by the module endpoint presentation entry
  (`WalletEndpointModule.AddWalletEndpointPresentation`): one `IErrorCatalogContributor`, one
  `IErrorResourceSet`.

## 6. Typed-fault seam

`WalletOperation` classifies only by typed code —
`catch (ContractOperationException ex) when (WalletErrorCodes.IsKnown(ex.Code))` plus
`catch (SemanticException ex)` — in both the value and value-less overloads, with
`NotFoundIfNull` and `ResolveOutcomeCode` (HTTP-reachable → typed code, else
`publicOutcomeCode ?? typedCode`). Zero `ex.Message`, zero `.Message.Contains(`/`StartsWith`/`==`,
zero `IResult`, zero `InvalidOperationException("<literal>")`, zero `Results.Json`/
`Results.BadRequest`/`Results.Problem` and zero local `ProblemDetails` builder anywhere in the
production surface. The retired `WalletExceptionMapper` and the `Application/Errors/` folder are
absent. The single remaining `Results.NotFound()` is the locked Development-gated demo-preview
precondition; the 11 endpoint responses all go through `api.From(...)`.

## 7. Contracts-only microservice boundary

Every project edge is own-module layering or the `Tooba.BuildingBlocks` / `Tooba.ModuleContracts` /
`Tooba.Persistence` foundations, with exactly **one** legal foreign edge:
`Tooba.Wallet.Infrastructure -> Tooba.Notification.Contracts` (the canonical
`INotificationCreationPort`). Zero foreign module `Application`/`Infrastructure`/`Domain`/`Endpoints`
reference, zero foreign DbContext, zero cross-module persistence reach-through, zero cross-module
EF/SQL join, zero `TypeForwardedTo`, zero namespace alias. `Endpoints` never reaches
`Infrastructure`/`Host`; `Domain` depends only on `BuildingBlocks` + its own `Contracts`.
`microserviceExtractable = true`.

## 8. Structure (defense in depth; authority stays W2)

`PROFESSIONAL_SHALLOW` capability-first folders (`Customer/`, `Admin/`, `Payments/`, `Refunds/`,
`Composition/`, `Ports/`, `Models/`, `Validation/`); `Path-Namespace-State = EXACT` for every
production file; `Root-Allowlist-State = ENFORCED` (Contracts/Domain/Application empty; Endpoints
`WalletEndpointModule.cs`; Infrastructure empty with `WalletModule.cs` under `DependencyInjection/`);
`Physical-Copy-State = CLEAN`; `Solution-Explorer-State = CANONICAL` (`/Modules/Wallet/` with exactly
six projects); `File-Cohesion-State = COHESIVE`; `God-File-State = NONE` (the former 682-LOC
`WalletDirectory.cs` is five capability partials under `Persistence/`, each < 300 LOC).

## 9. Schema / migrations preserved

The `wallet` schema, `wallet_accounts`, `wallet_ledger_entries`, `gift_cards` and
`gift_card_redemptions` tables and the single migration `20260827180000_InitialWallet` (+ designer +
snapshot) are byte-identical to the W0 baseline. The outbox `Translate`/`ResolveEventClrType` still
return `null` (no external event this version). No migration, designer, snapshot or DbContext file was
touched by any wave.

## 10. Host final closure preserved

Zero Host production file added, moved or widened by W3. `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` /
`HOST_ROOT_FINAL_CERTIFIED` are untouched; `lastAcceptedTask` stays
`TB-TMAR-HOST-ROOT-FINAL-CERT-001` and `currentHostCheckpoint` stays `HOST_ROOT_FINAL_CERTIFIED`.

## 11. Manifest certification

Wallet was **promoted** (moved, not copied) from `uncertifiedHttpOwningModules` into the certified
`modules[]` array:

```text
structureCertified       : true
lockVersion              : ARCH-COMPLETE-002
structureAuthorityTask   : TB-TMAR-WALLET-AMSC-001-W2
structureAuthorityCommit : 779cd2f4f08cbc5bd90741384d8cef7d4a049e96
structureHandoffState    : READY_FOR_CERTIFY_CONSUMED_BY_W3
certificationState       : WALLET_AMSC_001_CERTIFIED
currentCertificationTask : TB-TMAR-WALLET-AMSC-001-W3
currentVerdict           : COMPLETE_REFERENCE_PATTERN
certificationNote        : AMSC-001 W0→W3 lineage
```

`preCertModules` is now **empty** and `uncertifiedHttpOwningModules` is now **empty**;
`structureLock.certifiedModules` is **32** with `Wallet` present exactly once. The W2 structure guard
was repointed to the promoted truth (it previously asserted the pre-cert `preCertModules[Wallet]`
shape) — assertions relocated, **none weakened**.

## 12. Guards strengthened, never weakened

- New `WalletModuleAmsc001W3CertGuardTests` (**12 facts**) locks the manifest/SoT certification and wave
  lineage, the HTTP_OWNING eleven-route ownership with a zero Host route count, the §6a source-derived
  route→request provenance set equality (plus the count-preserving mutation and fail-closed proofs), the
  validator matrix and non-circular validator discovery, the canonical API-result/typed-fault/catalog
  mechanisms, the bilingual locked resources and composed-catalog uniqueness, the transport validation
  codes, the Contracts-only microservice boundary, the unchanged schema/migration set, and the
  preserved Host closure + evidence tree + Master Recovery checkpoint.
- W1 guard (**9/9**) and W2 guard (**7/7**) re-run green — W1/W2 semantics preserved, not weakened.
- Cert guards `TmarCompleteReferenceStructureGateTests`, `PricingModuleAmsc001W3R3CertGuardTests`,
  `TaxModuleAmsc001W3CertGuardTests` and `UserPreferenceModuleAmsc001W3CertGuardTests` updated for the
  32-module `structureLock.certifiedModules` and the cleared `uncertifiedHttpOwningModules` list.
- No assertion was deleted, relaxed or repointed away from a real invariant; no baseline widened.

## 13. Verification

```text
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj          -> SUCCEEDED, 0 errors
dotnet test --filter WalletModuleAmsc001W3CertGuardTests                       -> 13 passed / 0 failed
dotnet test --filter Wallet* | ErrorCatalogUniqueCodeGuardTests
             | HostAdminAccessAmcCertGuardTests | HostAdminCanon003* | HostAdminCanon009*
             | PaymentPrecertHygieneTests | WalletCurrencyContractsTests
             | TmarCompleteReferenceStructureGateTests | TmarDurableGuardTests
             -> 91 passed / 1 skipped / 3 failed (after W3)
dotnet test --filter FullyQualifiedName~Architecture -> 1384 passed / 42 failed (after W3)
```

**Baseline proof (`NEW = 0`).** The same filters were run in an isolated `git worktree` at the W2 head
`779cd2f4f08cbc5bd90741384d8cef7d4a049e96`:

| Filter | W2 head `779cd2f4` | After W3 | Δ |
| --- | --- | --- | --- |
| Focused Wallet/cert set | 78 passed / 1 skipped / **3 failed** | 91 passed / 1 skipped / **3 failed** | +13 passed, failures unchanged |
| `~Architecture` | 1371 passed / **42 failed** | 1384 passed / **42 failed** | +13 passed, failures unchanged |

Set difference of the failing test **ids** is **empty in both directions** (`NEW_FAILURES = 0`,
`FIXED = 0`). The 42 repository-global failures are pre-existing and identical at both heads:

- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  — the documented out-of-scope `Tooba.Catalog.Contracts/Cart` + `Tooba.Cart.Contracts/{Checkout,Presentation}`
  project-level namespace deviation.
- `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` and
  `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
  — repository-global recovery pins unrelated to Wallet.
- The remaining 39 are the Host/Admin `StoreAppearance`/PW count-drift guards from earlier Catalog
  evacuation waves, unchanged by Wallet.

**Zero new failures, zero regressions, zero Wallet test failures.**

```text
Applicability               : HTTP_OWNING
Endpoint-Ownership-State    : MODULE_OWNED (11 routes / 3 groups, Host 0)
CQRS-State                  : COMPLIANT_11_OF_11
Validator-Matrix-State      : EXHAUSTIVE_4_OF_4
Route-Request-Set-Equality  : EXACT_SOURCE_DERIVED_11_OF_11_ENFORCED
Stable-Error-Code-State     : SINGLE_CANONICAL_CONTRACTS_HOME_50_DECLARED_10_HTTP_40_INVARIANT
Declared-Code-Guard-State   : PRESENT
Typed-Fault-Seam-State      : WALLETOPERATION_TYPED_CODE_ONLY
Raw-Prose-Fault-State       : ZERO
Localization-State          : CANONICAL_50_EN_50_FA_EXPLICIT_LOCKED_LOGICAL_NAMES
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

`USER_REVIEW_WALLET_AMSC_001_W3`; `automaticNextImplementationTask = NONE`; own cert commit SHA
reported through the Bridge Result channel only.
