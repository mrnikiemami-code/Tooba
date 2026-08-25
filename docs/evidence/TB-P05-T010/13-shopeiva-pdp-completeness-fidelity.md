# Shopeiva PDP completeness fidelity

Canonical visual baseline:

- accepted TB-P04-T007 Shopeiva PDP adaptation;
- accepted TB-P05-T007 live purchase experience;
- locked three-column desktop PDP and single-column mobile stack.

## Preserved structure

| Shopeiva region | Structural result | Live binding |
| --- | --- | --- |
| Header / breadcrumb | unchanged | Catalog category and title |
| Gallery | unchanged main image + thumbnails | every available Catalog media reference |
| Identity column | unchanged title hierarchy and spacing | Catalog title, brand, category, distinct short description |
| Options area | minimum Shopeiva-compatible chips inside existing identity column | Catalog variant axes; shown only when capability exists |
| Buy box | unchanged cards, quantity, price, CTA rhythm | selected Offer, Pricing, Inventory, seller, tax, market, Promotion |
| Other sellers | unchanged buy-box subsection | other active sellers for the selected variant |
| Tabs | existing intro/full/specifications/reviews tabs retained | distinct Catalog content, real attributes, honest Review absence |
| Related rail | unchanged Shopeiva product-card grid | backend-selected real products |
| Mobile | existing stack, wrapping options, horizontally scrollable tabs | same live contracts at 390x844 |

## Approved minimum addition

The variant chips are the only structural addition. It is authorized because:

1. Catalog already owns real variants and variant-axis attributes;
2. the accepted PDP had no visible option selector;
3. sellability requires the chosen option to resolve the correct Offer;
4. the addition sits in the existing identity/options position and reuses
   Shopeiva border, radius, spacing, typography, and blue active state.

No new design language, page shell, card system, or route was introduced.

## Removed fabricated signals

- static PDP `4.5` and star icon removed;
- fixed product-card stars removed, including related products;
- static key-feature business claims removed;
- reviews tab retained but states that capability is unavailable;
- JSON-LD contains no AggregateRating or review count.

## Commerce fidelity

Variant selection calls Host and replaces the complete detail projection.
Frontend does not infer a price, inventory, seller, valid combination, or
promotion. Add-to-cart uses the returned selected `OfferId`.

```text
Product != Offer != Price != Inventory
```

## Visual evidence

All desktop images 02–10 are `1440x900`. Mobile image 11 is `390x844`.
The mobile viewport has no horizontal page overflow; gallery, option chip,
identity, and buy flow remain in the locked vertical order. Tabs remain usable
through their existing horizontal strip.
