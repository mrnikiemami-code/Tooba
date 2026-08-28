import type { FilterModel } from "ag-grid-community";
import type { GridFilterValue } from "../data-grid/types";

type AgFilterEntry = {
  filterType?: string;
  type?: string;
  filter?: string | number | null;
  filterTo?: string | number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  values?: string[] | null;
};

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
): "equals" | "greaterThan" | "greaterThanOrEqual" | "lessThan" | "lessThanOrEqual" | "between" {
  switch (type) {
    case "equals":
      return "equals";
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
): string {
  switch (value.kind) {
    case "text":
      return `${header}: ${value.query}`;
    case "number":
      return `${header}: ${value.value}${value.valueTo != null ? `–${value.valueTo}` : ""}`;
    case "date":
      return `${header}: ${value.iso.slice(0, 10)}${value.isoTo ? `–${value.isoTo.slice(0, 10)}` : ""}`;
    case "enum":
    case "status":
      return `${header}: ${value.values.join(locale === "fa" ? "، " : ", ")}`;
    default:
      return header;
  }
}
