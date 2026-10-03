# OperatorProfile physical tree (before AMSC)

```
Modules/OperatorProfile/
  Application/
    Admin/Commands/UpsertOperatorProfileCommand.cs   # command+handler+validator
    Admin/Queries/GetOperatorProfileQuery.cs
    OperatorProfileContracts.cs                      # ROOT DUMP models+port
  Contracts/
    ActorDisplayContracts.cs                         # ROOT DUMP
    Errors/OperatorProfileErrorCodes.cs
  Domain/
    OperatorProfile.cs                               # ROOT DUMP
  Endpoints/                                         # NOT IN slnx
    Admin/...
    Errors/OperatorProfileErrorCatalogContributor.cs
    OperatorProfileEndpointModule.cs
  Infrastructure/
    ActorDisplayLookupAdapter.cs                     # ROOT
    OperatorProfileDirectory.cs                      # ROOT
    OperatorProfileModule.cs                         # + Outbox in same file
    Development/
    Persistence/ (+ Migrations)
```

slnx: Domain/Contracts/Application/Infrastructure under flat `/Modules/` — Endpoints absent.
