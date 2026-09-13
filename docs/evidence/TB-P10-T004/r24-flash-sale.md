# TB-P10-T004-R24 — Flash-sale cross-check

Offer `47801c5c-7490-4782-912f-5c6cd5f3f879` reservation policy PUT 3/1/2 (T-flash-policy 200). Store open-unpaid max 2; churn 30m/3.

Runtime: first Cycle #1 consumes churn; payment fail/retry does not reset timer or add event; cancel frees open slot and keeps churn; replacement consumes next event; repeated cancel/recreate hits `checkout.reservation_commit_limit_reached` before reserve (H-cart-intact res unchanged); hide cannot bypass open-unpaid (D-still-blocked).
