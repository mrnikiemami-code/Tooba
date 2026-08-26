# 22 — PDP Fidelity Checklist

Task: `TB-P05-T017` / repair `TB-P05-T017-R1`

| Section | Grade | Notes |
| --- | --- | --- |
| top layout | MATCH | 3-col gallery / identity / offer |
| gallery | MATCH | mediaAssetIds + thumbs |
| purchase/offer area | MATCH | Offer-priced CTA; no Product.Price |
| variants | MATCH | live variant selection retained |
| tab strip | MATCH | Shopeiva `sticky top-0 z-20` strip; active underline; RTL; badge counts; Tooba accent `#2563EB` |
| sticky behavior | MINOR TECHNICAL DEVIATION | Source uses `sticky top-0` inside `overflow-hidden` (viewport stick disabled). Tooba keeps `sticky top-0 z-20` and omits card-level `overflow-hidden` so stick engages; pin-check: stripTop 174→0 after scroll |
| overview (معرفی اجمالی) | MINOR TECHNICAL DEVIATION | honest commerce trust tiles vs Shopeiva marketing lorem chips |
| detailed content (معرفی تکمیلی) | MATCH | distinct fullDescription body |
| specifications | MINOR TECHNICAL DEVIATION | dynamic Catalog attrs + Package icon vs fixed Shopeiva icon set |
| reviews | MATCH | live Reviews from T012 |
| Q&A | MINOR TECHNICAL DEVIATION | live ProductQnA; no likes/guest-name chrome from demo |
| wholesale | MINOR TECHNICAL DEVIATION | live BulkInquiry; **no** fake discount calculator (intentional) |
| other sellers | MATCH | Offer list in buy-box (Tooba-native; absent in Shopeiva) |
| related products | MATCH | category rail retained |
| mobile 390×844 | MATCH | responsive stack + horizontal tab overflow |
| accent | MINOR TECHNICAL DEVIATION | Tooba `#2563EB` vs Shopeiva red |
| click-to-scroll / scroll-spy | MATCH | Shopeiva source has tab state only (`setActiveTab`); no section scroll-spy invented |

No UNRESOLVED section for required tabs or sticky strip.
