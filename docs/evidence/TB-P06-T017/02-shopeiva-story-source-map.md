# 02 — Shopeiva story source map (TB-P06-T017)

## Important

Shopeiva story sources are **NOT in the Tooba monorepo** (`SarvNewVer`). They live in the external FrontStarter / reference tree used for visual/interaction parity.

## Canonical external paths

| Role | Path (outside monorepo) |
|---|---|
| Story rail | `FrontStarter/shopeiva/src/components/home/stories/stories.jsx` |
| Story modal | `FrontStarter/shopeiva/src/components/home/stories/storyModal/storyModal.jsx` |
| Add-story modal (reference only) | `FrontStarter/shopeiva/src/components/home/stories/addStoryModal/addStoryModal.jsx` |

Sibling reference checkout used in this workspace (same files, not vendored into Tooba):

- `../SarvNewVerRequirment/reference/shopeiva/src/components/home/stories/stories.jsx`
- `../SarvNewVerRequirment/reference/shopeiva/src/components/home/stories/storyModal/storyModal.jsx`
- `../SarvNewVerRequirment/reference/shopeiva/src/components/home/stories/addStoryModal/addStoryModal.jsx`

## Tooba port targets (in monorepo)

| Shopeiva | Tooba |
|---|---|
| `stories.jsx` | `src/frontend/app/storefront/stories/home-stories.tsx` |
| `storyModal.jsx` | `src/frontend/app/storefront/stories/story-modal.tsx` |
| `addStoryModal.jsx` | **Omitted intentionally** (admin creates stories) |

## Mapping rule

- Port **exact** visual/interaction chrome (accent `#E53935`, Swiper rail, modal progress/tap RTL).
- Replace demo/static arrays with live Host `GET /v1/storefront/stories`.
- Do not invent a new story UX or customer-created AddStory flow.
