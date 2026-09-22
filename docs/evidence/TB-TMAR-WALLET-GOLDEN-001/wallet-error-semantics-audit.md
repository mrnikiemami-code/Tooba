# Wallet Error Semantics Audit

Public codes: customer.session.required, wallet.rejected, wallet.redeem.rejected, giftcard.rejected, giftcard.issue.rejected, giftcard.revoke.rejected, giftcard.missing, wallet.missing, wallet.adjust.rejected, admin.authorization.denied, wallet.demo.not_ready

WalletExceptionMapper: exact known machine codes only; unknowns rethrow; no Contains/StartsWith prose heuristics; no ex.Message leakage in Endpoints.
