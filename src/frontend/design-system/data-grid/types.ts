/**
 * مدل ستونی گرید عملیاتی Tooba. موجودیت دامنه را مصرف نمی‌کند؛ فقط view-model ردیف.
 */
export type GridAlign = "start" | "end" | "center";

/**
 * چگالی ردیف. پیش‌فرض comfortable است تا تایپوگرافی عملیاتی ریز Shopeiva تکرار نشود.
 */
export type GridDensity = "comfortable" | "compact";

export type GridFilterKind = "text" | "number" | "money" | "date" | "enum" | "boolean" | "entity" | "status";

export type TextFilterOperator = "contains" | "equals" | "startsWith";
export type NumberFilterOperator =
  | "equals"
  | "notEqual"
  | "greaterThan"
  | "greaterThanOrEqual"
  | "lessThan"
  | "lessThanOrEqual"
  | "between"
  | "blank"
  | "notBlank";
export type DateFilterOperator = "on" | "before" | "after" | "between";
export type EnumFilterOperator = "equals" | "notEqual" | "in" | "notIn";

export interface MoneyAmount {
  amount: number;
  currency: string;
  amountTo?: number;
}

export type GridFilterValue =
  | { kind: "text"; operator: TextFilterOperator; query: string }
  | { kind: "number"; operator: NumberFilterOperator; value: number; valueTo?: number }
  | { kind: "money"; operator: NumberFilterOperator; money: MoneyAmount }
  | { kind: "date"; operator: DateFilterOperator; iso: string; isoTo?: string }
  | { kind: "enum"; operator?: EnumFilterOperator; values: string[] }
  | { kind: "status"; operator?: EnumFilterOperator; values: string[] }
  | { kind: "boolean"; state: "all" | "true" | "false" }
  | { kind: "entity"; ids: string[]; search?: string };

export interface GridSort {
  columnId: string;
  direction: "asc" | "desc";
}

export type AdvancedFilterConnector = "and" | "or";

export interface AdvancedFilterCondition {
  id: string;
  field: string;
  value: GridFilterValue;
}

/** عبارت فیلتر پیشرفته — AND/OR صریح، left-to-right. */
export interface AdvancedFilterExpression {
  conditions: AdvancedFilterCondition[];
  connectors: AdvancedFilterConnector[];
}

/**
 * پرس‌وجوی سمت سرور. SQL/EF اینجا معنا ندارد؛ فقط قرارداد UI.
 */
export interface GridServerQuery {
  page: number;
  pageSize: number;
  sorts: GridSort[];
  filters: Record<string, GridFilterValue>;
  search?: string;
  advancedFilter?: AdvancedFilterExpression;
}

export interface GridServerPage<T> {
  rows: T[];
  total: number;
}

export interface GridColumnDef<T> {
  id: string;
  header: string;
  accessor: (row: T) => unknown;
  cell?: (row: T) => import("react").ReactNode;
  sortable?: boolean;
  filterKind?: GridFilterKind;
  filterable?: boolean;
  resizable?: boolean;
  reorderable?: boolean;
  hideable?: boolean;
  sticky?: "start" | "end";
  align?: GridAlign;
  width: number;
  minWidth: number;
  maxWidth: number;
  exportable?: boolean;
  enumOptions?: { value: string; label: string }[];
  /** اگر false باشد ستون در نمای پیش‌فرض مخفی است و از انتخابگر ستون برمی‌گردد. */
  defaultVisible?: boolean;
}

export interface GridColumnLayout {
  order: string[];
  visibility: Record<string, boolean>;
  widths: Record<string, number>;
}

/**
 * نمای ذخیره‌شده. ذخیره‌سازی از مدل جداست.
 * schemaVersion برای migration-safe round-trip است.
 */
export const SAVED_GRID_VIEW_SCHEMA_VERSION = 3;

export interface SavedGridView {
  schemaVersion?: number;
  id: string;
  name: string;
  /** فیلترهای ادغام‌شده برای GridServerQuery (simple + advanced). */
  filters: Record<string, GridFilterValue>;
  /** زیرمجموعهٔ project-owned advanced drawer — legacy v2; v3 uses advancedFilterExpression. */
  advancedFilters?: Record<string, GridFilterValue>;
  /** عبارت فیلتر پیشرفته با AND/OR صریح (schema v3). */
  advancedFilterExpression?: AdvancedFilterExpression;
  sorts: GridSort[];
  layout: GridColumnLayout;
  pageSize: number;
  search?: string;
  density?: GridDensity;
}

export interface SavedViewStore {
  list(): Promise<SavedGridView[]>;
  save(view: SavedGridView): Promise<void>;
  remove(id: string): Promise<void>;
  getDefaultViewId?(): Promise<string | null>;
  setDefaultViewId?(id: string | null): Promise<void>;
}

export interface EntityFilterAdapter {
  search(term: string): Promise<{ id: string; label: string }[]>;
}

export interface GridBulkAction<T> {
  id: string;
  label: string;
  requiresConfirmation: boolean;
  isAvailable: (rows: T[]) => boolean;
  execute: (rows: T[]) => Promise<{ ok: boolean; message: string }>;
}

export interface GridMessages {
  search: string;
  filters: string;
  columns: string;
  exportVisible: string;
  exportSelected: string;
  exportServer: string;
  savedViews: string;
  saveView: string;
  deleteView: string;
  defaultViewName: string;
  moveColumnUp: string;
  moveColumnDown: string;
  dragColumn: string;
  resizeColumn: string;
  densityComfortable: string;
  densityCompact: string;
  previous: string;
  next: string;
  pageSize: string;
  selected: string;
  selectPage: string;
  selectRow: string;
  clearSelection: string;
  clearAllFilters: string;
  clearFilter: string;
  loading: string;
  empty: string;
  emptyFiltered: string;
  error: string;
  retry: string;
  restoreColumns: string;
  bulkConfirm: string;
  close: string;
  reload: string;
}

export type GridQueryAdapter<T> = (query: GridServerQuery) => Promise<GridServerPage<T>>;
