# 03 — Secret safety proof (TB-P06-T001)

| Check | Result |
|---|---|
| Committed production secrets | **NONE** in base/production templates |
| Default production credentials | **NONE** in `appsettings.Production.json` |
| Dev credentials | Present only in `appsettings.Development.json` (DEV_ONLY) |
| Secret loading | Environment variable overrides (`Tooba__PostgreSQL__ConnectionReferences__*`) |
| SpiceDB token in source | Empty placeholder; ValidateOnStart requires env injection when Mode=SpiceDb |
| Frontend `NEXT_PUBLIC_*` secrets | **NONE** (zero matches) |
| docker-compose password | Labelled `dev-only-not-for-production` |

Actions this task: added `appsettings.Production.json` without secrets; SpiceDB token required at startup when Mode=SpiceDb; added `.env.example` for server-only Host origin.

No secret values added to evidence or commits.
