# Contracts guard — TB-TMAR-CONTRACTS-W4

Generic guard `Module_Contracts_projects_do_not_reference_Domain_Infrastructure_or_Host` now also forbids:
- foreign `*.Application` ProjectReferences
- EF `PackageReference`s

Applies to all current `*.Contracts` projects (Offer, Wallet, Tax).

BuildingBlocks primitives remain allowed (e.g. `QuantityRoundingMode`).
