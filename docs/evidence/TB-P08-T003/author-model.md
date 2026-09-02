# TB-P08-T003 — Author Model

- Entity: ContentAuthor (Content-owned public editorial identity)
- Table: content.authors; articles.author_id FK (nullable for legacy drafts)
- Fields: DisplayName, Slug (global unique), IsActive, bios, DAM image refs, social URLs
- Deactivate-only lifecycle; no hard delete when referenced
