# Party physical tree (before AMSC)

```
Modules/Party/
  Application/
    PartyContracts.cs                    # ROOT DUMP
    Admin/Sellers/{Queries,Validators}
    Seller/{Commands,Queries,Models,Validators,PartySellerSettingsErrorCodes.cs}
  Contracts/                             # ROOT interfaces dump
    AdminSellersGridContracts.cs
    IParty*.cs
  Domain/
    PartyDomain.cs                       # GOD FILE (~542)
  Endpoints/
    Admin/Sellers/, Seller/, Errors/, Resources/
  Infrastructure/
    PartyDirectory.cs, PartyOutboxRegistration.cs, ...  # ROOT
    Adapters/, Admin/, Development/, Events/, Grid/, Persistence/, Seller/
```

slnx: five Party projects under flat `/Modules/` (no `/Modules/Party/` folder).
