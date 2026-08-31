import type { ColDef } from "ag-grid-community";
import {
  applyAppGridFilterHeader,
  appGridExternalFilterFields,
  type AppGridFilterSpec,
} from "../../design-system/app-data-grid/app-grid-filter-header.ts";
import type { AdminProductListRow } from "./host-client";

/** قرارداد فیلترپذیری گرید محصولات Admin — منبع واحد حقیقت دامنه. */
export const ADMIN_PRODUCT_GRID_FILTER_MATRIX: Record<string, AppGridFilterSpec> = {
  actions: { field: "actions", kind: "none" },
  media: { field: "media", kind: "none" },
  title: { field: "title", kind: "text" },
  status: { field: "status", kind: "status" },
  categorySummary: { field: "categorySummary", kind: "text" },
  primaryCategoryName: { field: "primaryCategoryName", kind: "text" },
  additionalCategoryNames: { field: "additionalCategoryNames", kind: "text" },
  offerAmountRange: { field: "offerAmountRange", kind: "number", valueLabel: "مقدار (ریال)" },
  sellableUnits: { field: "sellableUnits", kind: "number" },
  updatedAt: { field: "updatedAt", kind: "jalali-date" },
  variantCount: { field: "variantCount", kind: "number" },
  offerCount: { field: "offerCount", kind: "number" },
  locationCount: { field: "locationCount", kind: "number" },
};

export const ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS = appGridExternalFilterFields(ADMIN_PRODUCT_GRID_FILTER_MATRIX);

export function productGridFilterableFields(): string[] {
  return ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS;
}

export function applyProductGridFilterHeader<T extends AdminProductListRow>(colDef: ColDef<T>): ColDef<T> {
  const field = String(colDef.field ?? colDef.colId ?? "");
  return applyAppGridFilterHeader(colDef, ADMIN_PRODUCT_GRID_FILTER_MATRIX[field]);
}
