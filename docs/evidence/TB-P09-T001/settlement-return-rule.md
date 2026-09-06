# Settlement–return rule (TB-P09-T001)

Seller settlement completion does **not** end customer return rights.
Eligibility ignores settlement entirely.
Post-settlement refund continues to post Debit via existing `AdjustFromRefundAsync` without rewriting/closing statements.
