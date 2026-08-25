# Shopeiva review fidelity

Compared against the purchased Shopeiva review sources recorded in
`02-shopeiva-review-backend-convergence.md`.

## Preserved visual grammar

- PDP keeps Shopeiva's existing review tab rather than adding a parallel page.
- The summary uses the score, five-star display, review count, and five
  distribution rows from `reviewStats.jsx`.
- Published items preserve the author/date/stars/title/body hierarchy from
  `reviewItem.jsx`.
- The form preserves selectable stars, optional title, body, and primary submit
  action from `reviewForm.jsx`.
- Desktop evidence is 1440×900 and mobile evidence is exactly 390×844; the
  mobile stack remains readable without replacing the Shopeiva shell.
- The Admin moderation surface adapts the Shopeiva vendor review-list intent to
  Tooba's already accepted Admin shell and Data Grid.

## Intentional Tooba convergence

- Persian RTL content and Tooba blue action tokens are retained.
- Public score/count/distribution and cards come from the Reviews API.
- Product-card stars render only where the live Published count is non-zero.
- Submission reports Pending moderation rather than presenting immediate
  publication.
- Admin actions call server-authorized publish/reject endpoints.

## Intentionally omitted, not simulated

- sample reviews and vendor `reviewsData`;
- fake `rating=4.5` / `reviews=120` product-card defaults;
- client-created verified badges;
- avatars, media, likes/helpful counts, replies, and seller responses;
- client-side aggregate authority;
- Published-looking Pending or Rejected content.

## Evidence correspondence

- `03`: live average 4, count 3, and 1–5 distribution.
- `04`: the three Published API rows; the Pending row is absent.
- `05`: Shopeiva-derived submission form.
- `06`: real zero-review Catalog product and honest empty state.
- `07`: real Pending moderation queue.
- `08`: real server denial for a non-member actor.
- `09`: the live review summary/cards at 390×844.
- `10`: live product-card rating 4 with count 3.

No screenshot payload was mocked or edited. The screenshots were captured from
the running Development Host/Next application and deterministic database seed.

## Normal-browser same-origin verification

The evidence was recaptured in standard headless Chrome with no
`--disable-web-security` or other browser security bypass. In the browser,
`readJson` requests the relative `/v1/...` URL; Next then forwards it through
the configured same-origin rewrite to the Development Host. SSR continues to
use the direct Host origin.

Playwright observed successful browser responses including:

```text
http://127.0.0.1:3000/v1/storefront/products/workspace-live-shirt/reviews?page=1&pageSize=10 → 200
http://127.0.0.1:3000/v1/storefront/products/demo-mobile-1/reviews?page=1&pageSize=10 → 200
```

Published review text was awaited before capture on desktop and mobile, and the
real zero-review message was awaited for `demo-mobile-1`. The prior CORS
capture blocker is resolved.
