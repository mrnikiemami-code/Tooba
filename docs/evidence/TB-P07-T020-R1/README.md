# TB-P07-T020-R1 evidence

## Goal

Consistent Product Workspace unsaved-changes guard so Attributes / Variants / SEO (and Media alt drafts when dirty) cannot silently lose edits on tab switch or exit EDIT.

`USER_VISUAL_ACCEPTED` = **NO**

## Behavior matrix

| Section | Local dirty? | Registered? | Tab switch / exit-edit | Notes |
|---|---|---|---|---|
| **General** | `formMode.isDirty` + draft | Yes (`general`) | Dialog → discard resets draft + `clearDirty`; exit-edit then leaves EDIT | Covered by parent registration |
| **Translations** | No | No | N/A | VIEW-only; fa-IR identity edits live in General / SEO |
| **Attributes** | `dirty` flag | Yes (`attributes`) when `dirty && editable` | Dialog → `discardDrafts` from last loaded schema values | In-panel انصراف discards without `window.confirm` |
| **Variants** | `showDirty` (`dirty \|\| axisDirty \|\| rowDirty`) | Yes (`variants`) when editable | Dialog → reload axis/row drafts from state | In-panel انصراف discards without `window.confirm` |
| **Media** | `altDirty` (`isAltDraftDirty`) | Yes (`media`) when `altDirty && editable` | Dialog → reset `altDrafts` from items | Attach/reorder/primary/remove are immediate mutations; only unsaved alt text is guarded |
| **SEO** | `isSeoDraftDirty` | Yes (`seo`) when `dirty && editable` | Dialog → `cancelEdit()` | Locale switch inside panel still uses `window.confirm` (R1); tab switch uses workspace Dialog |
| **Publishing** | No draft dirty | No | N/A | Status actions are confirm/immediate; tab links go through `requestSectionChange` |
| **History** | No | No | N/A | Read-only |

## Wiring

- `product-workspace-dirty.ts` — pure `createProductWorkspaceDirtyRegistry`
- `product-workspace-dirty-context.tsx` — `ProductWorkspaceDirtyProvider` + `useProductWorkspaceDirtyRegistration`
- `product-workspace-screen.tsx` — provider, `pendingNav`, design-system `Dialog` (`product-workspace-unsaved-*`), `beforeunload`, route leave after delete guarded
- Panels register discard callbacks; Save paths clear dirty (`setDirty(false)` / reload / draft from server)

## Explicit non-claims

- No AppDataGrid / Category Tree redesign
- No new domain features (DAM, offers, prices, stock)
- No visual ACCEPT
