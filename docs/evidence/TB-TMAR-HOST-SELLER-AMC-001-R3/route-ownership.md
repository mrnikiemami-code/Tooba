# Host/Seller — Seller-R3 — Route Ownership

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R3

## 1. Ownership matrix (after R3)

| Route | Verb | Owner |
| --- | --- | --- |
| `/v1/seller/settings` | GET | `Tooba.Party.Endpoints` (`PartySellerSettingsEndpoints`) |
| `/v1/seller/settings` | PUT | `Tooba.Party.Endpoints` (`PartySellerSettingsEndpoints`) |
| `/v1/seller/dashboard` | GET | `Tooba.Host` (`SellerPanelEndpoints`) |
| `/v1/seller/dev-contexts` | GET | `Tooba.Host` (`SellerPanelEndpoints`) |

## 2. Counts

| Metric | Before R3 | After R3 |
| --- | --- | --- |
| Host-owned seller routes | 4 | 2 |
| Party-owned seller routes | 0 | 2 |
| Duplicate route ownership | 0 | 0 |
| Host/Seller business files | 5 | 4 |

## 3. Duplicate-ownership proof

- The settings route template appears in `SellerPanelEndpoints.cs` **zero** times
  (`HostSellerAmcR3GuardTests.Host_seller_owns_exactly_two_routes_and_zero_settings_route`).
- `SellerPanelEndpoints.cs` contains exactly two `group.Map*` calls => Host-owned total 2.
- `PartySellerSettingsEndpoints.cs` contains exactly two `Map*` calls (`MapGet("/")`, `MapPut("/")`).
- `MapGroup("/v1/seller/settings")` is declared exactly once, in `PartyEndpointModule.MapPartyEndpoints`.
- `Program.cs` calls `MapPartyEndpoints()` and does **not** call `MapSellerSettingsEndpoints()`
  (the method no longer exists), so the settings group cannot be double-mapped.

## 4. Host namespace-source mapping (`SellerPanelEndpoints.cs`)

| Host source | Role after R3 |
| --- | --- |
| `group.MapGet("/dashboard", GetDashboardAsync)` | Host-owned dashboard shell (Order CQRS + Party display) |
| `group.MapGet("/dev-contexts", GetDevContexts)` | Host-owned Development actor snapshot |
| ~~`/v1/seller/settings` GET~~ | evacuated to Party |
| ~~`/v1/seller/settings` PUT~~ | evacuated to Party |
