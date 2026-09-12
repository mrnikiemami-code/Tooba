# Historical cart reservations

Old `cart:{id}` Held rows are not extended. They expire via `ReleaseExpiredHoldsAsync` or are released on line remove/abandon/expire if still on the cart and not bound to an Order. Checkout reuses a still-valid historical hold instead of double-reserving. Order-bound reservations are never swept as cart leftovers.
