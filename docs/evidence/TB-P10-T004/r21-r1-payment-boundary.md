# TB-P10-T004-R21-R1 — Payment Boundary

External initiate is `StorefrontPaymentComposer.InitiateAsync` after checkout exists. It does not call `SubmitAsync`.

Runtime: wallet initiate after commit returned `400 payment.wallet.mixed_deferred`; checkout `01a0994e-9f80-7000-a615-710dc04b1f4f` stayed `PendingPayment`; Cart stayed Converted.
