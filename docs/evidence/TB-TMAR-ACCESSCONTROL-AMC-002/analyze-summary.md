# AccessControl AMC re-run Analyze — TB-TMAR-ACCESSCONTROL-AMC-002

Target: `Tooba.AccessControl.*` (full module folder)  
Skills: analyze → migrate → certify  
Mode: ANALYSIS_ONLY  
Baseline HEAD: `00d33e46` (prior AMC-001 W5 CERT)

## Verdict vs prior CERT

Prior `accessControlModuleAmc001W5Cert` remains largely valid. Fresh skill scan against **current** AMC bar finds **one residual blocker** on the Development surface that was previously classified non-blocking:

| ID | Finding | Severity |
| --- | --- | --- |
| R1 | `SellerDevContextEndpoints`: ad-hoc `Results.Json` + hardcoded FA title (`در دسترس نیست`); query returns `SellerDevContextsView?` not `Result<T>` | RESULT_PIPELINE / LOCALIZATION |

## Already good (preserve)

- VS `/Modules/AccessControl/` + Endpoints in `Tooba.slnx`
- Foreign App/Infra/Domain ProjectReference **ZERO** (Contracts-only abroad)
- Endpoints → Domain **ZERO**
- Product Admin/AdminSeller/Seller routes: `Result` + `api.From`, zero `catch (AccessControlException)`
- Stable codes + catalog/resx; CapabilityGate uses codes only
- Domain Aggregates; Application Models/Ports/Exceptions split
- Validators 6/6; Host AccessControl residue ZERO
- Durable guards W1–W5 PASS

## Recommended waves

1. **W1 Migrate** — SellerDev Result + catalog/resx + `api.From` (eliminate last Endpoints `Results.Json`)
2. **W2 Cert** — re-affirm ARCH-COMPLETE-002 with stricter Endpoints `Results.Json` ZERO guard

## Behavior

Analyze wave: no production code change.
