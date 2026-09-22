# Promotion error semantics audit
`PromotionExceptionMapper` performs exact ordinal machine-code lookup only. Unknown exceptions propagate and HTTP does not expose exception messages.
