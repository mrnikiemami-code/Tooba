# Migrate — Host/Preferences AMC-001

## Waves

1. **Contracts**: `UserPreference.Contracts.Errors` + catalog codes (`preference.*`, `ui_preference.*`, session)
2. **Foundation**: `UserPreference.Endpoints` (Customer locale, Admin locale, Admin UI) + resx/error contributor
3. **CQRS**: LocalePreferences + UiPreferences Commands/Queries over existing directories
4. **Domain**: typed `SemanticException` instead of `InvalidOperationException` text
5. **Auth**: module `IUserPreferenceAdminAuthorizer`; Host thin `HostUserPreferenceAdminAuthorizer`; customer actor resolver (guest via Order.Contracts)
6. **Host ZERO**: deleted `Host/Preferences`; Program maps `MapUserPreferenceModuleEndpoints` + `AddUserPreferenceEndpointPresentation`

## Behavior preserved

- Routes/verbs unchanged:
  - `GET|PUT /v1/customer/preferences`
  - `GET|PUT /v1/admin/operator/preferences`
  - `GET|PUT /v1/admin/ui-preferences/{key}`
- Default locale `fa`; allowed `fa`|`en`
- UI key normalization + JSON payload semantics
- Schema/migrations unchanged; frontend unchanged
