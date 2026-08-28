import type { ColDef } from "ag-grid-community";
import type { AdminProductListRow } from "./host-client";

/** قرارداد فیلترپذیری گرید محصولات Admin — منبع واحد حقیقت. */
export type ProductGridFilterKind = "text" | "jalali-date" | "number" | "status" | "none";

export type ProductGridFilterSpec = {
  field: string;
  kind: ProductGridFilterKind;
  /** برچسب مقدار در پنل عددی (مثلاً تومان) */
  valueLabel?: string;
};

export const ADMIN_PRODUCT_GRID_FILTER_MATRIX: Record<string, ProductGridFilterSpec> = {
  actions: { field: "actions", kind: "none" },
  media: { field: "media", kind: "none" },
  title: { field: "title", kind: "text" },
  status: { field: "status", kind: "status" },
  categorySummary: { field: "categorySummary", kind: "text" },
  offerAmountRange: { field: "offerAmountRange", kind: "number", valueLabel: "مقدار (تومان)" },
  sellableUnits: { field: "sellableUnits", kind: "number" },
  updatedAt: { field: "updatedAt", kind: "jalali-date" },
  variantCount: { field: "variantCount", kind: "number" },
  offerCount: { field: "offerCount", kind: "number" },
  locationCount: { field: "locationCount", kind: "number" },
};

export const ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS = Object.entries(ADMIN_PRODUCT_GRID_FILTER_MATRIX)
  .filter(([, spec]) => spec.kind !== "none")
  .map(([field]) => field);

export function productGridFilterableFields(): string[] {
  return ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS;
}

export function applyProductGridFilterHeader<T extends AdminProductListRow>(
  colDef: ColDef<T>,
): ColDef<T> {
  const field = String(colDef.field ?? colDef.colId ?? "");
  const spec = ADMIN_PRODUCT_GRID_FILTER_MATRIX[field];
  if (!spec || spec.kind === "none") {
    return { ...colDef, filter: false };
  }
  return {
    ...colDef,
    filter: false,
    headerComponent: "appColumnHeader",
    headerComponentParams: {
      externalFilter: spec.kind,
      filterValueLabel: spec.valueLabel,
    },
  };
}
