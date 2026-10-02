# production-validation — TB-TMAR-HOST-CONFIGURATION-AMC-001

## ValidateProductionRequirements (Production env only)

| Rule | State |
| --- | --- |
| Edition not Unset | YES |
| SingleStore ≥1 tenant | YES |
| Edition-required connection refs present + non-empty | YES via `CollectConfiguredConnectionReferences` |
| StoreCommerce complete (Market + DefaultCurrency + valid SalesChannel) | YES |
| DeploymentId required | **NO** |
| TrustedProxies IP validity | **NO** in validator (Program-owned) |
| Connection string syntax parse | **NO** (Persistence resolver defensive) |

## CollectConfiguredConnectionReferences policy

| Edition | Refs collected |
| --- | --- |
| Marketplace | Marketplace connection reference |
| SingleStore | **All** tenant connection references (Active + Disabled + Suspended) |

Compare:

| Surface | Policy |
| --- | --- |
| Startup validation | All SingleStore tenant refs |
| StoreCommerce Production | Active tenants only |
| Health | Collects configured refs (parity intent with readiness) |
| Outbox / MultiTenancy serving | Active-oriented at consumer |

**Assessment:** intentional asymmetric policy (inactive tenants still need configured DBs if listed). Messaging reference **not** in this collect set.

## TrustedProxies

`ToobaPlatformOptions.TrustedProxies` bound on options, but Program reads `Tooba:TrustedProxies` array separately and `IPAddress.TryParse` — **invalid entries silently skipped** (no fail-fast). Classify: `PROGRAM_OWNED_SILENT_SKIP_DEBT` (outside Configuration W1 unless Architect expands).

## Validator exception message propagation

`catch (InvalidOperationException ex) => ValidateOptionsResult.Fail(ex.Message)` = startup operator text propagation, **not** runtime message classification. BuildRegistry messages embed tenant IDs/hosts/edition names — not connection strings.
