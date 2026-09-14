# TB-P10-T009-R1 — Runtime A–H

Host :5088 + FE :3000. Script `r1-capture.mjs`. Raw: `r1-runtime-raw.json`.

| Step | Appearance | Result |
| --- | --- | --- |
| A | classic + tooba-blue + LightOnly | PASS + `classic.png` |
| B | clean (Admin save) | PASS + `clean.png` |
| C | elevated | PASS + `elevated.png` |
| D | glass | PASS + `glass.png` |
| E | glass + DarkOnly | PASS + `skin-dark.png` |
| F | elevated + forest-green; glass + DarkOnly + forest-green | PASS |
| G | clean on Home/PLP/PDP-related/cart recs | PASS |
| H | restore classic / tooba-blue / LightOnly | PASS + `classic-restored.png` |

API save count: 9 (8 Host PUTs + 1 Admin UI save).
SSR markers matched requested skin. Invalid PUT 400. Ending appearance: tooba-blue / LightOnly / classic.
Wishlist skipped (auth).
