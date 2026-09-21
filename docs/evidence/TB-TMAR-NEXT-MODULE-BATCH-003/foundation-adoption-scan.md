# Foundation adoption scan — TB-TMAR-NEXT-MODULE-BATCH-003

| Pattern | Notification | Support |
|---|---|---|
| IClock / IIdGenerator | YES (Directory) | YES (Directory) |
| No UtcNow/Guid.NewGuid/UuidV7.New in production | YES | YES |
| No DI fallbacks (`?? new SystemUtcClock`) | YES | YES |
| Stable exception codes | YES | YES |
| Contracts-only cross-module | YES | YES (Notification) |
| Physical folders + namespace align | YES | YES |
| TypeForwardedTo | 0 | 0 |
| Result/SemanticError second model | not introduced | not introduced |
| MediatR ceremonial | not added | not added |
