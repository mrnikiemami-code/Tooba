import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const dir = import.meta.dirname;
const screen = readFileSync(join(dir, "content-article-admin-screen.tsx"), "utf8");
const commentsPanel = readFileSync(join(dir, "content-article-comments-panel.tsx"), "utf8");
const commentsApi = readFileSync(join(dir, "content-article-comments-api.ts"), "utf8");
const helpContent = readFileSync(join(dir, "content-help-content.ts"), "utf8");
const helpAffordance = readFileSync(join(dir, "content-help-affordance.tsx"), "utf8");
const helpPage = readFileSync(join(dir, "content-help-page-screen.tsx"), "utf8");
const helpRoute = readFileSync(join(dir, "content/help/page.tsx"), "utf8");
const mediaPanel = readFileSync(join(dir, "content-article-media-panel.tsx"), "utf8");
const errorMap = readFileSync(join(dir, "admin-error-map.ts"), "utf8");
const publication = readFileSync(join(dir, "content-article-publication.test.ts"), "utf8");
const ck = readFileSync(join(dir, "content-article-ckeditor.tsx"), "utf8");

test("comments tab and moderation panel are native to article workspace", () => {
  assert.match(screen, /content-article-tab-comments|id: "comments"/);
  assert.match(screen, /ContentArticleCommentsPanel/);
  assert.match(commentsPanel, /loadArticleComments/);
  assert.match(commentsPanel, /approveArticleComment/);
  assert.match(commentsPanel, /rejectArticleComment/);
  assert.match(commentsPanel, /hideArticleComment/);
  assert.match(commentsPanel, /content-article-comments-empty/);
  assert.match(commentsPanel, /content-article-comments-loading/);
  assert.match(commentsPanel, /content-article-comments-error/);
  assert.match(commentsApi, /\/v1\/admin\/content\/articles\/.*\/comments/);
  assert.doesNotMatch(commentsPanel, /AgGridReact/);
});

test("contextual help affordance and central help page cover required topics", () => {
  assert.match(helpAffordance, /ContentHelpAffordance/);
  assert.match(helpAffordance, /CONTENT_HELP_PAGE_HREF/);
  assert.match(helpPage, /content-help-page/);
  assert.match(helpRoute, /ContentHelpPageScreen/);
  assert.match(helpContent, /language/);
  assert.match(helpContent, /draftPublished/);
  assert.match(helpContent, /author/);
  assert.match(helpContent, /category/);
  assert.match(helpContent, /tags/);
  assert.match(helpContent, /featuredImage/);
  assert.match(helpContent, /galleryMedia/);
  assert.match(helpContent, /seoSocial/);
  assert.match(helpContent, /readiness/);
  assert.match(helpContent, /preview/);
  assert.match(helpContent, /publishSchedule/);
  assert.match(helpContent, /unpublishRepublish/);
  assert.match(helpContent, /history/);
  assert.match(helpContent, /comments/);
  assert.match(screen, /ContentHelpAffordance/);
  assert.match(screen, /content-article-help-link/);
});

test("SEO Media Home wording is human and avoids DAM acronym in normal UI", () => {
  assert.match(screen, /نمایش در بخش مقالات صفحه اصلی/);
  assert.doesNotMatch(screen, /ویژه در ریل خانه/);
  assert.match(screen, /جستجو و اشتراک/);
  assert.match(screen, /عنوان نمایش در نتایج جستجو/);
  assert.match(screen, /توضیح کوتاه برای نتایج جستجو/);
  assert.match(screen, /تصویر اشتراک‌گذاری/);
  assert.match(mediaPanel, /تصویر شاخص/);
  assert.match(mediaPanel, /گالری مقاله/);
  assert.match(mediaPanel, /کتابخانهٔ رسانه|کتابخانه رسانه/);
  assert.doesNotMatch(mediaPanel, /\bDAM\b/);
  assert.doesNotMatch(screen, /\bDAM\b/);
});

test("workspace preserves CKEditor readiness preview history category tags", () => {
  assert.match(screen, /ContentArticleEditor/);
  assert.match(screen, /ContentArticleReadinessSummary/);
  assert.match(screen, /content-article-preview/);
  assert.match(screen, /ContentArticleHistoryTimeline/);
  assert.match(screen, /ContentArticleCategoryPicker/);
  assert.match(screen, /ContentArticleTagsPanel/);
  assert.match(ck, /ClassicEditor/);
  assert.match(publication, /readiness|publish|history/i);
  assert.match(screen, /content-article-primary-actions/);
  assert.match(screen, /content-article-destructive-actions/);
});

test("comment moderation errors map to human messages", () => {
  assert.match(errorMap, /content\.comment\.invalid_transition/);
  assert.match(errorMap, /content\.comment\.not_found/);
  assert.match(errorMap, /content\.comment\.forbidden/);
  assert.match(errorMap, /content\.comment\.article_not_found/);
});

test("no public article comment form invented", () => {
  const blogDetail = join(dir, "../blogs/[slug]/blog-detail-ui.tsx");
  assert.ok(existsSync(blogDetail));
  const blog = readFileSync(blogDetail, "utf8");
  assert.doesNotMatch(blog, /submitComment|comment form|ArticleComment/i);
});
