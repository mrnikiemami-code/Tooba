# 07 — Seller shared Story management (TB-P06-T019-R1)

## Route

`/vendor-panel/stories` → `app/vendor-panel/stories/page.tsx`

Nav: `vendor-shell.tsx` item `stories` → `/vendor-panel/stories` (`live: true`). Integrity covered in `panel-nav-integrity.test.ts`.

## Thin wrapper

```tsx
export default function VendorStoriesPage() {
  return <StoryManagementScreen capabilities={SELLER_STORY_CAPABILITIES} />;
}
```

## Seller capabilities

- Create draft, edit when None/Rejected, manage items/media/CTA fields via shared form
- Submit for review; see rejection reason; resubmit
- **No** approve / reject / publish / schedule / disable UI
- Data from seller-scoped APIs only (`listSellerStories`, etc.)

## No CSS fork

Same `StoryManagementScreen` + shared copy; no seller-specific stylesheet or second editor implementation.
