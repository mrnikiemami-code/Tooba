# Time and ID Closure

Not closed. The parent audit found direct `DateTimeOffset.UtcNow` and `UuidV7.New()` calls in Order infrastructure, including `CheckoutDirectory`. R1 did not alter Checkout semantics to mask this residual.
