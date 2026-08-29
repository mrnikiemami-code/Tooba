/** ردیف تاریخچهٔ محصول از Host (نمایش انسانی؛ eventType خام برچسب اصلی UI نیست). */
export type ProductHistoryEntry = {
  historyId: string;
  /** کد رویداد فنی — فقط برای تشخیص/دیباگ؛ در UI به‌عنوان برچسب اصلی نشان داده نشود. */
  eventType: string;
  section: string;
  sectionLabelFa: string;
  summaryFa: string;
  beforeSummary: string | null;
  afterSummary: string | null;
  actorDisplayName: string;
  occurredAt: string;
};

/** صفحهٔ صفحه‌بندی‌شدهٔ تاریخچه. */
export type ProductHistoryPage = {
  items: ProductHistoryEntry[];
  totalCount: number;
  skip: number;
  take: number;
};

/** گزینهٔ فیلتر بخش برای UI (برچسب‌ها مطابق ProductHistoryRules). */
export type HistorySectionFilterOption = {
  value: string;
  labelFa: string;
};

export const SECTION_FILTER_OPTIONS: readonly HistorySectionFilterOption[] = [
  { value: "", labelFa: "همه بخش‌ها" },
  { value: "general", labelFa: "عمومی" },
  { value: "category", labelFa: "دسته‌بندی" },
  { value: "attributes", labelFa: "ویژگی‌ها" },
  { value: "variants", labelFa: "تنوع‌ها" },
  { value: "media", labelFa: "رسانه" },
  { value: "seo", labelFa: "سئو" },
  { value: "lifecycle", labelFa: "انتشار" },
] as const;

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function asString(value: unknown, fallback = ""): string {
  if (value == null) return fallback;
  return String(value);
}

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function asNullableString(value: unknown): string | null {
  if (value == null) return null;
  const text = String(value);
  return text.length === 0 ? null : text;
}

function asRecordArray(value: unknown): Record<string, unknown>[] {
  return Array.isArray(value)
    ? value.filter((item): item is Record<string, unknown> => !!item && typeof item === "object")
    : [];
}

/** نگاشت یک ردیف خام Host به مدل UI. */
export function mapProductHistoryEntry(raw: Record<string, unknown>): ProductHistoryEntry {
  const occurredRaw = readProp(raw, "occurredAt", "OccurredAt");
  return {
    historyId: asString(readProp(raw, "historyId", "HistoryId")),
    eventType: asString(readProp(raw, "eventType", "EventType")),
    section: asString(readProp(raw, "section", "Section")),
    sectionLabelFa: asString(readProp(raw, "sectionLabelFa", "SectionLabelFa")),
    summaryFa: asString(readProp(raw, "summaryFa", "SummaryFa")),
    beforeSummary: asNullableString(readProp(raw, "beforeSummary", "BeforeSummary")),
    afterSummary: asNullableString(readProp(raw, "afterSummary", "AfterSummary")),
    actorDisplayName: asString(readProp(raw, "actorDisplayName", "ActorDisplayName"), "سیستم"),
    occurredAt: occurredRaw == null ? "" : asString(occurredRaw),
  };
}

/** نگاشت صفحهٔ تاریخچه از پاسخ Host (camelCase یا PascalCase). */
export function mapProductHistoryPage(raw: unknown): ProductHistoryPage | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const items = asRecordArray(readProp(record, "items", "Items")).map(mapProductHistoryEntry);
  return {
    items,
    totalCount: asNumber(readProp(record, "totalCount", "TotalCount")),
    skip: asNumber(readProp(record, "skip", "Skip")),
    take: asNumber(readProp(record, "take", "Take"), 50),
  };
}

/** زمان رخداد به‌صورت محلی و دوستانه برای fa-IR. */
export function formatHistoryTimestamp(iso: string): string {
  if (!iso) return "—";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  try {
    return new Intl.DateTimeFormat("fa-IR", {
      dateStyle: "medium",
      timeStyle: "short",
    }).format(date);
  } catch {
    return date.toLocaleString("fa-IR");
  }
}

/** برچسب اصلی نمایشی — summaryFa؛ هرگز eventType یا JSON خام. */
export function historyPrimaryLabel(entry: ProductHistoryEntry): string {
  const summary = entry.summaryFa.trim();
  if (summary) return summary;
  const section = entry.sectionLabelFa.trim();
  return section || "رویداد";
}

/** آیا قبل/بعد برای نمایش وجود دارد (بدون dump JSON). */
export function historyHasBeforeAfter(entry: ProductHistoryEntry): boolean {
  return Boolean(entry.beforeSummary?.trim() || entry.afterSummary?.trim());
}

/** آیا هنوز صفحهٔ بعدی برای بارگذاری هست. */
export function historyHasMore(page: ProductHistoryPage): boolean {
  return page.totalCount > page.items.length;
}
