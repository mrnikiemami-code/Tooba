import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { mapContentArticle, mapContentAuthorPublic, mapContentCategoryPublic } from "../content/content-api.ts";
import { localeToContentApi } from "../../lib/i18n/routing.ts";
import { blogsAuthorPath, blogsCategoryPath } from "./blogs-copy.ts";

test("published article API client passes locale query", () => {
  const source = readFileSync(new URL("../content/content-api.ts", import.meta.url), "utf8");
  assert.match(source, /params\.set\("locale", locale\)/);
  assert.match(source, /loadPublishedArticleBySlug\(slug: string, locale: string\)/);
  assert.match(source, /params\.set\("categorySlug", categorySlug\)/);
  assert.match(source, /params\.set\("authorSlug", authorSlug\)/);
  assert.match(source, /loadPublicCategory\(slug: string, locale: string\)/);
  assert.match(source, /loadPublicAuthor\(slug: string, locale: string\)/);
});

test("mapContentArticle maps canonicalPath, seo image, and taxonomy slugs", () => {
  const article = mapContentArticle({
    ArticleId: "a1",
    Slug: "guide",
    Title: "Guide",
    Excerpt: "x",
    Locale: "en-US",
    CanonicalPath: "/en/blogs/guide",
    SeoImageMediaAssetId: "11111111-1111-4111-8111-111111111111",
    PublishDate: "2026-08-20T00:00:00Z",
    AuthorDisplayName: "Ed",
    Tags: [],
    IsFeatured: false,
    CategoryId: "c1",
    AuthorId: "au1",
    CategorySlug: "guides",
    AuthorSlug: "ed",
  });
  assert.equal(article?.canonicalPath, "/en/blogs/guide");
  assert.equal(article?.seoImageMediaAssetId, "11111111-1111-4111-8111-111111111111");
  assert.equal(article?.categoryId, "c1");
  assert.equal(article?.authorId, "au1");
  assert.equal(article?.categorySlug, "guides");
  assert.equal(article?.authorSlug, "ed");
});

test("mapContentCategoryPublic and mapContentAuthorPublic map host payloads", () => {
  const category = mapContentCategoryPublic({
    CategoryId: "c1",
    LanguageCode: "fa-IR",
    Name: "راهنما",
    Slug: "guides",
    ShortDescription: "کوتاه",
    SeoTitle: "SEO",
    CanonicalPath: "/fa/blogs/category/guides",
  });
  assert.equal(category?.slug, "guides");
  assert.equal(category?.canonicalPath, "/fa/blogs/category/guides");

  const author = mapContentAuthorPublic({
    AuthorId: "a1",
    DisplayName: "Editorial",
    Slug: "editorial",
    ShortBio: "bio",
    CanonicalPath: "/en/blogs/author/editorial",
  });
  assert.equal(author?.displayName, "Editorial");
  assert.equal(author?.canonicalPath, "/en/blogs/author/editorial");
});

test("blog detail page requires locale-scoped lookup", () => {
  const page = readFileSync(new URL("./[slug]/page.tsx", import.meta.url), "utf8");
  assert.match(page, /localeToContentApi/);
  assert.match(page, /loadPublishedArticleBySlug\(slug, contentLocale\)/);
  assert.match(page, /notFound\(\)/);
});

test("category and author routes use /blogs taxonomy paths and notFound", () => {
  const categoryPage = readFileSync(new URL("./category/[slug]/page.tsx", import.meta.url), "utf8");
  const authorPage = readFileSync(new URL("./author/[slug]/page.tsx", import.meta.url), "utf8");
  assert.match(categoryPage, /loadPublicCategory/);
  assert.match(categoryPage, /notFound\(\)/);
  assert.match(authorPage, /loadPublicAuthor/);
  assert.match(authorPage, /notFound\(\)/);
  assert.equal(blogsCategoryPath("guides"), "/blogs/category/guides");
  assert.equal(blogsAuthorPath("editorial"), "/blogs/author/editorial");
});

test("locale routing maps fa/en to content API locales", () => {
  assert.equal(localeToContentApi("fa"), "fa-IR");
  assert.equal(localeToContentApi("en"), "en-US");
});

test("sitemap fetches articles and taxonomy per locale without fake hreflang pairs", () => {
  const source = readFileSync(new URL("../sitemap.ts", import.meta.url), "utf8");
  assert.match(source, /localeToContentApi\(locale\)/);
  assert.match(source, /canonicalPath/);
  assert.match(source, /loadPublicCategories/);
  assert.match(source, /loadPublicAuthors/);
  // مقالات و taxonomy فقط url می‌گیرند؛ بدون alternates ساختگی روی آن‌ها.
  assert.doesNotMatch(source, /articles[\s\S]*alternates:\s*\{[\s\S]*languages/);
  assert.match(source, /بدون hreflang ساختگی/);
});

test("home page passes contentLocale to loadStorefrontHome", () => {
  const home = readFileSync(new URL("../page.tsx", import.meta.url), "utf8");
  assert.match(home, /loadStorefrontHome\(contentLocale\)/);
});
