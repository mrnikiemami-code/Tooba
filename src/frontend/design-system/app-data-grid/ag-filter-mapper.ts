import type { FilterModel } from "ag-grid-community";
import type { GridFilterValue, NumberFilterOperator } from "../data-grid/types";
import { filterOperatorLabelsFor } from "../data-grid/messages.ts";
import { formatJalaliDate } from "./jalali.ts";

export type FilterChipOptions = {
  enumLabels?: Record<string, string>;
  locale?: "fa" | "en";
};

type AgFilterEntry = {
  filterType?: string;
  type?: string;
  filter?: string | number | null;
  filterTo?: string | number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  values?: string[] | null;
};

/** GridFilterValue را به AG FilterModel برمی‌گرداند — فقط فیلترهای Community-safe ستونی. */
export function toAgFilterModel(
  filters: Record<string, GridFilterValue>,
  options?: { excludeFields?: ReadonlySet<string> },
): FilterModel {
  const model: FilterModel = {};
  for (const [field, value] of Object.entries(filters)) {
    if (options?.excludeFields?.has(field)) {
      continue;
    }
    const entry = mapGridFilterToAg(field, value);
    if (entry) {
      model[field] = entry;
    }
  }
  return model;
}

/** AG Grid FilterModel را به قرارداد GridServerQuery پروژه نگاشت می‌کند — backend مدل AG را نمی‌بیند. */
export function fromAgFilterModel(model: FilterModel | null | undefined): Record<string, GridFilterValue> {
  if (!model) {
    return {};
  }

  const filters: Record<string, GridFilterValue> = {};
  for (const [field, raw] of Object.entries(model)) {
    const mapped = mapAgFilterEntry(field, raw as AgFilterEntry);
    if (mapped) {
      filters[field] = mapped;
    }
  }

  return filters;
}

function mapGridFilterToAg(field: string, value: GridFilterValue): AgFilterEntry | undefined {
  switch (value.kind) {
    case "text":
      return {
        filterType: "text",
        type: reverseTextOperator(value.operator),
        filter: value.query,
      };
    case "number":
      return {
        filterType: "number",
        type: reverseNumberOperator(value.operator),
        filter: value.value,
        filterTo: value.operator === "between" ? value.valueTo : undefined,
      };
    case "date":
      return {
        filterType: "date",
        type: reverseDateOperator(value.operator),
        dateFrom: value.iso,
        dateTo: value.operator === "between" ? value.isoTo : undefined,
      };
    case "enum":
    case "status":
      // enum/status در کشوی پیشرفته Community-safe است — AG Set filter Enterprise-only است.
      return undefined;
    default:
      return undefined;
  }
}

function reverseTextOperator(operator: "contains" | "equals" | "startsWith"): string {
  switch (operator) {
    case "equals":
      return "equals";
    case "startsWith":
      return "startsWith";
    default:
      return "contains";
  }
}

function reverseNumberOperator(
  operator: NumberFilterOperator,
): string {
  switch (operator) {
    case "equals":
      return "equals";
    case "notEqual":
      return "notEqual";
    case "greaterThan":
      return "greaterThan";
    case "greaterThanOrEqual":
      return "greaterThanOrEqual";
    case "lessThan":
      return "lessThan";
    case "lessThanOrEqual":
      return "lessThanOrEqual";
    case "between":
      return "inRange";
    case "blank":
      return "blank";
    case "notBlank":
      return "notBlank";
    default:
      return "equals";
  }
}

function reverseDateOperator(operator: "on" | "before" | "after" | "between"): string {
  switch (operator) {
    case "before":
      return "lessThan";
    case "after":
      return "greaterThan";
    case "between":
      return "inRange";
    default:
      return "equals";
  }
}

function mapAgFilterEntry(field: string, raw: AgFilterEntry | undefined): GridFilterValue | undefined {
  if (!raw) {
    return undefined;
  }

  if (raw.filterType === "set" || Array.isArray(raw.values)) {
    const values = (raw.values ?? []).filter(Boolean).map(String);
    if (values.length === 0) {
      return undefined;
    }

    return field === "status"
      ? { kind: "status", values }
      : { kind: "enum", values };
  }

  if (raw.filterType === "number" || typeof raw.filter === "number") {
    if (raw.type === "blank" || raw.type === "notBlank") {
      return { kind: "number", operator: raw.type, value: 0 };
    }
    const value = Number(raw.filter);
    if (!Number.isFinite(value)) {
      return undefined;
    }

    const valueTo = raw.filterTo != null ? Number(raw.filterTo) : undefined;
    return {
      kind: "number",
      operator: mapNumberOperator(raw.type),
      value,
      valueTo: Number.isFinite(valueTo) ? valueTo : undefined,
    };
  }

  if (raw.filterType === "date" || raw.dateFrom) {
    const iso = normalizeAgDate(raw.dateFrom);
    if (!iso) {
      return undefined;
    }

    const isoTo = raw.dateTo ? normalizeAgDate(raw.dateTo) : undefined;
    return {
      kind: "date",
      operator: mapDateOperator(raw.type),
      iso,
      isoTo,
    };
  }

  const text = raw.filter != null ? String(raw.filter).trim() : "";
  if (!text) {
    return undefined;
  }

  return {
    kind: "text",
    operator: mapTextOperator(raw.type),
    query: text,
  };
}

function mapTextOperator(type: string | undefined): "contains" | "equals" | "startsWith" {
  switch (type) {
    case "equals":
      return "equals";
    case "startsWith":
      return "startsWith";
    default:
      return "contains";
  }
}

function mapNumberOperator(
  type: string | undefined,
): NumberFilterOperator {
  switch (type) {
    case "equals":
      return "equals";
    case "notEqual":
      return "notEqual";
    case "greaterThan":
      return "greaterThan";
    case "greaterThanOrEqual":
      return "greaterThanOrEqual";
    case "lessThan":
      return "lessThan";
    case "lessThanOrEqual":
      return "lessThanOrEqual";
    case "inRange":
      return "between";
    case "blank":
      return "blank";
    case "notBlank":
      return "notBlank";
    default:
      return "equals";
  }
}

function mapDateOperator(type: string | undefined): "on" | "before" | "after" | "between" {
  switch (type) {
    case "lessThan":
      return "before";
    case "greaterThan":
      return "after";
    case "inRange":
      return "between";
    default:
      return "on";
  }
}

function normalizeAgDate(value: string | null | undefined): string | undefined {
  if (!value) {
    return undefined;
  }

  const trimmed = value.trim();
  if (!trimmed) {
    return undefined;
  }

  const parsed = Date.parse(trimmed);
  if (Number.isNaN(parsed)) {
    return trimmed.slice(0, 10);
  }

  return new Date(parsed).toISOString();
}

export function filterChipLabel(
  field: string,
  header: string,
  value: GridFilterValue,
  locale: "fa" | "en",
  options?: FilterChipOptions,
): string {
  const enumLabels = options?.enumLabels ?? {};
  const formatNum = (n: number) => n.toLocaleString(locale === "fa" ? "fa-IR" : "en-US");

  switch (value.kind) {
    case "text":
      return `${header}: ${value.query}`;
    case "number": {
      if (value.operator === "between" && value.valueTo != null) {
        return `${header}: ${formatNum(value.value)} ${locale === "fa" ? "تا" : "to"} ${formatNum(value.valueTo)}`;
      }
      const ops = filterOperatorLabelsFor(locale);
      const opLabel =
        value.operator === "greaterThan" || value.operator === "greaterThanOrEqual"
          ? ops.greaterThan
          : value.operator === "lessThan" || value.operator === "lessThanOrEqual"
            ? ops.lessThan
            : ops.equals;
      return `${header}: ${opLabel} ${formatNum(value.value)}`;
    }
    case "money": {
      const amount = value.money.amount;
      const amountTo = value.money.amountTo;
      const currency = value.money.currency || (locale === "fa" ? "تومان" : "");
      if (value.operator === "between" && amountTo != null) {
        return `${header}: ${formatNum(amount)} ${locale === "fa" ? "تا" : "to"} ${formatNum(amountTo)} ${currency}`.trim();
      }
      return `${header}: ${formatNum(amount)} ${currency}`.trim();
    }
    case "date": {
      const from = formatJalaliDate(value.iso, locale);
      if (value.operator === "between" && value.isoTo) {
        const to = formatJalaliDate(value.isoTo, locale);
        return `${header}: ${from} ${locale === "fa" ? "تا" : "to"} ${to}`;
      }
      const ops = filterOperatorLabelsFor(locale);
      const opLabel =
        value.operator === "before" ? ops.before : value.operator === "after" ? ops.after : ops.on;
      return `${header}: ${opLabel} ${from}`;
    }
    case "enum":
    case "status": {
      const labels = value.values.map((v) => enumLabels[v] ?? v);
      return `${header}: ${labels.join(locale === "fa" ? "، " : ", ")}`;
    }
    default:
      return header;
  }
}
