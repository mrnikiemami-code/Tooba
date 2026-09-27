# Semantic structure repair — TB-TMAR-HOST-ADMIN-AMC-001-W2-R1

Removed mixed Application bundles:
- Settings/StoreQuantitySettingsContracts.cs
- Settings/StoreQuantitySettingsHandlers.cs
- Settings/SaveStoreQuantitySettingsCommandValidator.cs

Replaced with capability-first shallow Quantity surface under Settings/Quantity/{Commands,Queries,Models,Ports,Validators,Mapping}.
No Application *Contracts.cs remains on the W2 Quantity surface.
W2 behavior/boundaries preserved (ISender, Result, ApiResponseFactory, Host Admin 58, Appearance deferred).
