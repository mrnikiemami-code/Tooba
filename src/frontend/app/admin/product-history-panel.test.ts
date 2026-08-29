import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  SECTION_FILTER_OPTIONS,
  formatHistoryTimestamp,
  historyPrimaryLabel,
  mapProductHistoryPage,
  type ProductHistoryEntry,
} from "./product-history-panel-model.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));

function sampleEntry(partial: Partial<ProductHistoryEntry> = {}): ProductHistoryEntry {
  return {
    historyId: "11111111-1111-1111-1111-111111111111",
    eventType: "product.general.changed",
    section: "general",
    sectionLabelFa: "عمومی",
    summaryFa: "اطلاعات اصلی محصول ویرایش شد",
    beforeSummary: "عنوان قدیم",
    afterSummary: "عنوان جدید",
    actorDisplayName: "اپراتور",
    occurredAt: "2026-08-29T10:00:00Z",
    ...partial,
  };
}

test("mapProductHistoryPage maps camelCase and preserves API order", () => {
  const mapped = mapProductHistoryPage({
    items: [
      {
        historyId: "a",
        eventType: "product.created",
        section: "lifecycle",
        sectionLabelFa: "انتشار",
        summaryFa: "محصول ایجاد شد",
        beforeSummary: null,
        afterSummary: null,
        actorDisplayName: "سیستم",
        occurredAt: "2026-08-29T12:00:00Z",
      },
      {
        historyId: "b",
        eventType: "product.seo.changed",
        section: "seo",
        sectionLabelFa: "سئو",
        summaryFa: "اطلاعات سئو ویرایش شد",
        beforeSummary: null,
        afterSummary: "slug",
        actorDisplayName: "ادمین",
        occurredAt: "2026-08-28T12:00:00Z",
      },
    ],
    totalCount: 2,
    skip: 0,
    take: 50,
  });
  assert.ok(mapped);
  assert.equal(mapped!.items.length, 2);
  assert.equal(mapped!.items[0]!.historyId, "a");
  assert.equal(mapped!.items[1]!.historyId, "b");
  assert.equal(mapped!.totalCount, 2);
  assert.match(mapped!.items[0]!.summaryFa, /محصول ایجاد شد/);
  assert.match(mapped!.items[1]!.summaryFa, /سئو/);
});

test("mapProductHistoryPage accepts PascalCase Host payload", () => {
  const mapped = mapProductHistoryPage({
    Items: [
      {
        HistoryId: "c",
        EventType: "product.media.changed",
        Section: "media",
        SectionLabelFa: "رسانه",
        SummaryFa: "رسانهٔ محصول به‌روزرسانی شد",
        BeforeSummary: null,
        AfterSummary: null,
        ActorDisplayName: "سیستم",
        OccurredAt: "2026-08-27T08:00:00Z",
      },
    ],
    TotalCount: 1,
    Skip: 0,
    Take: 50,
  });
  assert.ok(mapped);
  assert.equal(mapped!.items[0]!.sectionLabelFa, "رسانه");
  assert.equal(mapped!.items[0]!.summaryFa, "رسانهٔ محصول به‌روزرسانی شد");
});

test("Persian summaries and section filter labels", () => {
  const entry = sampleEntry();
  assert.match(entry.summaryFa, /اطلاعات اصلی/);
  assert.equal(historyPrimaryLabel(entry), entry.summaryFa);
  assert.ok(SECTION_FILTER_OPTIONS.some((o) => o.value === "general" && o.labelFa === "عمومی"));
  assert.ok(SECTION_FILTER_OPTIONS.some((o) => o.value === "lifecycle" && o.labelFa === "انتشار"));
  const formatted = formatHistoryTimestamp("2026-08-29T10:00:00Z");
  assert.ok(formatted.length > 0);
  assert.notEqual(formatted, "—");
});

test("panel and screen wiring contracts", () => {
  const panel = fs.readFileSync(path.join(root, "product-history-panel.tsx"), "utf8");
  const screen = fs.readFileSync(path.join(root, "product-workspace-screen.tsx"), "utf8");
  const client = fs.readFileSync(path.join(root, "host-client.ts"), "utf8");

  assert.match(panel, /data-testid="product-history-panel"/);
  assert.match(panel, /getAdminProductHistory/);
  assert.match(panel, /summaryFa|historyPrimaryLabel/);
  assert.match(panel, /sectionLabelFa/);
  assert.match(panel, /در حال بارگذاری تاریخچه/);
  assert.match(panel, /تاریخچه‌ای ثبت نشده است/);
  assert.match(panel, /بارگذاری بیشتر/);
  assert.match(panel, /aria-busy/);
  assert.match(panel, /role="alert"/);
  assert.doesNotMatch(panel, /AgGridReact/);
  assert.doesNotMatch(panel, /eventType\}/);
  assert.doesNotMatch(panel, /\{entry\.eventType\}/);
  assert.doesNotMatch(panel, /JSON\.stringify/);

  assert.match(screen, /ProductHistoryPanel/);
  assert.match(screen, /sectionId === "history"/);
  assert.doesNotMatch(screen, /product-history-placeholder/);

  assert.match(client, /getAdminProductHistory/);
  assert.match(client, /\/history\?/);
});
