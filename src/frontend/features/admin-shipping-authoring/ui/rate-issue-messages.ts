/**
 * Localized text of every local rate-row issue and of the shared area range.
 *
 * Kept free of React and of the editors so the row summary, the area card and the isolated rate modal can
 * all localize the SAME code without importing each other.
 */
import type { AuthoringMessages } from "../messages/authoring-messages.ts";
import type { RateRowIssue } from "../model/editor-rates.ts";
import type { AreaRangeDraftIssueCode } from "../model/editor-area-range.ts";

/** Issue message of one row (first blocking reason). */
export function rowIssueMessage(code: RateRowIssue["code"], messages: AuthoringMessages): string {
  switch (code) {
    case "rowLimitExceeded":
      return messages.rowLimitExceeded;
    case "boundIncomplete":
      return messages.boundIncomplete;
    case "integerInvalid":
      return messages.integerInvalid;
    case "boundRangeInvalid":
      return messages.boundRangeInvalid;
    case "amountRequired":
      return messages.amountRequired;
    case "amountInvalid":
      return messages.amountInvalid;
    case "rialFractionInvalid":
      return messages.rialFractionInvalid;
    case "overlapConflict":
      return messages.rateOverlapConflict;
    case "durationRequired":
      return messages.durationRequired;
    case "durationInvalid":
      return messages.durationInvalid;
  }
}

/** Issue message of the shared area range. */
export function areaRangeIssueMessage(code: AreaRangeDraftIssueCode, messages: AuthoringMessages): string {
  if (code === "durationRequired") return messages.durationRequired;
  if (code === "durationInvalid") return messages.durationInvalid;
  return messages.areaRangeRowsRequired;
}
