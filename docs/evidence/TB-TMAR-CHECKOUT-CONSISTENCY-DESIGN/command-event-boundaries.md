# Command/event boundaries

- **ReserveInventory** (SYNC_COMMAND) owner=Inventory producer=CheckoutPM consumer=Inventory corr=checkoutId idemp=cc-cart-line
- **ReleaseInventory** (SYNC_COMMAND) owner=Inventory producer=CheckoutPM consumer=Inventory corr=checkoutId idemp=reservationId
- **CreateCheckoutOrder** (SYNC_COMMAND) owner=Order producer=CheckoutPM consumer=Order corr=checkoutId idemp=IdempotencyKey
- **ConvertCart** (SYNC_COMMAND) owner=Cart producer=CheckoutPM consumer=Cart corr=cartId idemp=cartId+version
- **CancelUnpaidCheckout** (SYNC_COMMAND) owner=Order producer=CheckoutPM/Host consumer=Order corr=checkoutId idemp=checkoutId+cancel
- **AuthorizePayment** (SYNC_COMMAND) owner=Payment producer=Host/Payment consumer=Payment corr=checkoutId idemp=attemptId
- **CheckoutCompleted** (INTEGRATION_EVENT) owner=Order/Payment producer=Outbox consumer=Fulfillment/Settlement corr=checkoutId idemp=inbox
- **CheckoutFailed** (INTEGRATION_EVENT) owner=Order producer=Outbox consumer=observers corr=checkoutId idemp=inbox
- **GetCart** (SYNC_QUERY) owner=Cart producer=CheckoutPM consumer=Cart corr=cartId idemp=n/a
- **LookupOffer/Price/Tax/Promo** (SYNC_QUERY) owner=Offer/Pricing/Tax/Promotion producer=CheckoutPM consumer=gateways corr=checkoutId idemp=n/a