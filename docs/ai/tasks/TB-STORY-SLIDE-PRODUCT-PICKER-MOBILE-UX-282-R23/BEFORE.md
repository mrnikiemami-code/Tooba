# Before-fix observation

Source snapshot: `1d20601cb` (plan commit; code still at the reviewed baseline).

The product option button had no selected identity beyond its row position:

```tsx
<button
  type="button"
  className="flex min-h-11 w-full items-center px-3 py-2 text-right text-sm hover:bg-secondary"
  data-testid="story-product-option"
  data-product-id={row.productId}
  disabled={productLoading}
  onClick={...}
>
  {row.productTitle}
</button>
```

The selected product was a muted caption rendered after the results and load-more control:

```tsx
{slideCtaTarget ? (
  <p className="text-xs text-muted" data-testid="story-product-selected">
    {copy.productSelectedPrefix} {selectedProductLabel || copy.productSelectedFallback}
  </p>
) : null}
```

Observed consequence: no `aria-pressed`, no check icon, no selected text badge, and the current product's identity was below the list rather than beside the top of the picker. This is the source-level counterexample for the changed presentation; the browser suite is the after-fix proof.
