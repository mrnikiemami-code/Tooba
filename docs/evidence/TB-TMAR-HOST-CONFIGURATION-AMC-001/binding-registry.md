# binding-registry — TB-TMAR-HOST-CONFIGURATION-AMC-001

## Program DI (Host)

```text
AddOptions<ToobaPlatformOptions>()
  .Bind(section ToobaPlatformOptions.SectionName = "Tooba")
  .ValidateOnStart()
AddSingleton<IValidateOptions<ToobaPlatformOptions>, PlatformOptionsValidator>()
AddSingleton(sp => PlatformOptionsValidator.BuildRegistry(IOptions<ToobaPlatformOptions>.Value))
```

MigrationRunner mirrors: Configure + validator + BuildRegistry singleton.

## Lifetimes / startup

| Artifact | Lifetime | Semantics |
| --- | --- | --- |
| `IOptions<ToobaPlatformOptions>` | Options framework | Mutable bind model; treated startup-frozen |
| `PlatformOptionsValidator` | Singleton | Optional `IHostEnvironment` for Production gates |
| `ControlPlaneRegistry` | Singleton | Built once from options.Value at first resolve |

No `IOptionsMonitor` reload path for registry. **Config reload = ABSENT_INTENTIONAL.**

## Raw vs normalized

| Model | Role |
| --- | --- |
| `ToobaPlatformOptions` (+ nested `*Options`) | Raw mutable bind |
| `ControlPlaneRegistry` / `TenantRecord` | Normalized immutable-ish runtime snapshot |

Runtime consumers prefer registry (MultiTenancy, Outbox workers, Health, Admin access, Development). Persistence resolver reads raw `ConnectionReferences` via options (certified).

## BuildRegistry purity

| Check | State |
| --- | --- |
| Deterministic transform | YES |
| Service locator / I/O / DB | ZERO |
| Environment reads inside BuildRegistry | ZERO (Production env only in Validate) |
| Fail-closed duplicates/invalid hosts/status | YES |

## Registry immutability

`required`/`init` on snapshot types; dictionaries exposed as `IReadOnlyDictionary` but backed by mutable `Dictionary` instances → **MUTABILITY_DEBT** (cast-mutate possible). Classify overall: **IMMUTABLE_ENOUGH** for Host platform CERT with debt note.

## Control-plane semantics

Comments + usage: config snapshot for **one process**, not durable tenant control-plane DB. Confirmed consumers treat it as deployment allowlist/bootstrap.
