# TB-P10-T005 — Default visual equivalence

Default palette `tooba-blue` RGB equals the accepted accent `#2563EB` and hover `#1d4ed8`.

Assertions: `src/frontend/lib/storefront-appearance/palette-registry.test.ts`.

Shopeiva red `#E53935` is not reintroduced. Home/PDP hex left in place so locked geometry/color does not drift. Shared chrome now reads the same RGB through CSS variables, so the default Storefront remains visually equivalent.
