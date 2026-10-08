# Folder-Granularity-State

**Verdict: `PROFESSIONAL_SHALLOW`**

## 1. Application — primary axis is capability

```text
Application/
  ReturnRequests/        <- business capability (return request lifecycle)
    Commands/            <- secondary technical axis
    Queries/
    Models/
    Ports/
  Composition/           <- shared cross-capability mechanism
  Validation/            <- shared cross-capability transport validation
```

Depth from project root to a source file is at most **3** (`Application/ReturnRequests/Commands/X.cs`).
The W0 tree was depth 4 with 11 unjustified leaves (`Application/Commands/<UseCase>/X.cs`).

Capability name `ReturnRequests` is **not mechanically invented**: it is the module's own business
axis already used by the Domain aggregate `ReturnRequest`, the Domain event `ReturnRequestedDomainEvent`,
the Contracts event `ReturnRequestedIntegrationEvent` and the port `IReturnDirectory`
(see `capability-map.md`).

## 2. Single-file leaf folder rule (section 8) — 0 violations

The rule counts **production source files**, not declared types. Enumerated result for the semantic
request/use-case trees:

| Scope | Leaf folders | Files in each | Verdict |
|---|---|---|---|
| `Application/ReturnRequests/Commands/` | 0 subfolders | 4 files at axis level | PASS |
| `Application/ReturnRequests/Queries/` | 0 subfolders | 7 files at axis level | PASS |
| `Application/ReturnRequests/Models/` | 0 subfolders | 9 files at axis level | PASS |
| `Application/ReturnRequests/Ports/` | 0 subfolders | 4 files at axis level | PASS |
| `Application/Composition/` | 0 subfolders | 2 files at axis level | PASS |
| `Application/Validation/` | 0 subfolders | 2 files at axis level | PASS |

Retired over-foldered paths (must never return; asserted by the durable guard):

```text
Application/Commands/ApproveReturn/ApproveReturnCommand.cs
Application/Commands/CreateReturn/CreateReturnCommand.cs
Application/Commands/RejectReturn/RejectReturnCommand.cs
Application/Commands/RetryReturnRefund/RetryReturnRefundCommand.cs
Application/Queries/GetAdminReturn/GetAdminReturnQuery.cs
Application/Queries/GetCustomerReturn/GetCustomerReturnQuery.cs
Application/Queries/GetSellerReturn/GetSellerReturnQuery.cs
Application/Queries/ListAdminReturns/ListAdminReturnsQuery.cs
Application/Queries/ListCustomerReturns/ListCustomerReturnsQuery.cs
Application/Queries/ListSellerReturns/ListSellerReturnsQuery.cs
Application/Queries/QueryAdminReturnsGrid/QueryAdminReturnsGridQuery.cs
```

All 11 held exactly **one** production source file ⇒ `OVER_FOLDERED` by default, with no
"multiple types in one file" or "request + handler co-located" justification available.

## 3. Technical-axis-first detection (section 10) — 0 violations

`TECHNICAL_AXIS_FIRST` is flagged when Application's **primary** axis is `Commands/ | Queries/ |
Validators/`. At the Application project root the folders are now:

```text
Composition  ReturnRequests  Validation
```

`Commands`/`Queries`/`Models`/`Ports`/`Validators`/`Errors`/`Handlers`/`Requests` at the Application
root are recorded as `forbiddenTopLevelFolders` in the manifest and asserted absent by the guard.
`ReturnRequests/Commands/` is capability-first and is **not** flagged (section 10 detection scope).

## 4. Folder-depth / explosion rules (section 11)

- No `Commands/<Capability>/<UseCase>/<UseCase>/…` nesting.
- No empty ceremonial folders anywhere in the module.
- No folder exists whose only content is another folder.
- `Validation/` and `Composition/` are shared homes used by the repository pattern for
  cross-capability concerns (section 5 allowance) — not technical-axis-first roots.

## 5. Legitimate deeper folders (not rejected)

| Path | Why it is legitimate |
|---|---|
| `Infrastructure/Persistence/Migrations/` | EF migrations + designer + snapshot — locked repository exemption |
| `Contracts/{Errors,Events,History,Operations,Resources,Settlement}` | capability/purpose folders, all multi-file or single-purpose boundary homes |
| `Domain/{Aggregates,Events,ValueObjects}` | module-established domain pattern |
| `Endpoints/{Admin,Customer,Seller}` | audience capability folders, each multi-file |
| `Tests/{Architecture,Behavior,Endpoints}` | test-axis folders, each multi-file |

## 6. Per-use-case exception rule (section 9)

Not used. Every use case fits the shallow capability axis; no deeper leaf was needed and none was
created.
