# Error descriptor model — TB-TMAR-FND-OBSERR-001-R3

## Model

`ErrorDescriptor(Code, Classification, HttpStatus, LocalizationKey, Severity, SafeTitleFallback)`

## Contracts

- `IErrorCatalogContributor` — module/foundation contributes descriptors
- `IErrorDefinitionCatalog` / `ErrorDefinitionCatalog` — immutable compose at startup; duplicate code ⇒ `InvalidOperationException`

## SafeErrorMapper

- Injects catalog; **no** `ClassifySemanticCode`
- Unknown semantic code ⇒ Business / 400 safe fallback (never guessed from name substrings)
- Foundation codes: `validation.failed`, `platform.unexpected`, `platform.error`
- Offer codes registered via `OfferErrorCatalogContributor` (not in BuildingBlocks)

## Evidence of removal

`SafeErrorMapper.cs` contains no `.not_found` / `cannot_activate` / `.denied` substring heuristics.
