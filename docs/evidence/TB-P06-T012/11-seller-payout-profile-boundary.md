# 11 — Seller Payout Profile Boundary

Task: `TB-P06-T012`

No bank/IBAN fields embedded in Settlement domain for T012 minimum. Payout destination profile deferred to future PaymentProfile module. UI does not collect card/Sheba (removed from Shopeiva wallet port). Production payout remains fail-closed until provider + verified profile exist.
