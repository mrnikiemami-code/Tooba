# Localization + error-catalog ownership — AccessControl (W3)

## Classification

| Aspect | State |
| --- | --- |
| Localization mechanism | `CANONICAL` |
| Error-code descriptor ownership | `UNIQUE_OWNER` |
| Composed-catalog duplicate descriptors | `ZERO` |
| `DUPLICATE_ERROR_DESCRIPTOR` | `ZERO` |
| `UNREGISTERED_CODES` | `ZERO` |
| `UNRESOLVED_ERROR_OWNER` | `ZERO` |
| `HARDCODED_TEXT` | `ZERO` |
| `Accept-Language` parsing in endpoints | `ZERO` |

## Owned error-code vocabulary — 20 codes

`Tooba.AccessControl.Contracts.Errors.AccessControlErrorCodes`:

```text
access.role.code_conflict
access.role.system_immutable
access.role.archived
access.role.not_found
access.user.invalid
access.assignment.exists
access.assignment.not_found
access.ceiling.not_delegable
access.scope.unsupported
access.scope.unknown_resource
access.owner.invalid
access.escalation.platform_permission
access.escalation.ceiling
access.validation.text
access.validation.code
access.permission.unknown
access.authorization.unavailable
access.capability.denied
seller.dev.unavailable
seller.dev.not-ready
```

## Descriptor ownership — exactly one contributor

`Endpoints/Errors/AccessControlErrorCatalogContributor.cs` (`IErrorCatalogContributor`) registers
**all 20** codes with `Code`, `Classification`, explicit `HttpStatus`, `LocalizationKey` (= code),
`Severity` and `SafeTitleFallback`, using the owned constants (not raw literals).

| Check | Result |
| --- | --- |
| Contributors registering `access.*` codes | 1 |
| Contributors registering `seller.dev.*` codes | 1 (same file) |
| Other modules registering any `access.*` / `seller.dev.*` code | `ZERO` (repo-wide scan) |
| Duplicate machine code inside the contributor | `ZERO` |
| Duplicate-suppression mechanism (`first/last wins`, `DistinctBy`, overwrite) | `ZERO` |

## HTTP status mapping (owned by descriptor, not by `code.Contains` heuristics)

| Code | Classification | HTTP |
| --- | --- | --- |
| `access.role.not_found` | NotFound | 404 |
| `access.assignment.not_found` | NotFound | 404 |
| `seller.dev.unavailable` | NotFound | 404 |
| `access.role.code_conflict` | Conflict | 409 |
| `access.assignment.exists` | Conflict | 409 |
| `access.role.system_immutable` | Validation | 400 |
| `access.role.archived` | Validation | 400 |
| `access.user.invalid` | Validation | 400 |
| `access.ceiling.not_delegable` | Validation | 400 |
| `access.scope.unsupported` | Validation | 400 |
| `access.scope.unknown_resource` | Validation | 400 |
| `access.owner.invalid` | Validation | 400 |
| `access.validation.text` | Validation | 400 |
| `access.validation.code` | Validation | 400 |
| `access.permission.unknown` | Validation | 400 |
| `access.escalation.platform_permission` | Forbidden | 403 |
| `access.escalation.ceiling` | Forbidden | 403 |
| `access.capability.denied` | Forbidden | 403 |
| `access.authorization.unavailable` | Platform | 503 |
| `seller.dev.not-ready` | Platform | 503 |

No `code.Contains(...)` / `code.StartsWith(...)` heuristic exists anywhere in the module.

## Localization resources

| Resource | Path |
| --- | --- |
| Default (`en`) | `Endpoints/Resources/AccessControlErrors.resx` |
| Persian (`fa`) | `Endpoints/Resources/AccessControlErrors.fa.resx` |
| Resource-set declaration | `Endpoints/Resources/AccessControlErrorResources.cs` (`AccessControlErrorResourceSet : IErrorResourceSet`, owns the `access.` prefix) |

Both `.resx` files carry every owned key with matching names
(`access.role.*`, `access.assignment.*`, `access.ceiling.*`, `access.scope.*`, `access.owner.*`,
`access.escalation.*`, `access.validation.*`, `access.permission.*`, `access.authorization.*`,
`access.capability.*`, `seller.dev.*`).

## Hard-coded text scan

| Scan | Result |
| --- | --- |
| User-facing Persian/English literal in Domain/Application/Infrastructure/Endpoints | `ZERO` |
| `exception.Message` / `ex.Message` used as a localized or user-facing contract | `ZERO` |
| Endpoint-level `Accept-Language` parsing | `ZERO` |
| `IRequestLocaleResolver` bypass | `ZERO` |

Persian text in production `.cs` appears only inside XML-documentation comments.
`SafeTitleFallback` values in the contributor are English diagnostic fallbacks required by the
canonical `ErrorDescriptor` contract, not user-facing localized messages — the localized text is
resolved from the module `.resx` set.

## Preservation

No localization key was renamed or repurposed by W0/W1/W2/W3. All 20 codes and all `.resx` keys
present before the AMSC run are still present with identical semantics.
