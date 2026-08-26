# 16 — Admin operational workflow map (TB-P05-T024)

```text
Catalog ops
  /admin/products (list Grid)
    → /admin/products/[id] Product Workspace
         → core product / variants / offers / pricing / inventory / media / SEO
         (UI composition via Host contracts; backend modules remain separate)

Order ops
  /admin/orders → /admin/orders/[checkoutId]
         → payment state + shipping snapshot + seller slices

Marketplace visibility
  /admin/sellers
  /admin/customers (checkout-derived)

Moderation
  /admin/reviews → publish/reject via Host

System
  /admin/settings → honest unavailable
```

No cross-module SQL. No fake fulfillment/mutation UIs.
