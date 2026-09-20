# Frontend dependency overlap — TB-TMAR-FE-BASELINE

Source: `package.json` + real import scan (`frontend-dependency-imports.json`).

| Package | Import sites | Files | Recommendation |
| --- | ---: | ---: | --- |
| `antd` | 1 | 1 | KEEP_NARROW (category tree only) / avoid expansion |
| `swiper` | 10 | 5 | CONSOLIDATE_LATER (dual carousels) |
| `embla-carousel-react` | 1 | 1 | CONSOLIDATE_LATER (dual carousels) |
| `embla-carousel-autoplay` | 1 | 1 | CONSOLIDATE_LATER (dual carousels) |
| `@ckeditor/ckeditor5-react` | 1 | 1 | CONSOLIDATE_LATER (dual editors; owners differ: articles vs product) |
| `ckeditor5` | 2 | 1 | CONSOLIDATE_LATER (dual editors; owners differ: articles vs product) |
| `@tiptap/react` | 1 | 1 | CONSOLIDATE_LATER (dual editors; owners differ: articles vs product) |
| `@tiptap/starter-kit` | 1 | 1 | CONSOLIDATE_LATER (dual editors; owners differ: articles vs product) |
| `ag-grid-react` | 1 | 1 | KEEP (admin grids) |
| `ag-grid-community` | 18 | 17 | KEEP (admin grids) |
| `lucide-react` | 93 | 93 | KEEP |
| `react-toastify` | 33 | 33 | KEEP |
| `react-hook-form` | 3 | 3 | KEEP |
| `zod` | 4 | 4 | KEEP |
| `dayjs` | 3 | 3 | KEEP |
| `jalaliday` | 2 | 2 | KEEP |
| `next` | 189 | 145 | KEEP |
| `isomorphic-dompurify` | 2 | 2 | KEEP |

## Notes

- **CKEditor** (articles) vs **TipTap** (product rich text): different surfaces; consolidate only with product decision.
- **Swiper** (home/blogs/stories) vs **Embla** (product showcase rails): dual carousels — FE-F6 candidate.
- **antd**: single AppCategoryTree usage — do not expand as general UI kit.
- No uninstalls in this task.
