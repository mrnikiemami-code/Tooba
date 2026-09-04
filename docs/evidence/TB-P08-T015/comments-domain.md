# Comments domain (TB-P08-T015)

## Entity

`Tooba.Content.Domain.ArticleComment`

| Field | Notes |
|-------|--------|
| CommentId | UUID v7 |
| ArticleId | owning article |
| AuthorPartyId | optional opaque contract ref (no cross-module ORM) |
| DisplayName | snapshot |
| Body | text |
| Status | Pending / Approved / Rejected / Hidden |
| CreatedAt | UTC |
| ModeratedAt / ModeratedByUserId / ModerationNote | audit |

## Persistence

- Table `content.article_comments`
- Migration `20260904110000_AddArticleComments`
- Cascade with article; **no hard-delete of moderation history** (status changes only)

## Directory

`IArticleCommentDirectory` / `ArticleCommentDirectory` — article-scoped list/create/moderate.
