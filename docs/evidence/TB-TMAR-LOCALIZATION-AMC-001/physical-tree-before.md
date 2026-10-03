# TB-TMAR-LOCALIZATION-AMC-001 — physical tree before

```
Modules/Localization/
  Tooba.Localization.Application/
    LanguageContracts.cs
  Tooba.Localization.Contracts/
    Errors/LanguageErrorCodes.cs
    ILanguageActivationPort.cs
    ILanguageReferenceGuard.cs
    LanguageLookupContracts.cs
  Tooba.Localization.Domain/
    Language.cs
  Tooba.Localization.Endpoints/   ← NOT in Tooba.slnx
    Admin/LocaleAdminEndpoints.cs
    Admin/ILocalizationAdminAuthorizer.cs
    Errors/LocalizationErrorCatalogContributor.cs
    LocalizationEndpointModule.cs
  Tooba.Localization.Infrastructure/
    LanguageDirectory.cs
    LanguageActivationBridge.cs
    LanguageLookupBridge.cs
    LanguageBootstrapHostedService.cs
    LocalizationModule.cs
    LocalizationOutboxRegistration (same file as module)
    Persistence/LocalizationDbContext.cs
    Persistence/Migrations/*
```

Solution: flat `/Modules/` entries for Contracts/Domain/Application/Infrastructure only.
