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
    authorId: "auth-1",
    categoryId: "cat-1",
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
  assert.equal(article?.authorId, "auth-1");
  assert.equal(article?.categoryId, "cat-1");
});

test("article locale is independent per row — no translation sibling required", () => {
  const fa = mapAdminContentArticle({
    articleId: "a-fa",
    slug: "iran-economy",
    title: "اقتصاد ایران",
    excerpt: "e",
    body: "b",
    locale: "fa-IR",
    status: "Published",
    authorDisplayName: "a",
    tags: [],
    isFeatured: false,
    publishDate: "2026-08-20T00:00:00Z",
    createdAt: "2026-08-20T00:00:00Z",
    updatedAt: "2026-08-20T00:00:00Z",
  });
  const en = mapAdminContentArticle({
    articleId: "a-en",
    slug: "unrelated-topic",
    title: "Unrelated topic",
    excerpt: "e",
    body: "b",
    locale: "en-US",
    status: "Draft",
    authorDisplayName: "a",
    tags: [],
    isFeatured: false,
    publishDate: "2026-08-20T00:00:00Z",
    createdAt: "2026-08-20T00:00:00Z",
    updatedAt: "2026-08-20T00:00:00Z",
  });
  assert.equal(fa?.locale, "fa-IR");
  assert.equal(en?.locale, "en-US");
  assert.equal(fa?.status, "Published");
  assert.equal(en?.status, "Draft");
  assert.notEqual(fa?.slug, en?.slug);
});

test("formatContentDate localizes", () => {
  assert.notEqual(formatContentDate("2026-08-20T00:00:00Z"), "—");
});
