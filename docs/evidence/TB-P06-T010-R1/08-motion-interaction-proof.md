# 08 — Motion / interaction proof (TB-P06-T010-R1)

| Interaction | Evidence | Mechanism |
| --- | --- | --- |
| Seller grid link hover | `18-tooba-seller-fulfillments-hover.png` | CDP `mouseover` on grid link before screenshot |
| Shipment row hover | CSS `transition-colors hover:bg-gray-100` in `fulfillment-ui.tsx` | class parity with Shopeiva product rows |
| Seller mutation buttons | disabled state while `busy !== null` | prevents double-submit; opacity transition |
| Input focus | `focus:ring-2 focus:ring-primary/30` on tracking/carrier fields | focus-visible parity |

No carousel/expand-collapse in fulfillment surfaces (none in Shopeiva source for this scope).

Not claimed from class names alone — hover capture `18` included.
