# Admin migration pattern — TB-TMAR-FE-ADMIN-W1

Proven again:

1. Self-contained feature API on `lib/admin/admin-result`
2. Feature screen under `features/<cap>/components`
3. Thin App Router page via public `index.ts`
4. Remove capability from `admin-api` + flat/god host file
5. Shrink FE-FOLDER-002 export baseline
6. Characterization + architecture guards

Next candidates: catalog-units (already separate API), shipping-services, reviews (still in admin-screens).

Pattern status: **SAFE_TO_CONTINUE** → TB-TMAR-FE-ADMIN-W2
