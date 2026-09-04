# TB-P08-T013 — Two-level rule

- Backend `ContentCategoryTreeRules.MaxDepth = 2` (root=1, subcategory=2).
- Create under Level 2 → `content.category.max_depth_exceeded`.
- Move that would produce depth >2 (incl. moving L1+child under L1) rejected.
- Parent/child same Language; Article may assign L1 or L2; mismatch → `content.category.language_mismatch`.
- Inactive/Archived not newly assignable → `content.category.inactive`.
