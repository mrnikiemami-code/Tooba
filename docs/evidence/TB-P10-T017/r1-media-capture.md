# TB-P10-T017-R1 — Media capture

Capture waits for `[data-storefront-media-well] img` and PDP gallery `img` load/error (timeout 4s in the capture script only), then `scrollTo(0,0)` for a first-viewport clip. No product-code sleep.

| Clip | File | Truth |
| --- | --- | --- |
| PLP PaletteTint | `screenshots/r1/plp-tinted.png` | `<img>` still in `storefront-product-card.tsx`. Capture measured naturalWidth=150 on Host media. 1440×1200 first viewport still reads as empty product-card photo chrome (wells collapse vs PDP gallery). |
| PDP PaletteTint | `screenshots/r1/pdp-tinted.png` | Gallery shows Tooba placeholder; buy box + variants visible. Status 200. |
| Landing PaletteTint | `screenshots/r1/landing-tinted.png` | Hero Digital Gold + category kettle render. Campaign product cards match PLP empty photo chrome. |
| Dark PDP / Landing | `dark-pdp-tinted.png` / `dark-landing-tinted.png` | Same media truth on DarkOnly + `html.dark`. |

Not a deleted image tag. Not a four-role architecture change.
