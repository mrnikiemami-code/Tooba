/** قابلیت‌های قابل خاموش/روشن شدن نوار ابزار AppDataGrid — پیش‌فرض = گرید حرفه‌ای کامل. */
export type AppGridCapabilities = {
  search?: boolean;
  advancedFilter?: boolean;
  savedViews?: boolean;
  columnManager?: boolean;
  csvExport?: boolean;
  excelExport?: boolean;
  rowSelection?: boolean;
};

export const DEFAULT_APP_GRID_CAPABILITIES: Required<AppGridCapabilities> = {
  search: true,
  advancedFilter: true,
  savedViews: true,
  columnManager: true,
  csvExport: true,
  excelExport: true,
  rowSelection: true,
};

/** ادغام قابلیت‌های سفارشی با پیش‌فرض canonical. */
export function resolveAppGridCapabilities(input?: AppGridCapabilities): Required<AppGridCapabilities> {
  return { ...DEFAULT_APP_GRID_CAPABILITIES, ...input };
}
