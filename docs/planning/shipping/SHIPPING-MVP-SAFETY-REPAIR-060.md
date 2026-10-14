# TB-SHIPPING-MVP-SAFETY-REPAIR-060 — null-scope 500 repair and wrong-target scratch safety

- Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
- Parent: TB-SHIPPING-MVP-AUTHORIZED-ADMIN-059
- Mode: BOUNDED_SHIPPING_NULL_SCOPE_AND_SCRATCH_TARGET_SAFETY_REPAIR
- Expected base: `cf48f77fdddd11dba8267ba2b0a143d2dc0b8b3b`
- Stop state: STOP_FOR_ARCHITECT
- Automatic next implementation task: NONE

## 1. What the Architect asked for

059 was PARTIAL-accepted for authenticated HTTP contract proof only, with two bounded safety defects to
repair before any further runtime activity:

- **A.** an omitted/null `scope` in the Shipping rate/coverage write bodies produced a 500 through an unsafe
  FluentValidation nested-property dereference;
- **B.** the 059 incident itself — an incorrect override key (`tenant_alpha` instead of `tenant-alpha`) let an
  "isolated" Host silently write to the real Development database `tooba_alpha`.

This task repairs both, with no database contact of any kind, no Host start, and no HTTP request.

## 2. Mission A — the verified 500

### Reproduced cause (source)

`ShippingAdminValidators.cs` used:

```csharp
RuleFor(x => x.Scope).NotNull();
RuleFor(x => x.Scope.CountryId).NotEmpty();                 // evaluated even when Scope is null
RuleFor(x => x.Scope.ServiceAreaId).Null().When(x => x.Scope is not null && x.Scope.CityId is null);
```

Under FluentValidation's default `CascadeMode.Continue`, a failing `NotNull()` rule does not stop the next
rule from being evaluated, and the property expression `x => x.Scope.CountryId` is *invoked* while evaluating
it. With `scope` omitted from the JSON body, `x.Scope` is `null` and the access throws
`NullReferenceException`. The exception is not a `SemanticError`, so it never reaches the canonical
`validation.failed` 4xx path — it escapes as `platform.unexpected` / 500.

`log-null-scope-focused-tests.txt` records a local reproduction: a validator built with the *old* rule shape
throws `NullReferenceException` for a null scope, while the repaired validator returns a declared validation
failure.

### Repair

Both write-request validators now:

1. declare the null scope with a stable error code
   `shipping.validation.scope_required` (`ShippingValidationCodes.ScopeRequired`), and
2. guard every nested-property rule behind `When(x => x.Scope is not null, …)`, so the member access is never
   evaluated for an absent scope.

Existing scope-shape checks are unchanged: `shipping.validation.country_required` and
`shipping.validation.service_area_requires_city` still fire on their original inputs, and the Geography
domain validator (`IShippingDestinationScopeValidator`) is untouched. No request DTO semantics, rates,
permissions, service catalog, geo data or checkout code changed.

### Translation to 4xx (in-memory, not a live claim)

`ValidationBehavior` raises `FluentValidation.ValidationException`; `SafeErrorMapper.MapValidation` maps it to
the foundation descriptor `validation.failed` with `HttpStatus = 400` and a per-property map of the declared
codes. The tests assert exactly that: status 400, error code `validation.failed`, never `platform.unexpected`,
with `shipping.validation.scope_required` under the `…Scope` key. This is the canonical request-pipeline and
presentation-envelope proof **in memory** — it is not a live HTTP assertion and no live 400 was observed in
this task.

## 3. Mission B — wrong-target scratch verification

### Root mistake, isolated exactly

Microsoft.Extensions.Configuration reads keys as `Tooba:PostgreSQL:ConnectionReferences:<reference>` with a
`__` environment separator, so the correct override key for the hyphenated reference is
`Tooba__PostgreSQL__ConnectionReferences__tenant-alpha`. 059 used
`…ConnectionReferences__tenant_alpha`. The underscore never matched the hyphenated key (the options
dictionary is `OrdinalIgnoreCase`, which cannot bridge `-` vs `_`), the override was silently ignored, and the
tenant kept resolving `Database=tooba_alpha`.

### Guard added

`assert-scratch-target.js` — a reusable, dependency-free, fail-closed **offline** preflight. It resolves the
effective configuration for one tenant from (a) an explicit config file and (b) an explicit `KEY=VALUE`
override file, applying ASP.NET override precedence (`__` → `:`), and then refuses unless:

| Check | Refusal |
| --- | --- |
| tenant declared exactly once | `3` unknown target / `4` ambiguous |
| every `Tooba__…ConnectionReferences__<ref>` override is the reference the tenant actually resolves; a reference that only normalizes to it (`-`/`_`) is the 059 signature, and an override no tenant references is a silent miss | `5` shadowed/unused override |
| resolved host, port and database equal the expected values exactly | `6` mismatch |
| database is a known live name or matches a live pattern | `7` live database |
| database matches `^tooba_\d{3}_scratch[a-z0-9_]*$` and ends with the per-run unique sentinel (`^[a-z0-9]{8,32}$`) | `7` not an exact scratch name |
| no other connection reference in the same configuration points at the same database | `7` shared target |
| the emitted SHA-256 fingerprint equals `--expect-fingerprint` | `6` mismatch |

It opens no connection, starts no Host, sends no HTTP request, never reads `process.env` or `appsettings`,
and prints only host/port/database plus `USERNAME_PRESENT` / `PASSWORD_PRESENT` booleans — never credentials.

### Trust boundary (explicit)

A PASS means the *supplied* configuration resolves to the expected scratch target. It is a preflight, not a
runtime guarantee: anything able to mutate the environment, the config file or user-secrets **after** the
check (parent shell, launch profile, a later `appsettings.*.json`, a wrapper) invalidates it. The launched
process must therefore run with `Tooba__*` / `ASPNETCORE__*` cleared and must receive the printed
`--expect-fingerprint`, which is the only machine-checkable link between "what was checked" and "what runs".
The script cannot prove anything about database contents and does not fix every possible misrouting path.

### Proof with synthetic fixtures only

`run-fixture-proofs.js` runs the guard over synthetic fixtures and asserts exact exit codes. All 12
expectations hold (`log-fixture-proofs.txt`, `log-guard-direct-runs.txt`):

| Case | Exit | Meaning |
| --- | --- | --- |
| 059 faulty underscore override | 5 | the incident is now caught, naming the offending key |
| safe scratch-only configuration | 0 | correctly wired disposable clone accepted |
| production-like target | 7 | correctly wired production target still refused |
| no override (config's own value) | 7 | live database refused |
| override no tenant references | 5 | silent miss refused |
| unknown tenant | 3 | refused |
| ambiguous tenant declaration | 4 | refused |
| non-unique sentinel | 2 | refused |
| fingerprint agrees / differs | 0 / 6 | preflight-to-launch binding enforced |
| source has no database/network client; never reads process env | 0 | offline by construction |

No appsettings file, live credential or live database name is used as an input; the fixture hosts are
`10.255.255.9` / `127.0.0.1` synthetic values with fixture credentials.

## 4. Verification performed

```
dotnet build Tooba.Shipping.Application            -> Build succeeded, 0 errors
dotnet build Tooba.Shipping.Tests                  -> Build succeeded, 0 errors
dotnet test  Tooba.Shipping.Tests (focused filter)  -> 20/20 passed
dotnet test  Tooba.Shipping.Tests (whole project)   -> 89/89 passed
node run-fixture-proofs.js                         -> FIXTURE_PROOFS=PASS (12/12), exit 0
```

No database connection, no Docker, no Host start/restart/deploy, no HTTP request to any backend, no
appsettings/user-environment/tenant-registry change, no migration, no pricing rows touched.

## 5. Remaining risk / limitations

- The 059 rollback (3 rate policies, 2 coverage grants, 5 auth sessions) remains **Worker evidence only** —
  not independently verified by the Architect. The unattributable `pricing.prices` (+8) and
  `pricing.outbox_messages` (+24) deltas were **not touched**, as instructed.
- The user's own long-running Host process was never modified, restarted or inspected for this task.
- Checkout shadow remains OFF; no rate or coverage data is assumed to exist.
- Any future live test still needs an independently checked target provenance; this guard is offline and
  configuration-scoped only.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` was deliberately **not** modified: the incident and this
  ruling are recorded in the 059 and 060 planning/evidence artifacts, and appending to a shared master
  recovery document was not essential for this bounded repair.
