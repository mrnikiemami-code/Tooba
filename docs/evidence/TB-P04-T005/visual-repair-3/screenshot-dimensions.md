# TB-P04-T005 visual repair 3 — PNG dimensions

Measured with `System.Drawing.Image` on disk. Capture method: CDP `Emulation.setDeviceMetricsOverride` plus `Page.captureScreenshot` clip matching the claimed CSS viewport (`deviceScaleFactor` 1).

| file | claimed viewport | PNG width | PNG height | verdict |
| --- | --- | --- | --- | --- |
| 01-list-1440x900-rtl-light.png | 1440×900 | 1440 | 900 | PASS |
| 02-overview-1440x900-rtl-light.png | 1440×900 | 1440 | 900 | PASS |
| 03-variants-1440x900-rtl-light.png | 1440×900 | 1440 | 900 | PASS |
| 04-commercial-1440x900-rtl-light.png | 1440×900 | 1440 | 900 | PASS |
| 05-inventory-1440x900-rtl-light.png | 1440×900 | 1440 | 900 | PASS |
| 06-seo-content-1440x900-rtl-light.png | 1440×900 | 1440 | 900 | PASS |
| 07-publication-1440x900-rtl-light.png | 1440×900 | 1440 | 900 | PASS |
| 08-mobile-overview-390x844-rtl-light.png | 390×844 | 390 | 844 | PASS |
| 09-mobile-commercial-390x844-rtl-light.png | 390×844 | 390 | 844 | PASS |
| 10-ltr-1440x900-light.png | 1440×900 | 1440 | 900 | PASS |
| 11-dark-1440x900-rtl.png | 1440×900 | 1440 | 900 | PASS |
| 12-conflict-1440x900-rtl.png | 1440×900 | 1440 | 900 | PASS |

Mobile PNGs are 390px wide (not a letterboxed desktop frame). `forceNarrow` is not used for these captures.
