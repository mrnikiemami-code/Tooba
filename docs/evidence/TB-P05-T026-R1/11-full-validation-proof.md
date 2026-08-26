# 11 — Full validation proof (TB-P05-T026-R1)

## Backend

Helper: `scripts/run-backend-validation-with-nuget-proxy.ps1`  
Log: `11-full-backend-validation.log`

| Metric | Result |
|---|---|
| restore | PASS |
| build warnings | **0** |
| build errors | **0** |
| NU1900 | **0** |
| tests passed | **205** |
| failed | **0** |
| skipped | **0** |

## Frontend

Log: `11-full-frontend-validation.log`

| Suite | Result |
|---|---|
| typecheck | PASS |
| lint | PASS (no ESLint warnings/errors) |
| test:critical-storefront (home/pdp/listing guards) | PASS |
| test:storefront (includes cart/checkout/payment mappers) | PASS |
| test:customer | PASS |
| test:seller | PASS |
| test:admin | PASS |
| test:grid | PASS |
| test:workspace | PASS |
| test:product-workspace | PASS |
| build (`next build`) | PASS |

Note: `test:listing` / `test:cart` / `test:checkout` are not separate npm scripts; listing guard is in critical-storefront; cart/checkout covered by `test:storefront`.

## Post-build dev recovery

After `next build`, `next dev` Home returned HTTP 500 (known `.next` conflict). Recovery: stop dev → clear `.next` → restart `npm run dev -- --hostname 127.0.0.1 --port 3000` → Home/PDP verified 200 again.

**Full validation: PASS**
