# Proposal

## Why

Stage 1 made editing an existing seller offer safe. Stage 2 must make offer creation usable from both the existing admin shell and the existing seller shell, without inventing a second owner or losing the first durable Offer when price or stock is incomplete.

## What Changes

- Add admin-managed creation for a chosen seller and published Catalog variant.
- Keep seller self-creation on the same Offer aggregate and identity.
- Make creation resumable through an Offer-owned deterministic request identity; retry returns the first Offer instead of posting a second one.
- Timeout/price/stock failure leaves a truthful Draft and follows only remaining steps.
- Replace the newest-100-product picker with server search and paging over published variants.
- Replace seller/admin offer full-list loading with server search, sorting and bounded paging.
- Enforce view/create/edit capabilities on the server for admin and seller commands.
- Reuse AdminShell/VendorShell and existing design-system controls; document the Impeccable critique and post-check. Persian and English, RTL/LTR, and 360x640 remain required.

## Non-goals

- No Stage 3 shipping-service selection, promotion discounts, order/cart/payment/settlement flow, migration, or live commercial seed.
- No parallel shell, no Catalog mutation, and no admin impersonation via seller headers.
