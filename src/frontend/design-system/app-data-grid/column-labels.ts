/** برچسب انسانی ستون‌های سیستمی AG Grid — نه colId فنی. */
const SYSTEM_COLUMN_LABELS: Record<string, { fa: string; en: string }> = {
  "ag-Grid-SelectionColumn": { fa: "انتخاب", en: "Selection" },
  "ag-Grid-AutoColumn": { fa: "گروه", en: "Group" },
};

export function resolveColumnLabel(
  colId: string,
  columnLabels: Record<string, string>,
  locale: "fa" | "en",
): string {
  const system = SYSTEM_COLUMN_LABELS[colId];
  if (system) return system[locale];
  return columnLabels[colId] ?? colId;
}

export function isColumnVisibilityLocked(colId: string): boolean {
  return colId === "ag-Grid-SelectionColumn" || colId === "actions";
}
