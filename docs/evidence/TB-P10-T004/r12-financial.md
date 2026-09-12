# TB-P10-T004-R12 — Financial regression

Runtime J: payable 2217588 = seller 2017588 + StoreShipping 200000. Seller settlement-visible = seller alloc only.

R11 invariant `CheckoutPayableInvariant` still used by Paid projection. Duplicate success inbox stays 1 (R11 G). Refund/return history not touched in R12.
