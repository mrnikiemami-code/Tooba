import { isFilterActive, normalizeIsoDate } from "./serialize.ts";
import type { GridColumnDef, GridFilterValue, GridServerPage, GridServerQuery } from "./types";

function asString(value: unknown): string {
  if (value == null) {
    return "";
  }
  return String(value);
}

function asNumber(value: unknown): number {
  if (typeof value === "number") {
    return value;
  }
  if (value && typeof value === "object" && "amount" in value) {
    return Number((value as { amount: number }).amount);
  }
  return Number(value);
}

function matches(cell: unknown, filter: GridFilterValue): boolean {
  switch (filter.kind) {
    case "text": {
      const hay = asString(cell).toLowerCase();
      const needle = filter.query.trim().toLowerCase();
      if (!needle) {
        return true;
      }
      if (filter.operator === "equals") {
        return hay === needle;
      }
      if (filter.operator === "startsWith") {
        return hay.startsWith(needle);
      }
      return hay.includes(needle);
    }
    case "number":
    case "money": {
      const n = asNumber(cell);
      const target = filter.kind === "money" ? filter.money.amount : filter.value;
      const to = filter.kind === "money" ? filter.money.amountTo : filter.valueTo;
      switch (filter.operator) {
        case "equals":
          return n === target;
        case "greaterThan":
          return n > target;
        case "greaterThanOrEqual":
          return n >= target;
        case "lessThan":
          return n < target;
        case "lessThanOrEqual":
          return n <= target;
        case "between":
          return n >= target && n <= (to ?? target);
        default:
          return true;
      }
    }
    case "date": {
      const iso = normalizeIsoDate(asString(cell).slice(0, 10) || "1970-01-01");
      if (filter.operator === "on") {
        return iso === filter.iso;
      }
      if (filter.operator === "before") {
        return iso < filter.iso;
      }
      if (filter.operator === "after") {
        return iso > filter.iso;
      }
      return iso >= filter.iso && iso <= (filter.isoTo ?? filter.iso);
    }
    case "enum":
    case "status":
      return filter.values.length === 0 || filter.values.includes(asString(cell));
    case "boolean":
      if (filter.state === "all") {
        return true;
      }
      return String(Boolean(cell)) === filter.state;
    case "entity":
      return filter.ids.length === 0 || filter.ids.includes(asString(cell));
    default:
      return true;
  }
}

/**
 * موتور دمو برای قرارداد سمت‌سرور. آداپتر واقعی همین شکل را روی HTTP پیاده می‌کند و کل جدول را به مرورگر نمی‌آورد.
 */
export function executeGridQuery<T>(
  source: readonly T[],
  columns: GridColumnDef<T>[],
  query: GridServerQuery,
): GridServerPage<T> {
  const accessors = new Map(columns.map((column) => [column.id, column.accessor]));
  let rows = source.filter((row) => {
    if (query.search?.trim()) {
      const q = query.search.trim().toLowerCase();
      if (!columns.some((column) => asString(column.accessor(row)).toLowerCase().includes(q))) {
        return false;
      }
    }
    return Object.entries(query.filters).every(([columnId, filter]) => {
      if (!isFilterActive(filter)) {
        return true;
      }
      const accessor = accessors.get(columnId);
      return accessor ? matches(accessor(row), filter) : true;
    });
  });

  for (const sort of [...query.sorts].reverse()) {
    const accessor = accessors.get(sort.columnId);
    if (!accessor) {
      continue;
    }
    rows = [...rows].sort((left, right) => {
      const cmp = asString(accessor(left)).localeCompare(asString(accessor(right)), undefined, { numeric: true });
      return sort.direction === "asc" ? cmp : -cmp;
    });
  }

  const total = rows.length;
  const start = (query.page - 1) * query.pageSize;
  return { rows: rows.slice(start, start + query.pageSize), total };
}

export function rowsToCsv<T>(rows: T[], columns: GridColumnDef<T>[], columnIds: string[]): string {
  const selected = columns.filter((column) => columnIds.includes(column.id) && column.exportable !== false);
  const header = selected.map((column) => csvCell(column.header)).join(",");
  const body = rows.map((row) =>
    selected
      .map((column) => csvCell(String(column.accessor(row) ?? "")))
      .join(","),
  );
  return [header, ...body].join("\n");
}

function csvCell(value: string): string {
  if (/[",\n]/.test(value)) {
    return `"${value.replaceAll("\"", "\"\"")}"`;
  }
  return value;
}
