# 29 — Original vs Tooba Reference Map

Task: `TB-P05-T017-R1`

Method: live purchased Shopeiva Next app on `http://127.0.0.1:3017` + Tooba storefront on `http://127.0.0.1:3000`. Capture via `scripts/capture-t017-r1-evidence.mjs`.

Shopeiva source component: `SarvNewVerRequirment/reference/shopeiva/src/components/singleProduct/productTabs/productTabs.jsx` (`sticky top-0 z-20`, six tabs, active border, count badges).

| Original PNG | Tooba equivalent | Source / structure | Intentional deviations | Unresolved |
| --- | --- | --- | --- | --- |
| `02-original-shopeiva-pdp-top.png` | `11-tooba-pdp-top-1440x900.png` | PDP top 3-col + tab strip | Tooba blue accent; Offer-priced buy box | none |
| `03-original-shopeiva-tab-overview.png` | `12-tooba-tab-overview.png` | Overview tab body | trust tiles vs marketing chips | none |
| `04-original-shopeiva-tab-details.png` | `13-tooba-tab-details.png` | Full description tab | live catalog copy | none |
| `05-original-shopeiva-tab-specifications.png` | `14-tooba-tab-specifications.png` + `27-sticky-tab-active-section.png` | Specs tab | dynamic attrs | none |
| `06-original-shopeiva-tab-reviews.png` | `15-tooba-tab-reviews.png` | Reviews tab | live Reviews module | none |
| `07-original-shopeiva-tab-qa.png` | `16-tooba-tab-qa.png` | Q&A tab | live ProductQnA; badge count | none |
| `08-original-shopeiva-tab-wholesale.png` | `17-tooba-tab-wholesale.png` | Wholesale tab | BulkInquiry form; no fake calculator | none |
| (sticky desktop) | `26-sticky-tab-desktop.png` | sticky strip below site header | overflow-hidden omitted so sticky works | none |
| (sticky active) | `27-sticky-tab-active-section.png` | active specs + strip | same | none |
| (sticky mobile) | `28-sticky-tab-mobile.png` | mobile overflow-x tabs | same | none |

Recreated-by-eye references: **none** (02–08 are live Shopeiva PNGs; prior markdown absence proofs removed).
