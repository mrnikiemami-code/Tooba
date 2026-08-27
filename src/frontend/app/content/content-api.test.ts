import assert from "node:assert/strict";
import test from "node:test";
import { formatContentDate, mapAdminContentArticle, mapContentArticle } from "./content-api.ts";

test("mapContentArticle maps host PascalCase published payload", () => {
  const article = mapContentArticle({
    ArticleId: "a1",
    Slug: "guide",
    Title: "راهنما",
    Excerpt: "چکیده",
    Body: "بدنه",
    CoverMediaAssetId: null,
    PublishDate: "2026-08-20T00:00:00Z",
    AuthorDisplayName: "تحریریه",
    Tags: ["راهنما"],
    IsFeatured: true,
    SeoTitle: "SEO",
    SeoDescription: "desc",
    Category: "خرید",
    Locale: "fa-IR",
  });
  assert.ok(article);
  assert.equal(article?.slug, "guide");
  assert.equal(article?.category, "خرید");
  assert.equal(article?.body, "بدنه");
});

test("mapAdminContentArticle includes status", () => {
  const article = mapAdminContentArticle({
    articleId: "a1",
    slug: "x",
    title: "t",
    excerpt: "e",
    body: "b",
    locale: "fa-IR",
    status: "Draft",
    authorDisplayName: "a",
    tags: [],
    isFeatured: false,
    publishDate: "2026-08-20T00:00:00Z",
    createdAt: "2026-08-20T00:00:00Z",
    updatedAt: "2026-08-20T00:00:00Z",
  });
  assert.equal(article?.status, "Draft");
  assert.equal(article?.id, "a1");
});

test("formatContentDate localizes", () => {
  assert.notEqual(formatContentDate("2026-08-20T00:00:00Z"), "—");
});
