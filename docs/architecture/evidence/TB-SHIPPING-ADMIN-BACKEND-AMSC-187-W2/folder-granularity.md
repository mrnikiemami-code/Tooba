# Folder granularity

Classification: **`PROFESSIONAL_SHALLOW`** (after the W2 extraction; was `ROOT_DUMP`-by-absence at
the Endpoints root — the canonical composition file was missing from the root while the composition
type lived in `Admin/`).

Rule used throughout: count **production source files**, not declared types. Folders examined:
`Contracts` (7 files), `Domain` (10), `Application` (16), `Infrastructure` (17 non-migration + 13
migration/designer/snapshot), `Endpoints` (9 + the new root entry), `Tests` (34, out of structure scope).

## Application — capability-first, technical axes secondary

```text
Application/
  Operations/  Queries/ShippingOperationalQuoteQueries.cs
  Packaging/   Commands/ShippingPackagingPolicyCommands.cs
               Ports/IShippingPackagingPolicyDirectory.cs
               Validation/ShippingPackagingValidators.cs
  Rates/       Commands/ShippingRateCoverageCommands.cs
               Ports/{IShippingDestinationScopeValidator,IShippingMethodCoverageDirectory,
                      IShippingMethodOptionValidator,IShippingRateCoverageDirectory}.cs
               Queries/GetShippingMethodFinancialStatusQuery.cs
               ReadModels/ShippingMethodFinancialStatusEvaluator.cs
  Scheduling/  Commands/ShippingScheduleCommands.cs
               Ports/IShippingOperatingScheduleDirectory.cs
  Validation/  ShippingAdminValidators.cs, ShippingOperationalValidators.cs, ShippingScheduleValidators.cs
```

* No `Application/Commands|Queries|Validators` root (technical-axis-first is absent).
* No per-use-case leaf folder: every request/handler/validator file sits **directly** on a
  capability → secondary-axis folder. `Operations/Queries/` holds 1 file but the folder is a
  capability → axis pair, not a use-case-named leaf (`Queries` ≠ `ShippingOperationalQuoteQueries`).
* The cross-capability `Validation/` seam is the repository-established shared-validator pattern
  (Wishlist `Validation/`, Payment `Validators/`); it holds 17 of the 22 validators and does not
  fragment a capability.

## Endpoints — audience folder + canonical root composition

```text
Endpoints/
  ShippingEndpointModule.cs                       (NEW — root composition entry)
  Admin/   ShippingAdminAuthorizer.cs, 5 × *Endpoints.cs
  Errors/  ShippingErrorCatalogContributor.cs
  Resources/ ShippingErrorResources.cs, ShippingErrors.resx, ShippingErrors.fa.resx
```

`Errors/` and `Resources/` are legitimate special-purpose folders (identical to the certified
Payment.Endpoints layout: root composition + `Errors/` + audience folders), so they are not treated
as capability route folders and are **not** forbidden by the guard.

## Infrastructure / Domain / Contracts

* `Infrastructure`: `DependencyInjection/ShippingModule.cs` (module composition root, the Payment
  precedent), capability adapters in `Operations/Packaging/Rates/Scheduling`, EF model in
  `Persistence/` and migrations in `Persistence/Migrations/`. No root `.cs`.
* `Domain`: `Aggregates/` (4) + capability folders `Operations/Packaging/Rates/Scheduling` for the
  pure evaluators. No root god-file.
* `Contracts`: `Errors/` (single stable-code home) + `Admin/Operations/Packaging/Rates/Scheduling`.

## Rejected corrections (deliberately not done)

| Candidate | Why not |
| --- | --- |
| Split `Rates/ReadModels/ShippingMethodFinancialStatusEvaluator.cs` (single file) | Not a use-case leaf; a capability-owned read model. Splitting would be cosmetic. |
| Merge `Application/Validation/*` into capability folders | Would be a semantic re-organization, not a physical necessity; the shared seam is the repo pattern. |
| Flatten `Operations/Queries/` | Would create an Application-root dump. |
| Deduplicate the duplicated body readers (`ReadBodyAsync` in Admin endpoint files) | Explicitly out of scope: semantic dedup requires a separately authorized Migrate repair. |
