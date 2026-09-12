# Performance

Add/GET do not write inventory reservations. Cart GET uses `GetAvailabilityBatchAsync`. Checkout reserves after unique insert; availability batched per commit. No reservation-renewal polling.
