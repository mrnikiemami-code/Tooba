# Migrate — Host/Localization AMC-001

## Waves

1. **Contracts**: `ILanguageReferenceGuard` + `LanguageErrorCodes` moved to Localization.Contracts
2. **Content guard**: `ContentLanguageReferenceGuard` → Content.Infrastructure.Adapters; registered in ContentModule
3. **Endpoints foundation**: new `Tooba.Localization.Endpoints` (LocaleAdminEndpoints, authorizer port, error catalog)
4. **Failure semantics**: InvalidOperation/ContractOperation language codes → SemanticException + ApiResponseFactory (message-as-code ZERO)
5. **Host**: `HostLocalizationAdminAuthorizer`; Program maps `MapLocalizationModuleEndpoints`; deleted `Host/Localization/`
6. **Guards**: `HostLocalizationAmcGuardTests` + durable SoT

## Behavior preserved

- Routes: GET/POST `/v1/admin/languages`, PUT/PATCH `/v1/admin/languages/{code}`
- Success JSON shape unchanged (languageId/code/urlPrefix/…)
- Admin auth via Host panel access adapter
- Schema / frontend unchanged
