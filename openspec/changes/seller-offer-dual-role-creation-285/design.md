# Design

## Decisions

1. **Admin authority is explicit.** Admin create/read/edit uses the existing admin panel gate plus existing `product.create`, `product.view`, or `product.edit` capability. Seller id is body input for admin create; the admin actor is never replaced with seller headers.
2. **Seller write authority is explicit.** Seller list/detail keep the existing party view gate. Create requires `product.create`; patch, price and inventory require `product.edit`. Seller A cannot use seller B's id because the party member check is server-side.
3. **Creation is Draft-first.** Admin and seller create a Draft Offer. Price and stock remain Pricing/Inventory commands. No status becomes Active automatically.
4. **Idempotent creation uses a client request id.** The Offer handler derives a stable OfferId from seller + catalog variant + request id. A retry with the same intent returns the stored Offer (after verifying the same seller/variant); a changed intent fails closed. No schema change.
5. **Resume is authoritative-state driven.** After the Offer exists, the page checks the returned detail: skip a price already matching, skip inventory already matching, write only missing parts. A local token survives refresh; after completion it is removed.
6. **Catalog picker is server-owned.** Published products and variants are searched and paged in SQL. Search matches Catalog code or localized product name. An old item beyond the newest 100 remains discoverable.
7. **Offer lists are server-owned.** Both roles query Offer through a bounded page/count query with stable ordering. The browser does not load the full array.
8. **No schema change.** Existing unique seller/variant/channel index and PK provide identity. The create idempotency key is encoded in the stable OfferId; the list queries use existing columns. No migration is planned.

## Lifecycle

Create (Draft) -> optional Pricing set -> optional Inventory set -> seller/admin can reopen. A partial failure leaves a Draft with the same OfferId and an explicit incomplete notice. Cancel before the first send writes nothing. Controlled response loss only tests error branches after a real first write.

## Roles and languages

Admin view-only may list/read but cannot call create/patch/price/inventory. Seller A and B have separate seller scopes. The UI uses AdminShell/VendorShell and the existing language context; fa and en labels come from page-local dictionaries, not a new language engine.
