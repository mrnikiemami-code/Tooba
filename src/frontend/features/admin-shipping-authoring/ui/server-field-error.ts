/**
 * One actionable mapping from a REJECTED save to the local field that actually caused it.
 *
 * The server is the authority: this module never guesses a cause, never turns a failure into success
 * and never hides the technical code. It only translates the codes/properties the server really
 * returns into (a) the section of this screen, (b) the human field name, and (c) a localized reason,
 * so the operator is not left with a generic "some fields were rejected".
 *
 * Two real shapes are covered:
 * 1. registered property errors (`errors: { "<property>": ["<code>"] }`) from the validation pipeline;
 * 2. a single root `errorCode` for domain rules (for example `shipping.schedule.timezone.invalid`).
 */
import type { EditorPropertyError, EditorTransportFailure } from "../api/editor-document-api.ts";
import type { AuthoringMessages } from "../messages/authoring-messages.ts";

/** The local target a server rejection points at (only fields this screen really owns). */
export type ServerFieldTarget =
  | "name"
  | "description"
  | "settings"
  | "type"
  | "weekTimeZone"
  | "weekDays"
  | "regions"
  | "rates"
  | "unknown";

/** One localized, actionable rejection line. */
export interface ServerFieldIssue {
  readonly target: ServerFieldTarget;
  /** Localized human field name. */
  readonly field: string;
  /** Localized human reason. */
  readonly reason: string;
}

/** TAB that owns each target, so the screen can reveal the failing section. */
export function targetTab(target: ServerFieldTarget): "general" | "regions" {
  return target === "regions" || target === "rates" ? "regions" : "general";
}

/** `data-testid` of the input that receives focus for a target, when the screen renders one. */
export const TARGET_TEST_ID: Readonly<Record<ServerFieldTarget, string | null>> = {
  name: "shipping-authoring-name",
  description: "shipping-authoring-description",
  settings: "shipping-authoring-freight-Prepaid",
  type: "shipping-authoring-type",
  weekTimeZone: "shipping-authoring-week-timezone",
  weekDays: "shipping-authoring-week",
  regions: "shipping-authoring-groups-panel",
  rates: "shipping-authoring-group-add",
  unknown: null,
};

/** Root error codes this screen can place on a real field (no invented code is listed here). */
const ROOT_CODE_TARGETS: Readonly<Record<string, ServerFieldTarget>> = {
  "shipping.schedule.timezone.invalid": "weekTimeZone",
  "shipping.schedule.timezone.required": "weekTimeZone",
  "shipping.validation.schedule_timezone_id_invalid": "weekTimeZone",
  "shipping.schedule.day.time_invalid": "weekDays",
  "shipping.schedule.day.range_invalid": "weekDays",
};

/** Property-path → target, checked in order (most specific first). */
const PROPERTY_TARGETS: readonly (readonly [RegExp, ServerFieldTarget])[] = [
  [/timezone|time_zone/i, "weekTimeZone"],
  [/schedule|weekday|days?\b|starttime|endtime/i, "weekDays"],
  [/description/i, "name"],
  [/shippingtype|typeassignment|\btype\b/i, "type"],
  [/freightamount|packagingamount|delivery|rows?\b|rate/i, "rates"],
  [/regiongroup|areas?\b|country|city|servicearea|administrative/i, "regions"],
  [/settings|freightpay|calculation/i, "settings"],
  [/name/i, "name"],
];

/** Localized human name of one target. */
export function targetFieldLabel(target: ServerFieldTarget, messages: AuthoringMessages): string {
  switch (target) {
    case "name":
      return messages.fieldNameLabel;
    case "description":
      return messages.fieldDescriptionLabel;
    case "settings":
      return messages.fieldSettingsLabel;
    case "type":
      return messages.fieldTypeLabel;
    case "weekTimeZone":
      return messages.fieldWeekTimeZoneLabel;
    case "weekDays":
      return messages.fieldWeekDaysLabel;
    case "regions":
      return messages.fieldRegionsLabel;
    case "rates":
      return messages.fieldRatesLabel;
    case "unknown":
      return messages.saveValidation;
  }
}

/**
 * Localized reason for one server code.
 *
 * Unknown codes are NOT translated into an invented cause: the registered field name is shown and the
 * technical code stays available in the diagnostics note.
 */
function reasonFor(code: string | null, target: ServerFieldTarget, messages: AuthoringMessages): string {
  if (code === null) return messages.saveValidation;
  if (/timezone|time_zone/i.test(code)) return messages.weekTimeZoneInvalidServer;
  if (/required/i.test(code) && target === "weekTimeZone") return messages.weekTimeZoneRequired;
  if (/time|schedule|weekday/i.test(code)) return messages.weekDayTimeInvalid;
  if (/conflict/i.test(code)) return messages.saveConflict;
  if (/name/i.test(code)) return messages.nameRequired;
  if (/type/i.test(code)) return messages.typeRejectedServer;
  if (/amount|decimal|rate/i.test(code)) return messages.amountInvalid;
  return messages.saveValidation;
}

function propertyTarget(property: string): ServerFieldTarget {
  for (const [pattern, target] of PROPERTY_TARGETS) {
    if (pattern.test(property)) return target;
  }
  return "unknown";
}

/**
 * The actionable issues of one rejected save, in server order and de-duplicated by (target, reason).
 *
 * @param failure The typed transport failure of the rejected PUT.
 * @param messages Localized messages of the interface locale.
 */
export function serverFieldIssues(
  failure: EditorTransportFailure,
  messages: AuthoringMessages,
): readonly ServerFieldIssue[] {
  const issues: ServerFieldIssue[] = [];
  const seen = new Set<string>();
  const push = (target: ServerFieldTarget, reason: string): void => {
    const key = `${target}|${reason}`;
    if (seen.has(key)) return;
    seen.add(key);
    issues.push({ target, field: targetFieldLabel(target, messages), reason });
  };

  const groups: readonly EditorPropertyError[] =
    failure.kind === "conflict" || failure.kind === "validation" || failure.kind === "httpError"
      ? failure.errors
      : [];
  for (const group of groups) {
    const target = propertyTarget(group.property);
    const code = group.codes.find((candidate) => candidate.length > 0) ?? null;
    push(target, reasonFor(code, target, messages));
  }

  if (issues.length === 0 && (failure.kind === "validation" || failure.kind === "httpError")) {
    const rootCode = failure.errorCode;
    const target = rootCode !== null ? (ROOT_CODE_TARGETS[rootCode] ?? "unknown") : "unknown";
    push(target, reasonFor(rootCode, target, messages));
  }
  return issues;
}

/** The technical codes of a rejection, for the small diagnostics note (never the main prose). */
export function serverErrorCodes(failure: EditorTransportFailure): readonly string[] {
  const codes = new Set<string>();
  if (failure.kind === "conflict" || failure.kind === "validation" || failure.kind === "httpError") {
    for (const group of failure.errors) for (const code of group.codes) codes.add(code);
  }
  if ("errorCode" in failure && failure.errorCode) codes.add(failure.errorCode);
  return [...codes];
}
