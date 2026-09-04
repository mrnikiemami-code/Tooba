# TB-P08-T016 — Category / Tags / Author

## Code / contract (worker)

- Categories Admin: AppCategoryTree, maxDepth=2
- Article picker: `ContentArticleCategoryPicker` hierarchical searchable — not flat ComboBox / `content-article-category-select`
- Tags: `ContentArticleTagsPanel` chips/search/create/remove — no TagsCsv textbox UI
- Authors: global searchable picker; Draft create without Author; Publish readiness may require Author
- FE taxonomy-tags + author tests pass; BE ContentCategory*/ContentTaxonomyTags pass

## Browser (parent)

- [ ] Tree UX; Level 2 cannot add child  
  Categories Admin tree depth rule not exercised this session.
- [x] Main + subcategory selectable with parent context; language scoped  
  Observed: hierarchical searchable picker with `parent › child` labels; selected **موبایل**.
- [ ] Tags chips persist after reload  
  Not exercised this session.
- [ ] Author picker human labels; no raw IDs  
  Not fully browser-verified. API smoke: authors/picker returns **400** without `activeOnly` query param (requires `activeOnly`).
