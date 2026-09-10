# Payment pricing integrity
Checkout payableAmount (merchandise + shipping) is Host-authoritative.
Payment initiate amount now equals checkout payable (shipping included via OrderPaymentBridge.ShippingAmount).
Client amount field ignored.
Evidence: runtime B/F amount == payable.
