# Host authority audit — BATCH-005

## Cart
- No Cart business/query engine ownership added in Host for this batch.
- Production CartDbContext use remains bootstrap/migrate/dev seed pattern.

## Settlement
| Surface | Authority | Required | Actual |
|---------|-----------|----------|--------|
| SettlementPanelComposer | module directory + Host grid | thin transport | THICK — injects SettlementDbContext |
| AdminPayoutGridQueryEngine | should be Settlement.Infrastructure | Host.Grid | HOST-OWNED |
| SettlementEndpoints | ApiResponseFactory / Result | thin | manual Results.Json + ex.Message |
| PartyDbContext seller names | Party.Contracts port | Host direct | HOST PartyDbContext |

## Verdict
Settlement Host Db/query authority: NOT NONE → blocks COMPLETE_REFERENCE_PATTERN for Settlement endpoints.
