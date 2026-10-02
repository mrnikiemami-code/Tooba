# Story AMC W3 — capability foldering (VS)

Parent: TB-TMAR-STORY-AMC-001-W2-CERT

## Changes

- Domain split: `Enums/`, `Rules/`, `Tenant/`, `Aggregates/` (path↔namespace)
- Application capability `Stories/` with Commands/Queries/Models/Ports/Presentation/Validators (root Application .cs = ZERO)
- Infrastructure: `Directory/`, `Development/`; root allowlist = `StoryModule.cs` only
- `Tooba.slnx`: dedicated `/Modules/Story/` solution folder (removed flat Modules dump duplicate)

## Not claimed

Full ARCH-COMPLETE-002 `structureCertified` / validator matrix / Result&lt;T&gt; Domain — deferred.
