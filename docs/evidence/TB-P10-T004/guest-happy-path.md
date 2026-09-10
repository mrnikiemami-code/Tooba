# B — Guest happy path
Runtime PASS: cart create → lines → shipping projection/selection/commit → manual payment.
payable == payment amount; FE /fa/cart|/shipping|/payment 200; no CVV/cardNumber fields in HTML.
