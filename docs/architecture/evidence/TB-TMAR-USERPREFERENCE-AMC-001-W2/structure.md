# TB-TMAR-USERPREFERENCE-AMC-001-W2 — Layer structure

## Physical layout (after)

```text
Modules/UserPreference/
  Domain/Aggregates/{UserPreference,UiPreference}.cs
  Application/
    Ports/{IUserPreferenceDirectory,IUiPreferenceDirectory}.cs
    Models/UserPreferenceModels.cs
    LocalePreferences/{Commands,Queries,Validators}/
    UiPreferences/{Commands,Queries,Validators}/
  Contracts/Errors/
  Infrastructure/
    UserPreferenceModule.cs
    Directories/
    Persistence/{DbContext,Migrations,Outbox}
    Development/
  Endpoints/  (catalog/Result deferred to W3)
```

## Deferred to W3

- Endpoints `Results.Json` + `catch SemanticException` → Result pipeline
- Error catalog/resx → Contracts
- Validator codes → UserPreferenceErrorCodes + catalog
