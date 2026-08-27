# 06 — Admin shared Story management (TB-P06-T019-R1)

## Route

`/admin/stories` → `app/admin/stories/page.tsx` → `AdminStoriesScreen`.

## Thin wrapper

```tsx
export function AdminStoriesScreen() {
  return <StoryManagementScreen capabilities={ADMIN_STORY_CAPABILITIES} />;
}
```

(`admin-screens.tsx`)

## Admin capabilities (`ADMIN_STORY_CAPABILITIES`)

- Create / edit / publish / schedule / disable
- Review (approve / reject with reason)
- Show origin + seller owner columns
- No seller submit button (`canSubmit: false`)

## Baseline preserved

Existing admin Story list/editor geometry, copy language, and responsive layout are carried by the shared screen — not a redesign. Seller-review filters/actions land **inside** this same panel.
