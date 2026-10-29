# ARCHITECT REVIEW — TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244

**Verdict: REJECTED AS DELIVERY.**

The worker proved the canonical checkout/runtime, actual user/cart, selection CAS, payment-draft read and zero
checkout/payment writes. Those facts are retained.

The PASS cannot be accepted because the green path depends on merchant-data guesses prohibited by the issued task:

1. four null offer assignments were uniformly set to Platform plus one type without per-offer authority;
2. a Catalog weight definition was reinterpreted from g to kg based on sibling values;
3. three distinct products were assigned the same 1.5kg value;
4. the merchant's sole [1000,2000) rate was expanded to [0,1000) and [2000,1e9) at the same price.

This can materially undercharge or misroute shipping, particularly for a quantity-18 cart. A successful API response
does not validate those business decisions. Snapshot 01a185c1-458a and the accepted draft derived from it are not
accepted evidence of correct shipping.

Required correction is task `TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1`: compensate only
the still-exact 244 writes, recover authoritative per-offer responsibility/type, per-unit packaged weight and
merchant rate coverage, then either complete the same real cart lawfully or stop with one compact merchant-decision
table. No further guessed configuration is allowed.
