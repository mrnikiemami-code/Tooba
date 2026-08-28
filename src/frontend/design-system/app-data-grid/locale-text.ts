export type LocaleKey =
  | "search"
  | "filters"
  | "columns"
  | "exportCsv"
  | "exportExcel"
  | "savedViews"
  | "saveView"
  | "deleteView"
  | "loading"
  | "empty"
  | "emptyFiltered"
  | "error"
  | "retry"
  | "previous"
  | "next"
  | "pageSize"
  | "selectedPage"
  | "clearSelection"
  | "clearFilters"
  | "advancedFilter"
  | "apply"
  | "cancel"
  | "restoreColumns"
  | "pageSelectionNote"
  | "exportScopeNote";

export const faGridLocale: Record<LocaleKey, string> = {
  search: "جستجو…",
  filters: "فیلترها",
  columns: "ستون‌ها",
  exportCsv: "خروجی CSV (صفحهٔ جاری)",
  exportExcel: "خروجی Excel (صفحهٔ جاری)",
  savedViews: "نمای ذخیره‌شده",
  saveView: "ذخیره نما",
  deleteView: "حذف نما",
  loading: "در حال بارگذاری…",
  empty: "ردیفی برای نمایش نیست",
  emptyFiltered: "با این فیلتر ردیفی یافت نشد",
  error: "خطا در بارگذاری",
  retry: "تلاش مجدد",
  previous: "قبلی",
  next: "بعدی",
  pageSize: "اندازه صفحه",
  selectedPage: "انتخاب صفحهٔ جاری",
  clearSelection: "پاک کردن انتخاب",
  clearFilters: "پاک کردن فیلترها",
  advancedFilter: "فیلتر پیشرفته",
  apply: "اعمال",
  cancel: "انصراف",
  restoreColumns: "بازنشانی ستون‌ها",
  pageSelectionNote: "فقط ردیف‌های صفحهٔ جاری انتخاب می‌شوند",
  exportScopeNote: "خروجی فقط شامل ردیف‌های صفحهٔ جاری است",
};

export const enGridLocale: Record<LocaleKey, string> = {
  search: "Search…",
  filters: "Filters",
  columns: "Columns",
  exportCsv: "Export CSV (current page)",
  exportExcel: "Export Excel (current page)",
  savedViews: "Saved view",
  saveView: "Save view",
  deleteView: "Delete view",
  loading: "Loading…",
  empty: "No rows to show",
  emptyFiltered: "No rows match filters",
  error: "Load error",
  retry: "Retry",
  previous: "Previous",
  next: "Next",
  pageSize: "Page size",
  selectedPage: "Select current page",
  clearSelection: "Clear selection",
  clearFilters: "Clear filters",
  advancedFilter: "Advanced filter",
  apply: "Apply",
  cancel: "Cancel",
  restoreColumns: "Reset columns",
  pageSelectionNote: "Only rows on the current page can be selected",
  exportScopeNote: "Export includes current page rows only",
};

export function resolveGridLocale(locale: "fa" | "en"): Record<LocaleKey, string> {
  return locale === "en" ? enGridLocale : faGridLocale;
}

export function buildAgGridLocaleText(locale: "fa" | "en"): Record<string, string> {
  const m = resolveGridLocale(locale);
  return {
    next: m.next,
    previous: m.previous,
    loadingOoo: m.loading,
    noRowsToShow: m.empty,
    filterOoo: m.filters,
    applyFilter: m.apply,
    resetFilter: m.clearFilters,
    clearFilter: m.clearFilters,
    equals: "برابر",
    notEqual: "نابرابر",
    contains: "شامل",
    notContains: "شامل نباشد",
    startsWith: "شروع با",
    endsWith: "پایان با",
    lessThan: "کمتر از",
    greaterThan: "بیشتر از",
    columns: m.columns,
    searchOoo: m.search,
  };
}
