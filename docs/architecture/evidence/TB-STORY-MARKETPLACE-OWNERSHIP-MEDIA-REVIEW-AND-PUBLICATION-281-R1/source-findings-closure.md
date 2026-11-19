# TB-281-R1 B1–B7 → source + proof

| Gate | Source | Proof | State |
| --- | --- | --- | --- |
| B1 public attach-before-filter | `StoryDirectory.GetPublicStoriesAsync` | `StoryMarketplace281R1DirectoryTests.Fresh_materialized_*` | PASS |
| B2 Party/Media fail-closed | `EnsureShopEligibleAsync`, `BindSellerMediaAsync` + error codes | R1 missing-media fact; party gate on admin-for-shop | PARTIAL (create cover bind / admin upload open) |
| B3 upload occurrence+digest+compensate | `MediaDirectory.Story.UploadForSellerAsync` + IntentHash migration | code + migration apply; no recording-store suite | PARTIAL |
| B4 all-path versions | Directory enable/disable/approve/reject/schedule/update + FE bodies | AMSC validator pins 23 required; FE call sites | PARTIAL (seller create/add/submit occurrence open) |
| B5 role actions | `MapAdmin(..., isAdmin)` + grid | R1 seller_actions_exclude_admin | PASS |
| B6 query/CTA/public identity | — | — | OPEN |
| B7 Media CQRS/tenant | — | — | OPEN |
