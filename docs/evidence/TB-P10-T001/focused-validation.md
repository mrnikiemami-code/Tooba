# TB-P10-T001 — Focused validation

## Frontend

```text
node --test app/storefront/storefront-cart-api.test.ts app/storefront/storefront-cart-ui.guard.test.ts
```

(cwd: `src/frontend`)

Expected coverage:

- cart mapper ignores product price fields
- session bootstrap
- customer message sanitization
- decimal quantity policy fields
- product-card ATC wired + toast + blue accent
- header opens mini-cart
- mini-cart Host APIs + CTA `/cart`
- cart recommendations + honest shipping copy

## Backend

Existing Cart foundation suites remain authoritative (not re-run whole Host suite unless needed):

- `CartFoundationTests`
- `StorefrontCompositionTests` cart JSON contract

## Recovery

```text
node docs/ai/recovery-staleness.guard.test.mjs
```
