import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { mapContentArticle } from "../content/content-api.ts";
import { localeToContentApi } from "../../lib/i18n/routing.ts";

test("published article API client passes locale query", () => {
  const source = readFileSync(new URL("../content/content-api.ts", import.meta.url), "utf8");
  assert.match(source, /params\.set\("locale", locale\)/);
  assert.match(source, /loadPublishedArticleBySlug\(slug: string, locale: string\)/);
});

test("mapContentArticle maps canonicalPath and seo image", () => {
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
  });
  assert.equal(article?.canonicalPath, "/en/blogs/guide");
  assert.equal(article?.seoImageMediaAssetId, "11111111-1111-4111-8111-111111111111");
});

test("blog detail page requires locale-scoped lookup", () => {
  const page = readFileSync(new URL("./[slug]/page.tsx", import.meta.url), "utf8");
  assert.match(page, /localeToContentApi/);
  assert.match(page, /loadPublishedArticleBySlug\(slug, contentLocale\)/);
  assert.match(page, /notFound\(\)/);
});

test("locale routing maps fa/en to content API locales", () => {
  assert.equal(localeToContentApi("fa"), "fa-IR");
  assert.equal(localeToContentApi("en"), "en-US");
});

test("sitemap fetches articles per locale without fake hreflang pairs", () => {
  const source = readFileSync(new URL("../sitemap.ts", import.meta.url), "utf8");
  assert.match(source, /localeToContentApi\(locale\)/);
  assert.match(source, /canonicalPath/);
  assert.doesNotMatch(source, /for \(const locale of LOCALES\) \{[\s\S]*articlesPayload[\s\S]*for \(const locale of LOCALES\)/);
});
