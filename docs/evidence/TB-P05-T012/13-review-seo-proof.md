# Review SEO proof

Captured from the running Next.js Development server at
`http://127.0.0.1:3000`; the values originate in the live Host product
composition.

## Published-review product

Command:

```powershell
$html = (Invoke-WebRequest http://127.0.0.1:3000/products/workspace-live-shirt).Content
[regex]::Matches($html, '<script type="application/ld\+json">(.*?)</script>') |
  ForEach-Object { $_.Groups[1].Value }
```

Observed Product JSON-LD excerpt:

```json
{
  "@type": "Product",
  "name": "پیراهن مردانه لینن",
  "aggregateRating": {
    "@type": "AggregateRating",
    "ratingValue": 4,
    "reviewCount": 3
  }
}
```

These values match both the live public Reviews API and the visible PDP/product
card: Published ratings 5, 4, and 3 produce average 4 and count 3.

## Zero-review product

Command:

```powershell
$html = (Invoke-WebRequest http://127.0.0.1:3000/products/demo-mobile-1).Content
$html.Contains('aggregateRating')
```

Observed result: `False`.

The zero-review PDP therefore emits no `AggregateRating`; it does not invent a
zero score, fake count, or sample review. The conditional is implemented in
`src/frontend/app/storefront/storefront-product-seo.ts` and requires
`reviewCount > 0` plus a non-null rating within 1–5.
