# Order commit boundary

UI action: Storefront «ادامه / ثبت ارسال» → `POST /v1/storefront/shipping/commit` → `CheckoutDirectory.SubmitAsync`.

This already created the payable Order before R9. Moving Order creation would destabilize Payment. Shipping commit remains the hard-reservation start. Atomicity: unique(cart_id) winner only then reserves; failure deletes the unsold checkout and releases acquired holds. Preview and opening /shipping do not reserve.
