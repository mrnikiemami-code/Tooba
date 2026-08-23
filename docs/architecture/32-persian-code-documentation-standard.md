# Tooba — Persian Code Documentation Standard

Status:

```text
COMPLETE (Architect accepted TB-P01-T005)
```

## Why Persian documentation is mandatory

Tooba is implemented by agents and reviewed by the Architect. Weak English or tautological comments become architectural drift.

Required Tooba-owned members must explain **responsibility, contract, invariants, and (when relevant) security, tenant, and failure semantics** in clear professional Persian.

```text
Missing or weak Persian documentation on required Tooba-owned members
= Code Review / Architect Acceptance failure
```

Technical identifiers stay in English inside Persian prose (`TenantId`, `DbContext`, `ActivitySource`, `ProblemDetails`).

## What must be documented

For Tooba-owned production code under `src/backend/` and reusable/platform TypeScript under `src/frontend/`:

```text
Class / Record / Struct / Interface / Enum
Method / Function / Constructor
Property / Indexer
Public delegate
Important internal members whose behavior is not obvious
```

Test **methods** may rely on clear names. Test **fixtures**, shared helpers, and custom factories need concise Persian documentation.

Do not comment every JSX tag.

## Quality bar

Bad:

```text
این کلاس TenantContext است.
```

Good:

```text
زمینهٔ تغییرناپذیر Tenant جاری را پس از resolve امن Host نگهداری می‌کند.
این شناسه مستقل از hostname است و نباید توسط لایه‌های پایین‌تر دوباره از Request استخراج شود.
```

Near architectural boundaries, comments must make invariants visible, for example:

```text
TenantId != Hostname
Host is routing input, not durable identity
No cross-module persistence
ConnectionReference is not raw credential storage
Technical logs are not Audit
Marketplace and SingleStore have distinct resolution semantics
```

Security-sensitive code must say **why** a restriction exists:

```text
Forwarded headers accepted only from trusted proxies
Tenant headers/query/cookies are not authority
Unknown tenant resolution fails closed
Connection strings must not be logged
ProblemDetails must not expose implementation details
```

Never put secrets in comments.

## C# XML policy

Public APIs use `/// <summary>`. Non-trivial parameters, returns, and exceptions use `<param>`, `<returns>`, `<exception>`.

Enforcement (backend):

```text
GenerateDocumentationFile = true
CS1591 is a build error on non-test Tooba projects
```

Implemented in `src/backend/Directory.Build.props`.

There is **no global** `NoWarn` of `CS1591`.

## TypeScript / TSDoc policy

Reusable components, exported functions, types, hooks, utilities, and config helpers use TSDoc/JSDoc in Persian.

ESLint JSDoc enforcement is **not** enabled in this task: the frontend surface is still a platform shell; a brittle rule set would punish JSX noise. Convention + Architect review apply. A dedicated analyzer may be added later if the UI surface grows.

## Generated-code exclusions

Do not require Persian XML on:

```text
EF *.Designer.cs
*ModelSnapshot.cs
obj / bin
node_modules
package lock files
framework-generated boilerplate
```

Scoped in `src/backend/.editorconfig`. Hand-written migration classes may have a short Persian purpose comment.

Test projects disable `GenerateDocumentationFile` so xUnit facts are not forced into XML; fixtures are still documented in source.

## Analyzer / build enforcement

```bash
dotnet build
```

Missing XML on public Tooba-owned C# APIs fails the build via CS1591.

Do not treat this as a semantic Persian-quality checker. Quality remains Architect review.

## Review checklist

- Public C# types/members have strong Persian XML, not name-echo.
- Important Host internals (resolution, connection, ProblemDetails) document fail-closed and non-leakage.
- Frontend reusable/platform modules have TSDoc without tagging every element.
- No blanket CS1591 suppression.
- Generated snapshots untouched except when the tool regenerates them.
- Behavior, schema, and API contracts unchanged unless a later envelope says otherwise.
