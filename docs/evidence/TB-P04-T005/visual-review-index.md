# TB-P04-T005 visual review index

Functional PASS is not Visual ACCEPT.

Screenshots were captured from the running Next production server at `http://localhost:3012` (browser tool). They are not generated placeholders.

| File | What was on screen | Honest gap |
| --- | --- | --- |
| list-desktop-rtl.png | `/admin/products` DataGrid + fixture source banner | Host was not running; list is the contract fixture |
| workspace-overview-desktop-rtl.png | Overview identity; Price/Stock not on Product | — |
| workspace-variants.png | Variants tab (fingerprint + offer count) | Capture raced with Commercial in one take; Architect should confirm the file shows the intended tab |
| workspace-commercial-multi-seller.png | Multi-seller offers/prices/tax | Same capture race as Inventory; file may show Inventory table. Do not treat as faked pixels; treat as possible tab mismatch |
| workspace-inventory.png | Offer-scoped OnHand/Reserved/Available | Copied from the Inventory-visible capture |
| workspace-seo.png | Slug seam + Semantic Content != Page Composition | — |
| workspace-mobile.png | `forceNarrow` compact shell | Native dialog unsaved-guard caveat remains DEFERRED_NON_BLOCKING |
| workspace-ltr.png | Direction toggle LTR | — |
| workspace-dark.png | Color scheme dark | — |
| workspace-readonly-or-conflict.png | Stale-save UI demonstration + later `?scope=view` read-only (Save/Publish disabled) | Conflict banner is UI demonstration when Host is down; Host did not return HTTP 409 in this capture |

Cursor PASS != Architect visual ACCEPT.
