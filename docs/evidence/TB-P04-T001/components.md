# TB-P04-T001 — Component inventory (grouped)

Inspected `src/components` (323 files). Many are page-local sections, not a design system.

## Primitives

Tailwind utility compositions; no shared Button/Input package. Class: ADAPT into Design System primitives later.

## Navigation

`common/Header`, `common/Footer`/`DynamicFooter`, breadcrumb helper. Class: ADAPT.

## Commerce cards / product media / price display

Home/listing grids, Swiper product rails, PDP gallery. Price is a number on mock product. Class: ADAPT chrome, REBUILD data binding.

## Filters

Category/search/sale filter bars. Class: ADAPT.

## Forms

Login/register/forgot, shipping, payment, vendor product forms; RHF+zod where present. Class: ADAPT.

## Tables

`CompareTable`, best-seller breakdown `<table>`, vendor dashboard `<table>`. Class: REBUILD vs Data Grid.

## Feedback

`react-toastify`, empty cart, error copy ad-hoc. Class: REPLACE notifications; ADAPT empty states.

## Dialogs/drawers

Story modal; search overlay `w-80` from `right-0`. Class: ADAPT with a11y rebuild.

## Account

`user-panel` widgets, orders, addresses, wishlist. Class: ADAPT IA, REBUILD data.

## Dashboard / charts

Vendor dashboard + `chart.js`. Class: DEFER charts; REBUILD ops dashboard.

## Content blocks

Blogs, magazine, static heroes. Class: ADAPT.

## Utilities

Zustand stores, axios, Fuse.js, fake `setTimeout` loaders. Class: DROP as commerce truth; DEFER zustand for UI-only.

## Skeletons

Large `skeleton/*` tree (Home, PDP, Cart, Payment, Vendor, …). Class: REUSE.
