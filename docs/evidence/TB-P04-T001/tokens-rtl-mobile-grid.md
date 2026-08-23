# TB-P04-T001 — Tokens, RTL, mobile, grid, commerce notes

## Tokens (raw)

`--color-theme: #E53935`; zinc/white dark pair; IRANSansXNoEn locals; `max-w-[1800px]`; `px-4 sm:px-6`; Swiper 3500–5000ms; z sticky `lg:top-24`.

Candidate Tooba tokens are listed in `docs/architecture/51-shopeiva-study-reuse-map.md`. Not a final Design System.

## RTL

Root `dir=rtl`. Swiper `dir="rtl"` hard-coded. LTR islands for phone/email/tracking. Search drawer `right-0` is RTL-native and LTR-unsafe. CSS direction is not bilingual proof.

## Mobile

Carousels `slidesPerView: 2.2` / auto. Cart stacks then `lg:grid-cols-3`. Vendor tables `text-[10px]` desktop-first. Header mega-nav is a mobile risk.

## Grid

REBUILD. Three HTML tables only. No mandatory Tooba grid capabilities.

## Commerce mapping

Product JSON collapses Product/Offer/Price/Stock. No variant, multi-seller offer, tax, reserve vs purchase, or payment verification. Cart Zustand is UX-only.
