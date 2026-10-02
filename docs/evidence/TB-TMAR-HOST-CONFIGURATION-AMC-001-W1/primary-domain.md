# primary-domain — TB-TMAR-HOST-CONFIGURATION-AMC-001-W1

| Case | State |
| --- | --- |
| null / blank | optional; PrimaryDomain falls back to first host |
| valid | normalized; added to Hosts when absent |
| invalid non-empty | BuildRegistry throws; Validate fails |
| duplicate across tenants | Duplicate host mapping rejected |

HostNormalizer remains canonical. TenantId identity unchanged (Ordinal).
