# Endpoint boundary — Localization AMC-001-R1

- HTTP owner: Localization.Endpoints.Admin.LocaleAdminEndpoints
- Auth: ILocalizationAdminAuthorizer / HostLocalizationAdminAuthorizer (thin)
- Presentation: ApiResponseFactory only for typed faults
- Host/Localization: ABSENT
- ContentLanguageReferenceGuard: Content.Infrastructure via Localization.Contracts
