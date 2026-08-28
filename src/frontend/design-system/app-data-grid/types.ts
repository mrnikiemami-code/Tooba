export interface GridSortRequest {
  field: string;
  direction: "asc" | "desc";
}

export interface GridFilterRequest {
  field: string;
  operator: string;
  value?: string;
  valueTo?: string;
  values?: string[];
}

export interface GridQueryRequest {
  page: number;
  pageSize: number;
  search?: string;
  sort: GridSortRequest[];
  filters: GridFilterRequest[];
}

export interface GridPageResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
