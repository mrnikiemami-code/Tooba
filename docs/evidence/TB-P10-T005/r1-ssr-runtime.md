# TB-P10-T005-R1 — SSR runtime

FE `:3000` (`TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`) against live Host `:5088`.

A. Successful API path — first HTML of `/fa`:

```html
<html … data-storefront-palette="tooba-blue" data-storefront-scope="tenant:store-alpha"
  style="--color-primary:37 99 235;--color-primary-strong:29 78 216;…">
```

`data-storefront-scope=tenant:store-alpha` cannot come from the fallback (`storeScope=default`). Loader `loadStorefrontAppearance()` with Host `:5088` returns the same live projection.

B. Safe fallback — Host origin `127.0.0.1:59999` refused; contract returns default `tooba-blue` tokens without throw. Not used on the live pages above.

No client-only theme patch, no forced reload, no FOUC (vars on first HTML). `suppressHydrationWarning` unchanged; no hydration error text in HTML.
