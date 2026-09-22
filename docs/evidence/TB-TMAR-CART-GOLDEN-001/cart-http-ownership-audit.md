# cart-http-ownership-audit

Cart-HTTP-Ownership: MODULE_ENDPOINTS
- Tooba.Cart.Endpoints created; MapCartEndpoints() under /v1/storefront
- Exact URLs preserved (POST/GET cart, current, merge, lines CRUD)
- Host StorefrontEndpoints no longer maps Cart routes
- Host Program calls app.MapCartEndpoints()
