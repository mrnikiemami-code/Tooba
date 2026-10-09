# TB-TMAR-SUPPORT-AMSC-001 — Wave 1 (Migrate)

- **Skill:** `tooba-architecture-migrate` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Support/Tooba.Support.*`
- **Starting HEAD:** `567ac4004eb464f489695f078e794fe7645716ab` (W0 analyze commit, `HEAD == origin/main`)
- **W0 verdict consumed:** `READY_TO_MIGRATE` (`docs/architecture/evidence/TB-TMAR-SUPPORT-AMSC-001-W0/analyze.md`)
- **Branch:** `main`
- **Host touched in this wave:** NONE (composition/DI already registers the Support Application assembly)

Behavior preservation: routes, HTTP verbs, status codes, response bodies, DTO semantics, business
rules, state transitions, ordering, idempotency, transactions, persistence, schema, outbox event
names, telemetry names and commerce scoping are unchanged. Only fault-mapping mechanism,
localization coverage, stable-code ownership and validator coverage change.

Final objective: Support must be extractable as an independent microservice — zero cross-module
Application/Infrastructure/Domain coupling, Contracts-only boundaries.

---

## 1. Repair 1 — `Tooba.Support.Contracts` boundary project (blocker B1)

Support was the only HTTP-owning module in the repository with **no `Contracts` project**
(`tmar-module-structure-manifests.json` → `uncertifiedHttpOwningModules: ["Support","Wallet"]`).

New project `Tooba.Support.Contracts` (`net8.0`, no module references) owns the module's stable
cross-boundary error identity:

```
Tooba.Support.Contracts/
  Errors/SupportErrorCodes.cs      ← single canonical home of the support.* code identity
```

- `Tooba.Support.Contracts` added to `Tooba.slnx` under the existing `/Modules/Support/` folder
  (now six project entries; solution-explorer grouping unchanged in shape).
- Consumed by `Domain`, `Application`, `Infrastructure`, `Endpoints` and `Tests` through a
  `ProjectReference` — mirroring the certified `Returns`/`Story`/`Promotion`/`Settlement` layout.
- `Domain` now references only `BuildingBlocks` + `Support.Contracts` (no Application/Infrastructure).

## 2. Repair 2 — Canonical typed-fault seam (blocker B5)

`Application/Errors/SupportExceptionMapper.cs` classified faults by **exact message-text equality**
against a hardcoded 20-entry set, and the Domain/Infrastructure raised the framework
`InvalidOperationException` with a machine code in `Message`.

Retired and replaced by the canonical seam used by every certified module:

| Before | After |
| --- | --- |
| `throw new InvalidOperationException("support.ticket_not_found")` | `throw new ContractOperationException(SupportErrorCodes.TicketNotFound)` |
| `SupportExceptionMapper.TryMapExact(ex.Message, code, out _)` | `catch (ContractOperationException ex) when (SupportErrorCodes.IsKnown(ex.Code))` |
| `Application/Errors/SupportExceptionMapper.cs` | `Application/Composition/SupportOperation.cs` |

- `Application/Composition/SupportOperation.cs` — `ExecuteAsync<T>(Func<Task<T>>, string? publicOutcomeCode = null)`,
  `ExecuteAsync(Func<Task>, string? publicOutcomeCode = null)`, `NotFoundIfNull<T>`, and
  `ToSemanticError(ContractOperationException, string?)` which rethrows unknown codes.
- `Application/Errors/` folder is **removed entirely**; the retired mapper stays retired.
- Classification is by **typed code only**. A contract fault whose code is not a declared Support
  code, and every unexpected exception, propagates untouched to the canonical global exception
  boundary — a genuine defect is never silently converted into a business failure.
- **Behavior preservation (response contract):** the pre-existing mapper mapped every known
  directory/domain fault onto the stable **public outcome code of that use case**
  (`support.rejected` / `support.reply.rejected` / `support.action.rejected` /
  `support.patch.rejected` / `support.missing`). `SupportOperation` preserves exactly that: a
  domain invariant is a `Result`-shaped identity, **not** a new HTTP code, so it is mapped onto the
  stable public outcome code the handler declares, while a client-reachable code
  (`IsHttpReachable`, e.g. `support.missing`, `support.demo.not_ready`) is reflected verbatim.
  This is the declared `publicOutcomeCode` parameter — no client-observable code changed.

Call sites repaired to the typed primitive:

| File | Faults converted |
| --- | --- |
| `Domain/Aggregates/SupportTicket.cs` | `TicketIdRequired`, `RequesterRequired`, `SellerPartyRequired`, `SubjectInvalid`, `CloseNotAllowed`, `ReopenNotAllowed`, `RelatedIdWithoutType`, `RelatedTypeInvalid`, `RelatedIdRequired`, `IdempotencyKeyInvalid` |
| `Domain/Entities/TicketMessage.cs` | `MessageIdsRequired`, `MessageBodyInvalid`, `MessageInternalAdminOnly`, `IdempotencyKeyInvalid` |
| `Infrastructure/Directories/SupportDirectory.cs` | `TicketNotFound`, `ReplyClosed`, `RelatedOrderIdInvalid` |
| `Infrastructure/Messaging/SupportOutboxRegistration.cs` | `OutboxEmitNotSupported` |
| `Application/Composition/SupportEnumParsing.cs` | `CategoryInvalid`, `PriorityInvalid`, `StatusInvalid`, `RequesterKindInvalid` |

Zero `InvalidOperationException("support.*")`, zero `.Message.Contains(` / `.Message.StartsWith(` /
`.Message ==` / `TryMapExact` / `SupportExceptionMapper` remain in Support production (durable guard).

## 3. Repair 3 — Stable-code catalog with a declared reachability split (blocker B3)

`Tooba.Support.Contracts.Errors.SupportErrorCodes` is the single canonical home:

- `HttpReachable` (7) — codes an HTTP client can observe; each has exactly one descriptor
  registered by `SupportErrorCatalogContributor`: `support.missing`, `support.rejected`,
  `support.reply.rejected`, `support.action.rejected`, `support.patch.rejected`,
  `support.authorization.unavailable`, `support.demo.not_ready`.
- `DomainInvariants` (21) — codes the Domain aggregate and Infrastructure directory raise as typed
  faults. They are localized but deliberately get **no** dedicated HTTP descriptor: the use case
  maps them onto the stable public outcome code, so no client-visible code is added.
- `IsKnown` / `IsHttpReachable` / `IsDomainInvariant` are the declared-set guards; `IsKnown` is the
  union `SupportOperation` filters on.
- No code string changed. `customer.session.required`, `seller.authorization.denied` and
  `admin.authorization.denied` remain Foundation-owned and are deliberately **not** declared here
  (single descriptor owner); the module consumes them without re-registration.

## 4. Repair 4 — Localization coverage (blocker B2)

`Resources/SupportErrors.resx` localized exactly **one** key while six client-observable codes
resolved to descriptors with no entry.

- Both bilingual pairs are now complete: **28 keys** in `SupportErrors.resx` **and**
  `SupportErrors.fa.resx` — one entry per declared code (7 HTTP-reachable + 21 domain invariants).- `SupportErrorResourceSet` remains the module-owned `support.` keyspace set, registered exactly
  once by `SupportEndpointModule.AddSupportEndpointPresentation`; the resource files stay colocated
  with it in `Tooba.Support.Endpoints/Resources/` so the single registration is untouched.
- `support.action.rejected` is no longer a dead registration: it is genuinely emitted by the
  close/reopen handlers through `SupportOperation`, preserving the FE `support-api.ts` fallback
  contract.
- Machine-stable codes only; the module never emits Persian/English prose.

## 5. Repair 5 — Transport validator coverage (blocker B4, HARD BLOCKER)

- New `Application/Validation/SupportValidationCodes.cs` (14 stable `support.validation.*` machine
  codes) and `Application/Validation/SupportRequestValidators.cs` with **9** FluentValidation
  validators covering exactly the 9 `VALIDATOR_REQUIRED` requests of the W0 17-row matrix:

| Request | Validator | Transport-shape rules |
| --- | --- | --- |
| `CreateCustomerTicketCommand` | `CreateCustomerTicketCommandValidator` | `Subject` required/≤200; `Category` required + known; `Priority` known-or-empty; `Body` required/≤4000; `RelatedEntityType` ≤64; related id/type pairing; `IdempotencyKey` ≤128 |
| `CreateSellerTicketCommand` | `CreateSellerTicketCommandValidator` | same body shape + `IdempotencyKey` ≤128 |
| `ReplyCustomerTicketCommand` | `ReplyCustomerTicketCommandValidator` | `Body` required/≤4000; `IdempotencyKey` ≤128 |
| `ReplySellerTicketCommand` | `ReplySellerTicketCommandValidator` | same as customer reply |
| `ReplyAdminTicketCommand` | `ReplyAdminTicketCommandValidator` | same as customer reply |
| `PatchAdminTicketCommand` | `PatchAdminTicketCommandValidator` | `Status` known-or-empty; `Priority` known-or-empty |
| `ListCustomerTicketsQuery` | `ListCustomerTicketsQueryValidator` | `Status` known-or-empty |
| `ListSellerTicketsQuery` | `ListSellerTicketsQueryValidator` | `Status` known-or-empty |
| `ListAdminTicketsQuery` | `ListAdminTicketsQueryValidator` | `Status`/`RequesterKind`/`Category`/`Priority` known-or-empty; `Q` ≤200 |

- The remaining 8 requests stay `NO_VALIDATOR_REQUIRED` with durable provenance (route `:guid`
  constraint and/or server-derived actor/seller party): `GetCustomerTicketQuery`,
  `GetSellerTicketQuery`, `GetAdminTicketQuery`, `CloseCustomerTicketCommand`,
  `CloseSellerTicketCommand`, `ReopenCustomerTicketCommand`, `ReopenSellerTicketCommand`,
  `GetSupportDemoPreviewQuery`.
- Set equality: **17 shipped routes = 17 reachable requests = 9 + 8 classified exactly once**.
- Validators emit machine codes only (`WithMessage(` never appears) and own transport shape only —
  no ownership/state/idempotency rule is duplicated from the Domain.
- Discovery: `AddToobaCqrsFoundation` runs `AddValidatorsFromAssembly` over the Support Application
  assembly already registered in `Host/Program.cs:178` — **no Host change required**.

## 6. Boundary / coupling state

- `Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY`: the only foreign edge remains
  `Tooba.Support.Infrastructure → Tooba.Notification.Contracts`.
- Zero foreign `.Application` / `.Infrastructure` / `.Domain` reference; zero foreign
  `DbContext`/`DbSet`; zero cross-module EF/SQL join.
- `Endpoints` references only `Support.Application` (+ `Support.Contracts` and BuildingBlocks) —
  never `Support.Infrastructure` or Host.
- Host residue unchanged: `ALLOWED_SECURITY_ADAPTER` (2 authorizers) + `ALLOWED_COMPOSITION_ROOT`.

## 7. Persistence / schema state

`UNCHANGED`. The migration id `20260827120000_InitialSupport`, its Up/Down, the model snapshot,
schema `support`, `SupportDbContext` and the outbox registration are untouched. No migration was
regenerated for the structural cleanup.

## 8. Focused validation

| Command | Result |
| --- | --- |
| `dotnet build Tooba.Support.Tests` | **0 errors** |
| `dotnet test Tooba.Support.Tests` | **13 passed / 0 failed** |
| `dotnet build Tooba.Host.Tests` (builds Host + all module chains) | **0 errors** |
| `dotnet test Tooba.Host.Tests --filter SupportModuleAmsc001W1MigrateGuardTests` | **9 passed / 0 failed** |
| `dotnet test Tooba.Host.Tests --filter "Support\|ErrorCatalogUniqueCode\|HostAdminCanon003\|HostAdminAccessAmcCert\|HostModuleEndpointOwnership"` | **all Support/ErrorCatalog/Host-admin facts pass** |

New durable guard: `src/backend/Host/Tooba.Host.Tests/Architecture/SupportModuleAmsc001W1MigrateGuardTests.cs`
(9 facts) locks the single canonical Contracts stable-code home with its declared reachability split
(7 + 21 = 28), the typed seam (no message parsing, retired mapper + retired `Application/Errors`
folder), the bilingual resource coverage and contributor single-registration, the 9-validator
transport matrix with machine-code-only messages, the Foundation session code, the Contracts-only
boundary, the Host `SupportDbContext` allowlist, and the unchanged migration set. Its assertions are
**located by file name, not by technical-axis path**, so the W2 structure wave can folder the module
without weakening this guard.

### Pre-existing unrelated red (disclosed, not repaired here)

`TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` and
`…Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` are red at the W1
starting HEAD as well. They compare a frozen 16-entry `structureLock.certifiedModules` literal against
the live repository-global SoT list (BulkInquiry/Catalog drift) and a repository-global recovery
checkpoint; **Support appears in neither expectation**, so neither failure is caused by this wave.
The same two reds are already recorded as pre-existing by the AccessControl, AddressBook,
CustomerProfile, Media, Notification, Pricing, Promotion and Story AMSC evidence trees. Repairing the
repository-global recovery checkpoint is outside module-local scope.

## 9. Wave disposition

`READY_FOR_STRUCTURE` — ownership correct, behavior preserved, canonical mechanisms in place,
boundaries Contracts-only.
`Structure-Handoff-State = REQUIRED` (W2 owns the final physical/solution gate).
