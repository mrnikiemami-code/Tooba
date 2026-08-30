# TB-P07-T035 — Save regression (live)

Product: 01a05229-4211-7000-9048-43d8fd5998ff

| Mutation | Result |
| --- | --- |
| PATCH catalog-title + expectedUpdatedAt | 200; restored to پاوربانک نسخه 3 |
| PUT brand Apple then null | 200; brandless restored |
| PUT media primary cycle | 200; primary restored |
| Missing expectedUpdatedAt | 409 stale (designed) |

Evidence: live-save-regression.json (primaryCycleOk=true, restoredPrimary=true)

## Conclusion
Representative mutations persist; seeded values restored; no publish.
