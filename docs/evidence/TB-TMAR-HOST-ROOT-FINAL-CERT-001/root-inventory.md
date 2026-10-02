# Root inventory — TB-TMAR-HOST-ROOT-FINAL-CERT-001

## Directories (Host root)

Admin, App_Data, Authentication, Caching, Composition, Configuration, Development, Errors, Health, Messaging, MultiTenancy, Observability, Order, Outbox, Persistence, Properties, Security, Transport

(Untracked/local: `bin`, `obj`, `artifacts` — gitignored; not production source.)

## Root files (tracked production)

| File | Disposition |
| --- | --- |
| Program.cs | HOST_COMPOSITION_ROOT — sole root production .cs |
| Tooba.Host.csproj | Host project / composition dependency graph |
| appsettings.json | Environment configuration source |
| appsettings.Production.json | Environment configuration source |
| appsettings.Development.json | Environment configuration source |

## Allowlist

`HostFolderStructureTests` RootCsAllowlist = { Program.cs } — matches reality. Not widened.

## Logs

No tracked `*.log` / `*.err.log` under Host root. `.gitignore` covers `src/backend/Host/Tooba.Host/*.log`.

## Labels

`HOST_ROOT_ALLOWLIST_CERTIFIED`
