import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  articleReadinessCheckLabel,
  mapArticleHistoryPage,
  mapArticlePublicationReadiness,
} from "./content-article-publication-model.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("mapArticlePublicationReadiness accepts PascalCase Host payload", () => {
  const mapped = mapArticlePublicationReadiness({
    CanPublish: false,
    Score: 40,
    Checks: [
      {
        Key: "content.publish.author",
        LabelKey: "content.publish.check.author",
        Required: true,
        Satisfied: false,
        ActionTarget: "author",
      },
    ],
    RequiredMissing: [
      {
        Key: "content.publish.author",
        LabelKey: "content.publish.check.author",
        Required: true,
        Satisfied: false,
        ActionTarget: "author",
      },
    ],
    RecommendedMissing: [],
  });
  assert.ok(mapped);
  assert.equal(mapped!.canPublish, false);
  assert.equal(mapped!.requiredMissing[0]?.key, "content.publish.author");
  assert.match(articleReadinessCheckLabel(mapped!.requiredMissing[0]!, "fa-IR"), /نویسنده/);
});

test("mapArticleHistoryPage maps human labels newest-first payload", () => {
  const page = mapArticleHistoryPage({
    Items: [
      {
        HistoryId: "1",
        ArticleId: "a",
        EventType: "article.republished",
        EventLabelFa: "انتشار مجدد",
        EventLabelEn: "Republished",
        SummaryFa: "مقاله دوباره منتشر شد",
        SummaryEn: "Article republished",
        PreviousState: "پیش‌نویس",
        NewState: "منتشرشده",
        ActorDisplayName: "سیستم",
        OccurredAt: "2026-09-04T10:00:00Z",
      },
    ],
    TotalCount: 1,
    Skip: 0,
    Take: 50,
  });
  assert.ok(page);
  assert.equal(page!.items[0]?.eventLabelFa, "انتشار مجدد");
  assert.doesNotMatch(page!.items[0]?.eventLabelFa ?? "", /article\./);
});

test("T014 workspace keeps CKEditor, category picker, tags chips", () => {
  const screen = fs.readFileSync(
    path.join(root, "app/admin/content-article-admin-screen.tsx"),
    "utf8",
  );
  assert.match(screen, /ContentArticleEditor/);
  assert.match(screen, /ContentArticleCategoryPicker/);
  assert.match(screen, /ContentArticleTagsPanel/);
  assert.match(screen, /ContentArticleReadinessSummary/);
  assert.match(screen, /ContentArticleHistoryTimeline/);
  assert.match(screen, /content-article-history-pager/);
  assert.match(screen, /ContentArticlePublishDateField/);
  assert.match(screen, /content-article-preview/);
  assert.match(screen, /برای پیش‌نمایش ابتدا تغییرات را ذخیره کنید/);
});

test("T014 preview route is admin-only and noindex", () => {
  const preview = fs.readFileSync(
    path.join(root, "app/admin/content/articles/[articleId]/preview/page.tsx"),
    "utf8",
  );
  assert.match(preview, /loadArticleAdminPreview/);
  assert.match(preview, /noindex/);
  assert.match(preview, /ArticleBodyHtml/);
  assert.doesNotMatch(preview, /\?preview=true/);
});

test("T014 publish dialog blocks known mandatory readiness gaps", () => {
  const dialog = fs.readFileSync(
    path.join(root, "app/admin/content-article-destructive-dialog.tsx"),
    "utf8",
  );
  assert.match(dialog, /content-article-publish-blockers/);
  assert.match(dialog, /disabled=\{pending \|\| blocked\}/);
  assert.match(dialog, /republish/);
});
