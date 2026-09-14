# Runtime — TB-P10-T015

Host :5088 + FE :3000. Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`. Capture: `docs/evidence/TB-P10-T015/capture.mjs`. Report: `runtime-report.json` ok=true.

| Step | Result |
| --- | --- |
| A Appearance open | PASS |
| B default Neutral UI | PASS |
| C PaletteTint + tooba-blue | PASS |
| D Home Neutral then tinted blue | PASS (distinct screenshots) |
| E Landing inherits tint | PASS `landing-campaign` |
| F/G forest-green + PaletteTint | PASS |
| H DarkOnly tinted | PASS |
| I/J restore Neutral + tooba-blue + LightOnly + classic + canonical Home | PASS |

Ending appearance: tooba-blue / LightOnly / classic / Neutral.

Screenshots: `docs/evidence/TB-P10-T015/screenshots/`

No render-loop. 401 `/api/auth/me` is anonymous storefront. Hydration mismatch text is pre-existing theme bootstrap, not BackgroundStyle-owned.
