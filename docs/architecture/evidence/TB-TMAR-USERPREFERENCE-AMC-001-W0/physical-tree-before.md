# UserPreference physical tree (before AMC)

```text
Modules/UserPreference/
  Tooba.UserPreference.Domain/
    UserPreference.cs              # root dump
    UiPreference.cs                # root dump
  Tooba.UserPreference.Application/
    UserPreferenceContracts.cs     # ports + models dump
    UserPreferenceShapes.cs        # root dump
    LocalePreferences/{Commands,Queries}/
    UiPreferences/{Commands,Queries}/
  Tooba.UserPreference.Contracts/
    Errors/UserPreferenceErrorCodes.cs
  Tooba.UserPreference.Infrastructure/
    UserPreferenceModule.cs        # + Outbox type
    UserPreferenceDirectory.cs     # root
    UiPreferenceDirectory.cs       # root
    Persistence/{DbContext,Migrations}
    Development/
  Tooba.UserPreference.Endpoints/
    UserPreferenceEndpointModule.cs
    Customer/ Admin/
    Errors/ Resources/             # catalog ownership wrong layer
```

## slnx before

- Flat `/Modules/`: Domain, Application, Infrastructure only
- Contracts + Endpoints: **not listed**
