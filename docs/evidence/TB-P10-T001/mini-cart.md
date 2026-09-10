# TB-P10-T001 — Mini-cart

Implemented in `storefront-mini-cart.tsx`, opened from header bag button.

Preserved Shopeiva shape:

- Overlay `bg-black/50 backdrop-blur-sm`
- Left drawer `max-w-sm`
- Header with count + close
- Line cards: image / title / Host line price / − qty + / remove
- Footer total from Host `subtotalExclusiveOfTax`
- CTA تکمیل خرید → `/cart` (not shipping)

Theme: Tooba blue `#2563EB` (not template red).
Data: `loadStorefrontCart` / `changeCartLineQuantity` / `removeCartLine`.
