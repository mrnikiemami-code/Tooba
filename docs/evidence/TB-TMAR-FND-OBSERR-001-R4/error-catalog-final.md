# Error catalog final — TB-TMAR-FND-OBSERR-001-R4

`ErrorDefinitionCatalog` builds one dictionary at startup. Duplicate codes throw `duplicate_error_descriptor:{code}`. The map is not exposed for later mutation.

Foundation contributor registers:

- `validation.failed` — Validation, 400, Warning
- `platform.unexpected` — Unexpected, 500, Error
- `platform.error` — Platform, 500, Error

`OfferErrorCatalogContributor` registers every public `OfferErrorCodes` constant used by the seller API, each with status, classification, localization key, severity Warning, and an English safe title. `OfferErrorCatalogAndLocalizationTests.Every_public_offer_error_code_constant_has_descriptor_and_english_resource` checks the set.

`SafeErrorMapper` has no `ClassifySemanticCode` and no substring status rules. A registered semantic code uses the descriptor. An unknown semantic code returns a fixed 400 Business result and keeps the code. It does not infer 404 or 409 from the code text.

`PlatformHttpException` remains a compatibility input: HTTP status comes from the exception; a registered code supplies the localization key. Unrelated modules were not migrated.

Verdict: PASS.
