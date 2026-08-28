import type { ColDef } from "ag-grid-community";

/** انواع فیلتر هدر app-owned — AG Grid popup استفاده نمی‌شود. */
export type AppGridFilterKind = "text" | "jalali-date" | "number" | "status" | "none";

export type AppGridFilterSpec = {
  field: string;
  kind: AppGridFilterKind;
  /** برچسب مقدار در پنل عددی (مثلاً تومان) */
  valueLabel?: string;
};

/** اعمال هدر فیلتر app-owned روی ColDef — بدون وابستگی به دامنهٔ خاص. */
export function applyAppGridFilterHeader<T>(colDef: ColDef<T>, spec?: AppGridFilterSpec): ColDef<T> {
  const field = String(colDef.field ?? colDef.colId ?? "");
  const resolved = spec ?? { field, kind: "none" as const };
  if (resolved.kind === "none") {
    return { ...colDef, filter: false };
  }
  return {
    ...colDef,
    filter: false,
    headerComponent: "appColumnHeader",
    headerComponentParams: {
      externalFilter: resolved.kind,
      filterValueLabel: resolved.valueLabel,
    },
  };
}

/** استخراج فیلدهای فیلتر خارجی از ماتریس فیلتر صفحه. */
export function appGridExternalFilterFields(matrix: Record<string, AppGridFilterSpec>): string[] {
  return Object.entries(matrix)
    .filter(([, spec]) => spec.kind !== "none")
    .map(([field]) => field);
}
