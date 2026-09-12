# Checkout races

Stock lost before commit → 409 `checkout.inventory.unavailable`; no checkout row. Two buyers / last unit: unique checkout + ReserveAsync; loser gets shortage; stock never oversells. Same-cart retry uses unique(cart_id). PRICE_CHANGED preserved before/without a leftover hold.
