# Historical Audit
Candidates: Class A (manual Pending+evidence + Released), Class B (Succeeded + Released), Class C ambiguous.
Scan: GET `/v1/admin/orders/inventory-recovery/audit` (bounded, no mutation).
Exclude: cancelled, refunded, rejected-without-claim, fully fulfilled.
