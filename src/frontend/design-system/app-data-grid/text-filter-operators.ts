import type { TextFilterOperator } from "../data-grid/types";
import type { FilterOperatorLabels } from "../data-grid/messages";

/** عملگرهای استاندارد فیلتر متنی — هم‌تراز با whitelist بک‌اند. */
export const TEXT_OPERATORS: TextFilterOperator[] = [
  "contains",
  "notContains",
  "equals",
  "notEqual",
  "startsWith",
  "endsWith",
  "blank",
  "notBlank",
];

export function textFilterNeedsValue(operator: TextFilterOperator): boolean {
  return operator !== "blank" && operator !== "notBlank";
}

export function textOperatorLabel(operator: TextFilterOperator, ops: FilterOperatorLabels): string {
  switch (operator) {
    case "contains":
      return ops.contains;
    case "notContains":
      return ops.notContains;
    case "equals":
      return ops.equals;
    case "notEqual":
      return ops.notEqual;
    case "startsWith":
      return ops.startsWith;
    case "endsWith":
      return ops.endsWith;
    case "blank":
      return ops.blank;
    case "notBlank":
      return ops.notBlank;
    default:
      return operator;
  }
}
