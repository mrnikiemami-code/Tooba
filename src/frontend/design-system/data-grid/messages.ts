import type { GridMessages } from "./types";

/**
 * کاتالوگ پیام گرید. فارسی پیش‌فرض ویترین است؛ کلید انگلیسی قرارداد i18n است نه متن سخت‌کد محصول.
 */
export const faGridMessages: GridMessages = {
  search: "جستجو",
  filters: "فیلترها",
  columns: "ستون‌ها",
  exportVisible: "خروجی ستون‌های نمایان",
  exportSelected: "خروجی ردیف‌های انتخابی",
  exportServer: "درخواست خروجی سمت سرور",
  savedViews: "نمای ذخیره‌شده",
  saveView: "ذخیرهٔ نما",
  deleteView: "حذف نما",
  defaultViewName: "نمای ذخیره‌شده",
  moveColumnUp: "جابه‌جایی ستون به بالا",
  moveColumnDown: "جابه‌جایی ستون به پایین",
  dragColumn: "کشیدن برای جابه‌جایی ستون",
  resizeColumn: "تغییر عرض ستون",
  densityComfortable: "راحت",
  densityCompact: "فشرده",
  previous: "قبلی",
  next: "بعدی",
  pageSize: "اندازهٔ صفحه",
  selected: "انتخاب‌شده",
  selectPage: "انتخاب صفحه",
  selectRow: "انتخاب ردیف",
  clearSelection: "پاک‌کردن انتخاب",
  clearAllFilters: "پاک‌کردن همهٔ فیلترها",
  clearFilter: "پاک کردن فیلتر",
  loading: "در حال بارگذاری",
  empty: "داده‌ای نیست",
  emptyFiltered: "با این فیلتر نتیجه‌ای نیست",
  error: "بارگذاری ناموفق بود",
  retry: "تلاش دوباره",
  restoreColumns: "بازگشت ستون‌ها",
  bulkConfirm: "این اقدام روی ردیف‌های انتخابی اجرا شود؟",
  close: "بستن",
  reload: "بارگذاری مجدد",
};

export const enGridMessages: GridMessages = {
  search: "Search",
  filters: "Filters",
  columns: "Columns",
  exportVisible: "Export visible columns",
  exportSelected: "Export selected rows",
  exportServer: "Request server export",
  savedViews: "Saved views",
  saveView: "Save view",
  deleteView: "Delete view",
  defaultViewName: "Saved view",
  moveColumnUp: "Move column up",
  moveColumnDown: "Move column down",
  dragColumn: "Drag to reorder column",
  resizeColumn: "Resize column",
  densityComfortable: "Comfortable",
  densityCompact: "Compact",
  previous: "Previous",
  next: "Next",
  pageSize: "Page size",
  selected: "Selected",
  selectPage: "Select page",
  selectRow: "Select row",
  clearSelection: "Clear selection",
  clearAllFilters: "Clear all filters",
  clearFilter: "Clear filter",
  loading: "Loading",
  empty: "No data",
  emptyFiltered: "No results for these filters",
  error: "Failed to load",
  retry: "Retry",
  restoreColumns: "Restore columns",
  bulkConfirm: "Run this action on the selected rows?",
  close: "Close",
  reload: "Reload",
};

/** برچسب عملگر فیلتر — مقدار داخلی انگلیسی می‌ماند، نمایش محلی است. */
export type FilterOperatorLabels = {
  notEqual: string;
  in: string;
  notIn: string;
  contains: string;
  notContains: string;
  equals: string;
  startsWith: string;
  endsWith: string;
  greaterThan: string;
  greaterThanOrEqual: string;
  lessThan: string;
  lessThanOrEqual: string;
  between: string;
  blank: string;
  notBlank: string;
  on: string;
  before: string;
  after: string;
  all: string;
  yes: string;
  no: string;
};

export const faFilterOperatorLabels: FilterOperatorLabels = {
  contains: "شامل",
  notContains: "شامل نمی‌شود",
  equals: "برابر",
  notEqual: "نابرابر",
  in: "یکی از",
  notIn: "هیچ‌کدام از",
  startsWith: "شروع با",
  endsWith: "پایان با",
  greaterThan: "بیشتر از",
  greaterThanOrEqual: "بیشتر یا مساوی",
  lessThan: "کمتر از",
  lessThanOrEqual: "کمتر یا مساوی",
  between: "بین",
  blank: "خالی",
  notBlank: "غیرخالی",
  on: "در تاریخ",
  before: "قبل از",
  after: "بعد از",
  all: "همه",
  yes: "بله",
  no: "خیر",
};

export const enFilterOperatorLabels: FilterOperatorLabels = {
  contains: "Contains",
  notContains: "Not contains",
  equals: "Equals",
  notEqual: "Not equal",
  in: "In",
  notIn: "Not in",
  startsWith: "Starts with",
  endsWith: "Ends with",
  greaterThan: "Greater than",
  greaterThanOrEqual: "Greater than or equal to",
  lessThan: "Less than",
  lessThanOrEqual: "Less than or equal to",
  between: "Between",
  blank: "Blank",
  notBlank: "Not blank",
  on: "On date",
  before: "Before",
  after: "After",
  all: "All",
  yes: "Yes",
  no: "No",
};

export function filterOperatorLabelsFor(locale: "fa" | "en" = "fa"): FilterOperatorLabels {
  return locale === "en" ? enFilterOperatorLabels : faFilterOperatorLabels;
}
