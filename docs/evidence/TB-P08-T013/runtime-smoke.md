# TB-P08-T013 — Runtime smoke

## Status

**PASS (API + page)** after Host restart with T013 bits/migrations.

## Checks

| Check | Result |
|-------|--------|
| Host `/health` | 200 |
| Create Category L1 (fa) | 200 (`id`) |
| Create Category L2 under L1 | 200 |
| Create Category L3 under L2 | **rejected** `content.category.max_depth_exceeded` |
| Category tree fa | 200 |
| Create ContentTag fa | 200 |
| Assign Tag to Article | 200 |
| List Article Tags | 200 (assigned present) |
| FE `/admin/content` | 200 |
| Focused BE tests (agent) | 9/9 |
| FE/recovery guards (agent) | pass |

## Notes

Interactive browser picker/chips UI not fully automated; source wiring + Admin API smoke cover enforcement. `USER_VISUAL_ACCEPTED=NO`.
