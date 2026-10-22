# RESULT — TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2

Status: PASS (only the two authorized Domain boundary repairs)
Parent: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_DOMAIN_SORT_ZERO_AND_ATOMIC_INVARIANT_REPAIR
Base: d937e32a7d6162fc186b71292efd0a1456272b7a (verified `git merge --ff-only origin/main`)
Worker: tooba-worker-01 / cursor

## Baseline failure captured before the fix

Added the sort-zero regression first and ran it against the unmodified base:

```
dotnet test ...Fulfillment.Tests.csproj --filter "FullyQualifiedName~Inclusive_sort_boundaries"
-> Failed: 1, Passed: 2  (Inclusive_sort_boundaries_are_accepted_by_create_and_update(sortOrder: 0))
```

Cause confirmed: `ShippingType.Create` called
`ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sortOrder > SortOrderMaxValue ? -1 : sortOrder)`,
which rejects the declared valid minimum `0` (and would have reported `-1` for both a negative value and
an over-maximum value).

## R196-R1-01 — inclusive sort boundary

`ShippingType.Create` now calls the same local `EnsureSortOrder` used by `Update`
(`0..1_000_000` inclusive). Minimum/maximum constants, the database CHECK and the Application contract are
unchanged. The hack that collapsed all invalid values into `-1` is gone, so `-1` and `1_000_001` are still
rejected while `0`, `1` and `1_000_000` are accepted for both `Create` and `Update`. A rejected `Update`
mutates nothing: `SortOrder`, `IsActive`, `Revision` and `UpdatedAt` are asserted value-identical.

## R196-R1-02 — atomic local Domain invariants

Public translation factory/mutator again enforce local invariants, without any `Domain -> Contracts`
reference (the existing Fulfillment boundary guard is preserved; `Tooba.Fulfillment.Domain.csproj` is
byte-identical):

- `ShippingTypeTranslation.Create` requires a non-empty server row id and non-empty `LanguageId`, a
  non-null/non-blank name of at most 128 characters, and an optional description of at most 512 whose only
  "empty" form is `null` (a whitespace description is rejected rather than silently treated as a clear).
- `Replace` validates the **complete proposed pair** (`EnsureValidText`) before assigning either field, then
  assigns both. A bad description therefore cannot leave the same tracked object with a new name.
- `AddTranslation` builds and validates the candidate first, then rejects a duplicate `LanguageId` **or** a
  duplicate translation row id, and only then touches the collection. Invalid input never modifies the
  collection, so no partial tracked edit is left for a later `SaveChanges`.
- `ShippingType.Create` validates every supplied entry (null, duplicate language, duplicate row id, local
  text invariants) through `ValidateSupplied` **before** the aggregate is constructed; the exposed aggregate
  cannot be born invalid.

Boundary kept: Domain throws local `ArgumentException`/`ArgumentOutOfRangeException` invariants only. No
message text is parsed or mapped into a business response; canonical user-input codes remain in
Application/Contracts/Endpoints. No duplicate business rule engine was introduced — Application
`ShippingTypePreparation.NormalizeText` remains the single canonical user-input policy and its trimming/
`null`-description normalization is preserved (Domain accepts the prepared values as-is and does not trim).

Preserved: code canonical rejection including surrounding whitespace, code immutability, inactive revision-1
creation, and the Application no-op/preflight behaviour (unchanged source, unchanged tests).

## Executed proof

```
dotnet test ...Fulfillment.Tests.csproj --filter "FullyQualifiedName~ShippingTypeCatalogRulesTests"
  -> Passed 54 / Failed 0

dotnet test ...Fulfillment.Tests.csproj \
  --filter "FullyQualifiedName~Fulfillment_golden_boundaries_and_physical_layout_remain_clean"
  -> Passed 1 / Failed 0   (Domain -> Contracts still absent)
```

New/updated Domain boundary tests (table-based, not one assertion per line):
- inclusive `0 / 1 / 1_000_000` accepted by `Create` and `Update`; `-1` and `1_000_001` rejected;
- rejected `Update` leaves `SortOrder`, `IsActive`, `Revision`, `UpdatedAt` identical;
- Unicode name at 128 and description at 512 accepted; name at 1 with `null` description accepted;
- empty row id, empty `LanguageId`, blank/null name, 129-character name, 513-character description rejected;
- bad description with a valid new name leaves the **same** translation object and both original values;
- invalid (blank text, empty language, empty row id) and duplicate add leaves the **same** aggregate
  collection unchanged;
- `Create` rejects null entry, duplicate language entry, and a supplied entry whose stored name is blank.

One controlled EF InMemory store regression (`Store_creates_sort_zero_inactive_revision_one_and_reads_it_back`)
creates `sortOrder = 0` with a lawful default-language fixture, asserts inactive/revision 1, reads it back via
`GetAsync` and `ListAsync`, then activates it at sort 0. Explicitly **not** HTTP/API/PostgreSQL proof.

## Not executed (unchanged gates)

Four-route request/handler/validator set equality, production-delegate Admin auth/body proof, real
adapter-backed dynamic-language matrix, real `ActivityListener` assertions, lookalike-constraint negative, and
historical migration chain versus regenerated snapshot plus real PostgreSQL constraint/unique/revision races
all remain PENDING. `TB196-Acceptance` is therefore
`PARTIAL_PENDING_API_TRACE_AND_RELATIONAL_PROOF`, not relational-only.

## Scope compliance

Production change limited to `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingType.cs`,
plus the focused test file. No Application workflow change, no Endpoint/transport/auth/tracing/DI/schema/
EF-mapping/migration/designer/snapshot/Contracts/resource/manifest/Host/frontend/Shipping/Storefront edit, no
namespace or folder move, no DB/container/server/browser/listener/seed/package action, no Host build, no
reset/stash/rebase/force-push. Unrelated dirty/untracked files preserved.

## Deadline

Claim 2026-10-22T10:40:18Z, result 2026-10-22T10:56Z, elapsed ~16 minutes — inside the 20-minute hard cap with
no new coding after minute 15.
