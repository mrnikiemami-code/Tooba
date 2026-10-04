# TB-TMAR-CART-AMSC-001-W3 — Localization & error-catalog ownership

## Ownership rule verified

`duplicate usage is allowed; duplicate descriptor ownership is not`.

| Code group | Count | Owner | Evidence |
|---|---|---|---|
| `cart.*` module-owned codes | 26 | `CartErrorCatalogContributor` | 26 `D(...)` descriptors in `Tooba.Cart.Endpoints/Errors/CartErrorCatalogContributor.cs` |
| `checkout.authentication_required` | 1 | `FoundationErrorCatalogContributor` | Cart declares the constant and localizes it, but registers **no** descriptor (comment in the contributor records this) |

`CartErrorCodes` declares 27 constants, all machine-stable, no Persian prose, no `Contains`/`StartsWith`
classification. `CartModuleAmsc001W3CertGuardTests` asserts that no other
`*ErrorCatalogContributor.cs` in the backend registers any Cart-owned code.

## Composed-catalog uniqueness

`ErrorCatalogUniqueCodeGuardTests.Composed_production_contributors_register_each_machine_code_exactly_once`
composes every public `IErrorCatalogContributor` shipped by the `Tooba.*` production assemblies and
fails on any duplicate machine code. It passes with the Cart contributor included. `ErrorDefinitionCatalog`
fail-fast behavior is untouched; no `first wins` / `last wins` / `DistinctBy` suppression exists.

## Resource coverage

Both `CartErrors.resx` and `CartErrors.fa.resx` contain a `data name=` entry for **every one of the 27
declared codes** (26 module-owned + the shared session code), verified by
`CartModuleAmsc001W3CertGuardTests.Cart_error_codes_resolve_to_localized_resources_in_both_cultures`.
The canonical seams are wired in `CartEndpointModule.AddCartEndpointPresentation`:
`IErrorCatalogContributor → CartErrorCatalogContributor` and `IErrorResourceSet → CartErrorResourceSet`.
No hard-coded user-facing prose exists in Domain/Application/Infrastructure/Endpoints.

## Residual watch (non-blocking, pre-existing)

`CartErrorCodes.LineCurrencyMissing` (`cart.line.currency_missing`) is declared **and** localized in both
resx files but has **no** catalog descriptor and **no** throwing site. It is the fail-closed code for a
quoted line that carries no currency truth, introduced by the multi-currency line work
(`f0cf9afb`), i.e. it predates AMSC-001. Because no descriptor is registered, the composed catalog is
not polluted and the uniqueness guard passes; this is recorded as undeclared/unregistered dead code
(SoT watch `R1`), not a certification blocker. No new code was invented to consume it during
certification (that would be a behavior change).
