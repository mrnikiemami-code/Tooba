# TB-P08-T016 — Category / Tags / Author

## Code / contract (worker)

- Categories Admin: AppCategoryTree, maxDepth=2
- Article picker: `ContentArticleCategoryPicker` hierarchical searchable — not flat ComboBox / `content-article-category-select`
- Tags: `ContentArticleTagsPanel` chips/search/create/remove — no TagsCsv textbox UI
- Authors: global searchable picker; Draft create without Author; Publish readiness may require Author
- FE taxonomy-tags + author tests pass; BE ContentCategory*/ContentTaxonomyTags pass

## Browser (parent)

- [ ] Tree UX; Level 2 cannot add child
- [ ] Main + subcategory selectable with parent context; language scoped
- [ ] Tags chips persist after reload
- [ ] Author picker human labels; no raw IDs
