# Idempotency — TB-P07-T034

- Product identity: `demo-prod-{sanitizedL3Key}-{n}`
- Existing slug → skip recreate (no duplicate products/tags/media/variants)
- Reset+seed clears seam products then reseeds to same counts
- Live first run: products=283; replay documented in `live-seed-replay.json`
