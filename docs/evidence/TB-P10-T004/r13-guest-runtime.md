# TB-P10-T004-R13 — Guest runtime

Host `http://127.0.0.1:5098` (rebuilt R13), `Host: alpha.localhost`, raw `r13-runtime-raw.json` ok=true.

| Step | Result |
| --- | --- |
| Order commit | 200 checkout `01a096ea-b433-7000-955d-07c77a252981` |
| Source cart | `01a096ea-b1c8-7000-a40f-627597619cdb` Status=Converted |
| Payment page GET + source secret | 200 |
| Fresh Active cart | `01a096ea-b5df-7000-82ab-d41c9b9bad04` ≠ source |
| AddToCart on fresh | 200 |
| Initiate + source secret | 200 payment `73ae63e5-3abe-4ade-82e8-baa3d607b549` |
| Result GET + source secret | 200 |
| Sandbox context + success | 200 / 200 |
| New secret checkout GET | 403 `checkout.access.denied` — not «ثبت سفارش انجام نشد» |
| New secret initiate | 403 `payment.access.denied` |
| Id-alone checkout GET | 403 `checkout.access.denied` |
| Converted mutation | 409 (rejected; not Active) |
| Replay commit | 400 `checkout.rejected` (no second Order) |
