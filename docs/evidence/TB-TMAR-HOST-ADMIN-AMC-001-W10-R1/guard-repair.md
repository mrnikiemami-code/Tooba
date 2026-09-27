# Guard repair — TB-TMAR-HOST-ADMIN-AMC-001-W10-R1

## Stale expectations removed

| Guard | Before | After |
|---|---|---|
| W8 Host Attribute file | `Contains("MapAttributeInvalid")` | `DoesNotContain("MapAttributeInvalid")` |
| W9 Host Attribute file | `Contains("MapAttributeInvalid")` | `DoesNotContain("MapAttributeInvalid")` |
| W10 Host Attribute file | `Contains("SetProductAttributeRequest")` | `DoesNotContain("SetProductAttributeRequest")` + `DoesNotContain("MapAttributeInvalid")` |

## Strengthened (not weakened)

| Guard | Added assertion |
|---|---|
| W8 Catalog Definitions endpoints | `DoesNotContain("MapAttributeInvalid")` (message-parsing already forbidden) |
| W9 Catalog Schema endpoints | `DoesNotContain("MapAttributeInvalid")` |
| W10 Catalog ProductValues endpoints | `DoesNotContain("MapAttributeInvalid")` |
| **New** `HostAdminAmcW10R1GuardTests` | Host file lacks both residuals; Seller owns DTO shape; migrated surfaces no message parsing; four Catalog routes once; Admin count 53 |

## Preserved prior-wave assertions

All useful W1–W10 inventory, CQRS, Result, authorization, Application foldering, and Host/Admin count=53 assertions remain.

## Intent

Guards now prove migrated Attribute surfaces do **not** depend on Host message-parsing helpers — they no longer pin a dead helper into production code.
