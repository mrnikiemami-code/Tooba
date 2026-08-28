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

export interface GridAdvancedFilterCondition {
  id: string;
  field: string;
  operator: string;
  value?: string;
  valueTo?: string;
  values?: string[];
}

export interface GridAdvancedFilterExpression {
  conditions: GridAdvancedFilterCondition[];
  connectors: ("and" | "or")[];
}

export interface GridQueryRequest {
  page: number;
  pageSize: number;
  search?: string;
  sort: GridSortRequest[];
  filters: GridFilterRequest[];
  advancedFilter?: GridAdvancedFilterExpression;
}

export interface GridPageResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
