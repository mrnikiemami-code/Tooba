import type { GridColumnLayout, GridFilterValue, GridServerQuery, GridSort, SavedGridView } from "./types";

/**
 * مقدار پول را نرمال می‌کند: مبلغ باید متناهی باشد و ارز خالی پذیرفته نمی‌شود.
 */
export function normalizeMoney(amount: number, currency: string): { amount: number; currency: string } {
  if (!Number.isFinite(amount)) {
    throw new Error("money amount must be finite");
  }
  const code = currency.trim().toUpperCase();
  if (!code) {
    throw new Error("money currency is required");
  }
  return { amount, currency: code };
}

/**
 * تاریخ فیلتر را به ISO تقویم میلادی نگه می‌دارد. جلالی فقط لایهٔ ورودی/نمایش است.
 */
export function normalizeIsoDate(value: string): string {
  const trimmed = value.trim();
  if (!/^\d{4}-\d{2}-\d{2}/.test(trimmed)) {
    throw new Error("date filter must be canonical ISO");
  }
  return trimmed.slice(0, 10);
}

/**
 * پرس‌وجوی گرید را به رشتهٔ پایدار برای URL/آداپتر سریال می‌کند. به روتر Next قفل نیست.
 */
export function serializeGridQuery(query: GridServerQuery): string {
  return JSON.stringify({
    page: query.page,
    pageSize: query.pageSize,
    sorts: query.sorts,
    filters: query.filters,
    search: query.search ?? "",
  });
}

export function deserializeGridQuery(raw: string): GridServerQuery {
  const parsed = JSON.parse(raw) as GridServerQuery;
  return {
    page: Math.max(1, Number(parsed.page) || 1),
    pageSize: Math.min(100, Math.max(5, Number(parsed.pageSize) || 10)),
    sorts: Array.isArray(parsed.sorts) ? parsed.sorts : [],
    filters: parsed.filters ?? {},
    search: parsed.search || undefined,
  };
}

export function serializeSavedView(view: SavedGridView): string {
  return JSON.stringify(view);
}

export function deserializeSavedView(raw: string): SavedGridView {
  return JSON.parse(raw) as SavedGridView;
}

export function defaultLayout(
  columnIds: string[],
  widths: Record<string, number>,
  visibility?: Record<string, boolean>,
): GridColumnLayout {
  return {
    order: [...columnIds],
    visibility: Object.fromEntries(columnIds.map((id) => [id, visibility?.[id] !== false])),
    widths: { ...widths },
  };
}

/**
 * در RTL لبهٔ چسبندهٔ start برابر inset-inline-start است نه left فیزیکی.
 */
export function stickyLogicalSide(sticky: "start" | "end"): "inline-start" | "inline-end" {
  return sticky === "start" ? "inline-start" : "inline-end";
}

export function moveColumn(order: string[], fromId: string, toId: string): string[] {
  const next = [...order];
  const from = next.indexOf(fromId);
  const to = next.indexOf(toId);
  if (from < 0 || to < 0 || from === to) {
    return next;
  }
  next.splice(from, 1);
  next.splice(to, 0, fromId);
  return next;
}

export function clampWidth(width: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, width));
}

export function toggleSelection(selected: ReadonlySet<string>, id: string): Set<string> {
  const next = new Set(selected);
  if (next.has(id)) {
    next.delete(id);
  } else {
    next.add(id);
  }
  return next;
}

export function selectPage(ids: string[]): Set<string> {
  return new Set(ids);
}

export function visibleExportColumns(layout: GridColumnLayout, exportableIds: string[]): string[] {
  return layout.order.filter((id) => layout.visibility[id] !== false && exportableIds.includes(id));
}

export function isFilterActive(value: GridFilterValue | undefined): boolean {
  if (!value) {
    return false;
  }
  switch (value.kind) {
    case "text":
      if (value.operator === "blank" || value.operator === "notBlank") return true;
      return value.query.trim().length > 0;
    case "number":
      if (value.operator === "blank" || value.operator === "notBlank") return true;
      return Number.isFinite(value.value);
    case "money":
      return Number.isFinite(value.money.amount);
    case "date":
      return Boolean(value.iso);
    case "boolean":
      return value.state !== "all";
    case "enum":
    case "status":
      return value.values.length > 0;
    case "entity":
      return value.ids.length > 0;
    default:
      return true;
  }
}

export function cycleSort(current: GridSort[] | undefined, columnId: string): GridSort[] {
  const existing = current?.find((item) => item.columnId === columnId);
  if (!existing) {
    return [{ columnId, direction: "asc" }];
  }
  if (existing.direction === "asc") {
    return [{ columnId, direction: "desc" }];
  }
  return [];
}
