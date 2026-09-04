# focused-validation.md

## Commands

- `dotnet test` filter `ContentArticleEditorTests|ContentAuthorDirectoryTests`
- `node --test` on content-list / content-article-admin-screen / content-author-admin-screen / content-article-crud
- `node docs/ai/recovery-staleness.guard.test.mjs`
- `git diff --check`

## Results

- BE: Passed 2 / Failed 0 / Skipped 0 (ContentArticleEditorTests + ContentAuthorDirectoryTests)
- FE source-assert: 19 pass / 0 fail
- recovery-staleness.guard: 3 pass / 0 fail
- `git diff --check`: clean (CRLF warnings only)
