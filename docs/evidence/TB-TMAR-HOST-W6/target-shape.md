# Target shape — TB-TMAR-HOST-W6

Host endpoint/composer → ISender → Command → Fulfillment.Application Handler → IShippingServiceDirectory → ShippingServiceDirectory (Infrastructure) → FulfillmentDbContext

Language gate: IShippingServiceLanguageGate (Host adapter) — no Fulfillment→Localization App edge.
Host retains: auth, DTO mapping, read composition (List/Get/tree), response shape.
