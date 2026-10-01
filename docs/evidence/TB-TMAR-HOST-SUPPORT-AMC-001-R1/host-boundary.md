# Host boundary — Support AMC R1

`Composition/SupportDevelopmentSeedHost.cs` end state:

- Uses `Tooba.AccessControl.Contracts.Development` only for AccessControl
- ZERO `AccessControl.Application` / `.Domain` / `.Infrastructure` / `.Persistence`
- ZERO `SupportDbContext` / `Database.MigrateAsync` / `Order.Application`
- Guest actor still `Order.Contracts.Fulfillment.StorefrontGuestActor`
- Still delegates migrate+seed to `SupportDevelopmentSeedBootstrap.ApplyAsync`
- No sink-folder regression; no new Host AccessControl helper folder
