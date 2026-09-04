# Focused validation (TB-P08-T016-R5)

## Commands

```text
cd src/frontend
node --test app/admin/content-article-admin-screen.test.ts app/admin/content-taxonomy-tags.test.ts app/admin/content-category-admin-screen.test.ts app/admin/content-article-media.test.ts app/admin/content-list.test.ts
node --test ../../docs/ai/recovery-staleness.guard.test.mjs
```

## Results

- FE focused contracts: **25/25 pass**
- Recovery staleness guard: **3/3 pass**
- USER_VISUAL_ACCEPTED=NO
