# 01 — Shopeiva PDP Inventory

Task: `TB-P05-T017`

Source root: `SarvNewVerRequirment/reference/shopeiva/src/`

| Section/Tab | Source | Structure | Tooba before | Gap | Action |
| --- | --- | --- | --- | --- | --- |
| Top buy-box | `singleProduct/productDetails/*` | 3-col gallery/identity/offer | LIVE | polish only | preserve |
| معرفی اجمالی | `productTabs/descriptionTab.jsx` | heading + text + feature grid | shortDescription paragraph | fidelity | LIVE structured intro |
| معرفی تکمیلی | same with `full` | distinct heading + long text | fullDescription paragraph | fidelity | LIVE structured full |
| مشخصات فنی | `specsTab.jsx` | icon cards 2-col | dl list | layout | LIVE attribute cards |
| نظرات | `reviewsTab` + reviews stack | stats + cards + form | LIVE T012 | — | LIVE |
| پرسش و پاسخ | `QATab.jsx` | list + form | none | backend | ProductQnA module LIVE |
| خرید عمده | `bulkOrderTab.jsx` | form + benefits | none | backend | BulkInquiry LIVE (no fake price) |
| Other sellers | absent in Shopeiva | — | buy-box LIVE | Tooba-only | keep |
| Related | `relatedProducts.jsx` | card rail | LIVE | — | keep |

Q&A and Wholesale exist as real Shopeiva tabs (demo data in source). Backend capabilities added in T017.
