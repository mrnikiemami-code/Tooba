# ARCHITECT REVIEW — TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1

**Verdict: ACCEPTED AS DATA-INTEGRITY RECOVERY; BLOCKED AS DELIVERY.**

The compensation is supported by owning-API and database receipts: the four guessed assignments, unit
reinterpretation, category bindings, three 1.5 values and the two invented adjacent rate rows were removed; the
original [1000,2000) row remains; the cart is Active and no checkout/payment was created. The current projection
honestly refuses all four unassigned offers. This is the correct safe state and must be preserved.

Delivery cannot be accepted because the merchant has not supplied:

- responsibility and shipping type for each offer;
- packaged per-unit weight for Huawei, Samsung and Xiaomi products;
- rate coverage/amounts for any resulting group total outside [1000,2000) grams.

No implementation defect has been established in this compensated state and no automatic next task is authorized.
The next execution may be issued only after these merchant decisions are provided.

Evidence correction is also required before any later acceptance: RESULT identifies the actual user as
`01a0996c-b8d9-7000-9854-80b3f59e8d7c`, while RECEIPTS identifies
`01a0996c-1b0e-7000-a8bd-5fd78f9b9e4f`. The worker must resolve this from the real authenticated session and
correct the documentary record; it must not infer that the two identities are equivalent.

Two accepted groups from the rejected 244 snapshot remain on disk but are fail-closed: projection is unassigned and
payment-draft returns `storefront.captured_selection.unavailable`. A later lawful selection must atomically replace
them; they may not be reused as authority.
