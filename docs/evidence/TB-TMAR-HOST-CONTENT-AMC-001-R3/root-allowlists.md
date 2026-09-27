# Root allowlists — Content

| Project | rootAllowlist | Justification |
|---|---|---|
| Tooba.Content.Contracts | [] | Errors/ only |
| Tooba.Content.Domain | [] | Aggregates/ + Rules/ |
| Tooba.Content.Application | [] | Composition/ContentOperation.cs under folder |
| Tooba.Content.Endpoints | ContentEndpointModule.cs, GlobalUsings.cs | composition + project-wide usings |
| Tooba.Content.Infrastructure | ContentModule.cs | composition entry only |

Forbidden root files enumerate migrated legacy names (directories, seed, ContentOperation, domain entities/rules, composers). Disk roots match allowlists exactly after R3 moves.
