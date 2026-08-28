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
  | "exportScopeNote"
  | "close"
  | "dragColumn"
  | "moveColumnUp"
  | "moveColumnDown"
  | "defaultViewName"
  | "renameView"
  | "restoreDefault"
  | "andConnector"
  | "orConnector"
  | "addCondition"
  | "removeCondition"
  | "clearAllFilters"
  | "setDefault"
  | "updateView"
  | "systemDefault"
  | "totalRows"
  | "showingRows"
  | "advancedFilterEntry"
  | "selectedCount"
  | "advancedFilterTitle"
  | "advancedFilterSubtitle"
  | "fieldLabel"
  | "operatorLabel"
  | "valueLabel"
  | "deleteCondition"
  | "activeFilters"
  | "applyFilters"
  | "resetDefault"
  | "columnManagerSearch"
  | "lockedColumnVisibility";

export const faGridLocale: Record<LocaleKey, string> = {
  search: "جستجو…",
  filters: "فیلترها",
  columns: "ستون‌ها",
  exportCsv: "خروجی CSV",
  exportExcel: "خروجی Excel",
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
  clearAllFilters: "حذف همه فیلترها",
  advancedFilter: "فیلتر پیشرفته",
  apply: "اعمال",
  cancel: "انصراف",
  restoreColumns: "بازنشانی ستون‌ها",
  pageSelectionNote: "فقط ردیف‌های صفحهٔ جاری انتخاب می‌شوند",
  exportScopeNote: "خروجی فقط شامل ردیف‌های صفحهٔ جاری است",
  close: "بستن",
  dragColumn: "جابجایی ستون",
  moveColumnUp: "بالا",
  moveColumnDown: "پایین",
  defaultViewName: "نمای جدید",
  renameView: "تغییر نام",
  restoreDefault: "بازنشانی پیش‌فرض",
  andConnector: "و",
  orConnector: "یا",
  addCondition: "افزودن شرط",
  removeCondition: "حذف",
  setDefault: "پیش‌فرض باشد",
  updateView: "به‌روزرسانی با وضعیت فعلی",
  systemDefault: "پیش‌فرض",
  totalRows: "تعداد کل",
  showingRows: "نمایش",
  advancedFilterEntry: "فیلتر پیشرفته",
  selectedCount: "انتخاب‌شده",
  advancedFilterTitle: "فیلتر پیشرفته محصولات",
  advancedFilterSubtitle: "جستجوی دقیق میان محصولات",
  fieldLabel: "فیلد",
  operatorLabel: "عملگر",
  valueLabel: "مقدار",
  deleteCondition: "حذف شرط",
  activeFilters: "فیلترهای فعال",
  applyFilters: "اعمال فیلترها",
  resetDefault: "بازنشانی پیش‌فرض",
  columnManagerSearch: "جستجوی ستون…",
  lockedColumnVisibility: "این ستون قابل پنهان‌سازی نیست",
};

export const enGridLocale: Record<LocaleKey, string> = {
  search: "Search…",
  filters: "Filters",
  columns: "Columns",
  exportCsv: "Export CSV",
  exportExcel: "Export Excel",
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
  clearAllFilters: "Clear all filters",
  advancedFilter: "Advanced filter",
  apply: "Apply",
  cancel: "Cancel",
  restoreColumns: "Reset columns",
  pageSelectionNote: "Only rows on the current page can be selected",
  exportScopeNote: "Export includes current page rows only",
  close: "Close",
  dragColumn: "Drag column",
  moveColumnUp: "Up",
  moveColumnDown: "Down",
  defaultViewName: "New view",
  renameView: "Rename",
  restoreDefault: "Restore default",
  andConnector: "AND",
  orConnector: "OR",
  addCondition: "Add condition",
  removeCondition: "Remove",
  setDefault: "Set as default",
  updateView: "Update with current state",
  systemDefault: "Default",
  totalRows: "Total",
  showingRows: "Showing",
  advancedFilterEntry: "Advanced filter",
  selectedCount: "Selected",
  advancedFilterTitle: "Advanced product filter",
  advancedFilterSubtitle: "Precise catalog search",
  fieldLabel: "Field",
  operatorLabel: "Operator",
  valueLabel: "Value",
  deleteCondition: "Remove condition",
  activeFilters: "Active filters",
  applyFilters: "Apply filters",
  resetDefault: "Reset to default",
  columnManagerSearch: "Search columns…",
  lockedColumnVisibility: "This column cannot be hidden",
};

export function resolveGridLocale(locale: "fa" | "en"): Record<LocaleKey, string> {
  return locale === "en" ? enGridLocale : faGridLocale;
}

export function buildAgGridLocaleText(locale: "fa" | "en"): Record<string, string> {
  const m = resolveGridLocale(locale);
  const ops = locale === "en"
    ? {
        equals: "Equals",
        notEqual: "Not equal",
        contains: "Contains",
        notContains: "Not contains",
        startsWith: "Starts with",
        endsWith: "Ends with",
        lessThan: "Less than",
        greaterThan: "Greater than",
      }
    : {
        equals: "برابر",
        notEqual: "نابرابر",
        contains: "شامل",
        notContains: "شامل نباشد",
        startsWith: "شروع با",
        endsWith: "پایان با",
        lessThan: "کمتر از",
        greaterThan: "بیشتر از",
      };
  return {
    next: m.next,
    previous: m.previous,
    loadingOoo: m.loading,
    noRowsToShow: m.empty,
    filterOoo: m.filters,
    applyFilter: m.apply,
    resetFilter: m.clearFilters,
    clearFilter: m.clearFilters,
    ...ops,
    columns: m.columns,
    searchOoo: m.search,
  };
}
