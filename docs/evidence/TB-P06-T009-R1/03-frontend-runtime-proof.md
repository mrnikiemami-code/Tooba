# 03 — Frontend runtime proof (TB-P06-T009-R1)

| URL | Method | Status | Interpretation |
|-----|--------|--------|----------------|
| `http://127.0.0.1:3000/` | GET | 200 | Home OK |
| `http://127.0.0.1:3000/products/demo-book-1` | GET | 200 | PDP OK |
| `http://127.0.0.1:3000/customer-panel` | GET | 200 | Customer area reachable |
| `http://127.0.0.1:3000/vendor-panel` | GET | 200 | Seller area reachable |
| `http://127.0.0.1:3000/admin` | GET | 200 | Admin area reachable |

No missing-route 404 on probed panel/storefront surfaces.
