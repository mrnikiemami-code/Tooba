# recovery-start — TB-TMAR-NEXT-MODULE-BATCH-005-R1

- Task-ID: TB-TMAR-NEXT-MODULE-BATCH-005-R1
- Parent: TB-TMAR-NEXT-MODULE-BATCH-005
- Claim: 3d2cb11f-0928-4b89-93e2-75510effbfe5
- Channel: tooba-main
- WorkerId: tooba-worker-01
- Bridge: http://127.0.0.1:17321
- Baseline HEAD: 4b342f62ccfab689b6123004083a5559150dc213
- HEAD == origin/main at claim: YES
- Mode: FAST-SAFE / REFERENCE_BATCH_REPAIR
- Scope: Settlement Host thin transport + Cart/Settlement architecture guards ONLY
- Out of scope: Tax/Pricing/Checkout resume/frontend/BATCH-006

## Claim status at start

- Bridge `/api/tasks` shows claim status=Claimed for R1
- Stashes preserved: stash@{0} unrelated-pre-r10; stash@{1} temp-before-push
- Untracked at start: docs/ai/tasks/TB-TMAR-NEXT-MODULE-BATCH-005-R1.task.md only

## Blocking residuals from parent BATCH-005

1. Host SettlementPanelComposer injects SettlementDbContext + PartyDbContext
2. Host AdminPayoutGridQueryEngine owns DB-native Settlement query
3. Host SettlementEndpoints manual Results.Json / ex.Message
4. Dedicated Cart.Tests / Settlement.Tests ArchitectureGuard projects missing
