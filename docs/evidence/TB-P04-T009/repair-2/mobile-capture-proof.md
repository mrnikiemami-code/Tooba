# TB-P04-T009 repair-2 — mobile capture proof

Capture method (not the Cursor browser-tool panel screenshot):

1. CDP `Emulation.setDeviceMetricsOverride` `{ width: 390, height: 844, deviceScaleFactor: 1, mobile: true }`
2. Remove `nextjs-portal` overlay nodes
3. CDP `Page.captureScreenshot` with `captureBeyondViewport: false` and `clip: { x: 0, y: 0, width: 390, height: 844, scale: 1 }`
4. Decode JSON `{ data: "<base64>" }` to PNG; verify IHDR

Live origin: `http://localhost:3000` (not `127.0.0.1`). Host: `http://localhost:5088`.

No frontend/backend source change. No debug overlay. No fixture cart. No secrets in files.

## Checkout (`01-checkout-mobile-390x844.png`)

Live URL: `http://localhost:3000/checkout` with the same device metrics as the PNG.

| Metric | Value |
| --- | --- |
| CSS viewport | 390 × 844 |
| deviceScaleFactor | 1 |
| window.devicePixelRatio | 1 |
| window.innerWidth | 390 |
| window.innerHeight | 844 |
| documentElement.clientWidth | 390 |
| documentElement.scrollWidth | 390 |
| documentElement.clientHeight | 844 |
| documentElement.scrollHeight | 3016 (vertical page scroll only) |
| horizontal overflow | no (`scrollWidth <= clientWidth`) |
| PNG IHDR | 390 × 844 |
| PNG bytes | 25343 |

## Confirmation (`02-order-confirmation-mobile-390x844.png`)

Live URL: `http://localhost:3000/order/confirmation?checkoutId=01a03537-1bb1-7000-8c92-3636dc3d855e`

Display ref: `TB-20260824191959-01-58f239`. Order state: **PendingPayment**. Copy states payment has not been made; this is not a paid-success page.

| Metric | Value |
| --- | --- |
| CSS viewport | 390 × 844 |
| deviceScaleFactor | 1 |
| window.devicePixelRatio | 1 |
| window.innerWidth | 390 |
| window.innerHeight | 844 |
| documentElement.clientWidth | 390 |
| documentElement.scrollWidth | 390 |
| documentElement.clientHeight | 844 |
| documentElement.scrollHeight | 2201 (vertical page scroll only) |
| horizontal overflow | no (`scrollWidth <= clientWidth`) |
| PNG IHDR | 390 × 844 |
| PNG bytes | 40554 |

## Validation error (`03-checkout-mobile-validation-error-390x844.png`)

Same `/checkout` session after empty shipping submit. Host message: «اطلاعات ارسال کامل نیست.»

| Metric | Value |
| --- | --- |
| CSS viewport | 390 × 844 |
| deviceScaleFactor | 1 |
| window.devicePixelRatio | 1 |
| window.innerWidth | 390 |
| window.innerHeight | 844 |
| documentElement.clientWidth | 390 |
| documentElement.scrollWidth | 390 |
| documentElement.clientHeight | 844 |
| documentElement.scrollHeight | 3016 (vertical page scroll only) |
| horizontal overflow | no (`scrollWidth <= clientWidth`) |
| PNG IHDR | 390 × 844 |
| PNG bytes | 24755 |

## Visual self-check

Inspected the three PNGs themselves. Each is a 390×844 crop of the live Shopeiva RTL page filling the viewport. No wide white canvas with a thin left strip.
