# AccessControl seam — Support AMC R1

Added Contracts development prelude:

- Path: `Tooba.AccessControl.Contracts/Development/AccessControlDevelopmentSeedContracts.cs`
- Namespace: `Tooba.AccessControl.Contracts.Development` (EXACT)
- Interface: `IAccessControlDevelopmentSeedPrelude.EnsureSupportSeedPrerequisitesAsync`
- DTO: `AccessControlDevelopmentSeedActors(SellerPartyId, SellerActorUserId)`

Implementation:

- `Tooba.AccessControl.Infrastructure/Development/AccessControlDevelopmentSeedPrelude.cs`
- Registered by `AccessControlModule`
- Internally uses `ISellerDevContextStore` + `IAccessControlDirectory` + `AccessOwnerScope` (AccessControl-owned)

No Application/Domain types leak through Contracts. No Host dependency in AccessControl.
