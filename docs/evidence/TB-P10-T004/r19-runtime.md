# R19 runtime A–R

In-memory Host + projector/directory/resolver proofs. Server clock `2026-09-13T10:00:00Z`.

| Case | Result |
| --- | --- |
| A Standard online success | PASS CommittedPaid; pending excluded |
| B Fail then retry same active cycle | PASS ExpiresAt unchanged; CTA pay |
| C Expiry → reacquire → Cycle #2 | PASS Retry TTL |
| D Unavailable after expiry | PASS no new numbered cycle |
| E Max cycles | PASS created>=max; no CTA |
| F Flash-sale mixed-policy | PASS 3m then 1m; no #3 |
| G Manual awaiting review | PASS no pay-again |
| H Confirm + active review | PASS same cycle → Succeeded path |
| I Confirm after expired review + reacquire | PASS Ensure then confirm |
| J Confirm after expired review + unavailable | PASS not Succeeded |
| K Guest ownership | PASS committed proof only |
| L Auth ownership | PASS PlacedByUserId |
| M New Active Cart + old pending | PASS both surfaces |
| N Multi-seller payable/shipping | PASS R11 |
| O Decimal 1.25 | PASS |
| P Admin audit/history/settings | PASS R17/R18 |
| Q No polling/N+1 | PASS source + grid batch |
| R Settings future-only | PASS active ExpiresAt frozen |

No TB-P10-T005.
