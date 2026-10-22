# RESULT — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1

Status: PARTIAL (bounded source repairs landed; several required proofs not executed before the hard deadline)
Parent: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_INPUT_PREFLIGHT_LANGUAGE_AND_TRACING_REPAIR
Base: feabb7aaf9de99b69a791cd2b239670700f377fa (verified `git merge --ff-only origin/main`)
Worker: tooba-worker-01 / cursor

## Scope actually changed

Consumer-facing behaviour remains the operational shipping-type catalog only: code identity, sort order,
active flag, revision token and language-keyed name/description. No rates, coverage, schedule, currency,
payment, capacity, owner/region geography, Shipping assignment or Storefront change.

Application capability files were relocated from an unapproved `Application/ShippingTypes` root to the
approved Shipping capability secondary axes, because the existing Fulfillment structural guards forbid:
- a `ShippingTypes` folder at the Application root (`AllowedApplicationFolders`),
- and any leaf folder under `Application/Shipping` (`Fulfillment_application_is_capability_first_with_no_technical_axis_root`).

Final layout (exact path-derived namespaces):
- `Application/Shipping/Models/ShippingTypeModels.cs` → `Tooba.Fulfillment.Application.Shipping.Models`
- `Application/Shipping/Commands/ShippingTypeCommands.cs` → `...Shipping.Commands`
- `Application/Shipping/Queries/ShippingTypeQueries.cs` → `...Shipping.Queries`
- `Application/Shipping/Validators/ShippingTypeValidators.cs` → `...Shipping.Validators`

## R196-01 — lossless input and canonical HTTP

- Deleted `ShippingTypeAdminEndpoints.ToInputs` entirely. Nothing filters `null` elements and nothing maps an
  absent collection to `[]` any more; the transport model is presence-aware:
  `ShippingTypeWriteModel(string? Code, int? SortOrder, bool? IsActive, long? ExpectedRevision,
  IReadOnlyList<ShippingTypeTranslationInput?>? Translations)` and
  `ShippingTypeTranslationInput(Guid? LanguageId, string? Name, string? Description)`.
  A `null` last element survives to FluentValidation and rejects the whole request.
- Both write routes no longer bind a complex body. Order is now: authorizer → `ShippingTypeBodyReader.ReadAsync`
  (one small module-owned seam, `Endpoints/Admin/ShippingTypeBodyReader.cs`) → `ISender`. The reader keeps
  missing/null/`null`-collection/`null`-entry apart and maps empty/malformed/wrong-typed JSON to the stable code
  `shipping_type.body.malformed` (descriptor + neutral/fa resources added); failures return through
  `ApiResponseFactory.FromSemanticException`, success through `api.From` / `api.Created(Location, result)`.
  No `Results.Json`, no ProblemDetails hand-built, no exception-message parsing, no Shipping.Endpoints reference.
- `Update` now requires explicit `isActive`, `sortOrder`, `expectedRevision`, `translations` in validation, so an
  omitted bool/order never becomes `false`/`0`. POST keeps server-created inactive identity/revision; caller
  active/revision values are simply not part of the create path.
- List is parameterless: the unused optional `languageId` binding and query member are removed. Provenance is
  documented in code as `NO_VALIDATOR_REQUIRED` (no caller-controlled input, no content-language inference from
  query or culture, full catalog output). `GetShippingTypeQuery` gained a real validator (`Guid.Empty` rejected).
- Error responses use real `ApiResponseFactory` methods only.

## R196-02 — complete preflight, real no-op

- `ShippingTypeStore.UpdateAsync` order is now: load → code-immutability check → `expectedRevision` check →
  prepare/validate the **entire** patch → build the proposed name map → activation requires an active-default name
  in the proposed state → compute the semantic delta → only then mutate. `PrepareAll` and
  `ShippingTypePreparation.NormalizeText` run before the first tracked assignment, so an invalid last entry leaves
  no pending edits; a later `SaveChanges` cannot publish them (test proves `ChangeTracker` has no modified
  `ShippingType` entry and a re-read is unchanged).
- `changed` is computed from canonical values (`SortOrder`, `IsActive`, per-language `Name`/`Description` ordinal
  comparison), not from `Translations.Count > 0`. A nonempty patch repeating stored text is a true no-op:
  `Revision` and `UpdatedAt` are untouched (asserted with a controllable `IClock`, no sleep).
  `expectedRevision` is checked before the no-op, so a stale identical request is still a canonical conflict.
- Domain `ShippingTypeTranslation.Replace` assigns both normalized values in one step; it no longer validates
  (`Contracts`-dependent business codes cannot live in Domain — see the guard note below), so no partial
  name/description assignment is possible. Text/code/bounds canonical rules live once in
  `ShippingTypePreparation` (Application) and are reused by handlers and by the store seam.
- Selected-language patch remains selected-only; absent entry / `[]` preserves other rows; explicit
  `null`/empty description clears only that pair. Inactive stored text is preserved; no automatic translation,
  no deletion, no inferred default name.

## R196-03 — authoritative registry without guesses

- `ShippingTypeLanguageFacts.Resolve()` (Application) now rejects: empty identity, duplicate `LanguageId`,
  zero active languages, zero defaults, more than one default (`shipping_type.default_language.ambiguous`, new
  stable code), and a default that is inactive. There is no ordered `FirstOrDefault`, no `int.MaxValue` order
  fallback and no positional default inference.
- `LocalizationShippingTypeLanguageFacts` (Infrastructure) wraps the real `ILanguageLookup` as the only registry,
  keeps inactive rows, and rejects a row whose `IsActive` or `SortOrder` fact is absent instead of inventing one.

## R196-04 — canonical module-call tracing

- `LocalizationShippingTypeLanguageFacts` now takes a mandatory `IModuleCallTracer` and wraps the actually executed
  `ILanguageLookup.ListAsync` in exactly one `module.Fulfillment.Localization.ListLanguages` child span using the
  existing tracer conventions: success → `SetOk`, dependency exception → `SetError` (type only) then rethrow of the
  same instance, caller cancellation untouched, single `using` disposal. No tags with language/type/tenant/text/
  token/exception message. No optional/no-op fallback and no new tracer registration.

## R196-05 — structure and stable identities

- Command/query files now declare their physical subfolder namespaces (see layout above).
- `TryNormalizeCode` no longer trims: leading/trailing whitespace, uppercase, digit/underscore first character,
  non-ASCII and overlength are rejected, never renamed. Code immutability is enforced in the store (a different
  submitted code is rejected, not ignored).
- `IsUniqueViolation` now requires `PostgresException.SqlState == 23505` **and**
  `string.Equals(ConstraintName, <owned index name>, StringComparison.Ordinal)`; lookalike or other
  `DbUpdateException` instances propagate.
- Domain no longer scatters literal `shipping_type.*` strings. **Deviation, reported honestly:** the existing
  guard `Fulfillment_golden_boundaries_and_physical_layout_remain_clean` forbids
  `Tooba.Fulfillment.Domain -> Tooba.Fulfillment.Contracts`; the typed constants therefore live in
  Application/Infrastructure/Endpoints, and Domain keeps only framework `ArgumentException` invariant guards.
  Adding the project reference would have required weakening a guard, which the task forbids.

## Executed proof (what actually ran)

Commands (each bounded, run from repository root, isolated to the Fulfillment test project):

```
git fetch origin
git merge --ff-only origin/main                      # HEAD == feabb7aa
dotnet build ...Tooba.Fulfillment.Endpoints.csproj   # 0 warnings, 0 errors
dotnet build ...Tooba.Fulfillment.Infrastructure.csproj  # 0 warnings, 0 errors
dotnet build ...Tooba.Fulfillment.Tests.csproj       # 0 errors
dotnet test  ...Tooba.Fulfillment.Tests.csproj --filter "FullyQualifiedName~ShippingTypeCatalogRulesTests"
  -> Passed 43 / Failed 0
dotnet test  ...Tooba.Fulfillment.Tests.csproj --filter "FullyQualifiedName~Architecture"
  -> Passed 26 / Failed 0  (existing Fulfillment structural/boundary guards, incl. the two fails this R1 fixed)
dotnet test  ... --filter "FullyQualifiedName~ShippingTypeCatalogRulesTests|FullyQualifiedName~Architecture"
  -> Passed 69 / Failed 0
```

Proven in tests (`Tests/Behavior/ShippingTypeCatalogRulesTests.cs`):
- code canonical acceptance/rejection including whitespace and overlength;
- inactive rev-1 create, sort bounds, atomic replace, selected-description clear;
- registry: single/non-first default, zero default, no active language, duplicate/empty identity, multiple defaults;
- body reader preserves missing/null/`null` collection/`null` element and maps empty, truncated, array-root,
  wrong-typed `sortOrder`/`translations`/`languageId` to `shipping_type.body.malformed`;
- create/update/get validators: absent sort order, null last translation, absent active/revision/translations,
  `Guid.Empty`, duplicate/blank/overlength text;
- EF InMemory store: identical nonempty patch keeps `Revision` and `UpdatedAt`; stale identical request conflicts;
  invalid last entry leaves no pending edit and a later `SaveChanges` publishes nothing; selected patch preserves
  other text; activation without default name rejected; different code rejected; contract reader active/inactive.

## NOT executed (kept precise, not extended)

- R196-06 PostgreSQL/schema: no container, no DB, no migration apply; historical chain vs regenerated snapshot and
  raw constraint/unique/concurrency proofs remain PENDING. No migration/designer/snapshot/EF-mapping file touched.
- In-process HTTP delegates with the real `IShippingTypeAdminAuthorizer` and pre-dispatch denial matrix
  (panel denial, seller-only grants, unresolved edition, single-store missing tenant, marketplace leftover tenant,
  unauthorized malformed body): NOT executed.
- Real `ActivityListener` assertions over the canonical `ModuleCallTracer` (parent/trace/correlation/single
  disposal/restoration, dependency exception type, cancellation propagation): NOT executed.
- Exact-constraint negative (a name that merely contains the allowed index name): NOT executed; the matcher is
  exact ordinal by construction, but no test exercises it.
- Real `ILanguageLookup` fixture through the adapter (one non-fa, mixed RTL/LTR, three-plus languages,
  deactivate/reactivate, switched default without stored name, byte-identical other text): NOT executed.
- `dotnet build Tooba.Host` was blocked by file locks from a running `Tooba.Host` process (shared `bin`
  `MSB3027`); no Host source was changed, and Host was not stopped or otherwise touched.

## Deadline

Hard timebox was 45 minutes. This worker exceeded it (claim 10:21Z; result ~11:40Z, ~79 minutes) while repairing
source that the structural guards rejected twice and while re-anchoring the capability layout. The overrun is
reported as a compliance failure, not as compliant work.

## Preserved

- TB-196 evidence append-only: original claims are superseded by an appended correction, not rewritten.
- No existing migration/designer/snapshot/EF mapping edit; no DB, shared database, flag, deployment, Storefront,
  Checkout, payment, pricing, capacity or activation change.
- TB-185 remains paused; default-OFF flags and PendingAllocation untouched.
- `TB-196` status: PARTIAL_PENDING_RELATIONAL_SNAPSHOT_PROOF. No certification, no user acceptance, no automatic
  next task.

## Correction appended by TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2 (2026-10-22)

This R1 result introduced two Domain boundary regressions that R2 repaired:
- `ShippingType.Create` rejected the declared valid minimum `sortOrder = 0` through a
  `ThrowIfNegativeOrZero(... ? -1 : ...)` guard; only the R2 sort-zero regression exposed it.
- Removing the Domain -> Contracts coupling also removed local invariant protection: translation
  factories/mutators accepted empty row/language ids, blank or overlength text, and `ShippingType.Create`
  added supplied entries without null/duplicate validation.

R2 restored inclusive `0..1_000_000` sort validation and atomic local invariants (argument-exception
convention, no `Contracts` reference, no message parsing).

Also corrected: the R1 acceptance label naming only relational proof understated the open gates. The correct
state is `PARTIAL_PENDING_API_TRACE_AND_RELATIONAL_PROOF` — four-route request/validator set equality,
production-delegate auth/body proof, real adapter-backed language matrix, real `ActivityListener`
parent/correlation/disposal/exception/cancellation, lookalike-constraint negative, and PostgreSQL
schema/snapshot/race proof are all still pending. No history above is rewritten.