# Order result/error audit

This pass removed message-based checkout conflict classification from `CheckoutProcessManager`: Order persistence now throws the typed `CheckoutConflictException`. Reservation release failure is no longer silently swallowed; it is logged with checkout identity and rethrows the original submit failure.

Residuals remain: Host admin endpoints expose `ex.Message`, and other Order Infrastructure paths still map exception text or use localized exception identity. Canonical `Result` plus central HTTP presentation is therefore incomplete and belongs in R1.
