# TB-P10-T006 — Live Storefront apply

FE `:3000` (`TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`) after Admin PUT, no rebuild.

After forest-green:

```html
data-storefront-palette="forest-green"
data-storefront-scope="tenant:store-alpha"
style="…--color-primary:21 128 61;…"
```

Tokenized surfaces (header `bg-primary`, product-card ATC/price, mini-cart, cart checkout CTA) consume those CSS variables. Home/PDP/Shipping leftover hexes stay deferred (see inventory).

`--color-danger` is not written on `<html>` (status colors remain `globals.css`: danger `185 28 28`, success `21 128 61`, warning `180 83 9`).

After restore:

```html
data-storefront-palette="tooba-blue"
--color-primary:37 99 235
```

No client-only recolor, no FOUC, no hydration mismatch text. `suppressHydrationWarning` unchanged.
