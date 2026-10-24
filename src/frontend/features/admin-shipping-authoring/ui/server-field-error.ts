/**
 * Exact placement of a REJECTED save on the local field that actually caused it.
 *
 * The server is the authority: this module never guesses a cause, never turns a failure into success
 * and never hides the technical code (it is offered in a collapsed diagnostics disclosure). Placement
 * uses TWO independent, explicit facts only:
 * 1. the registered property path (`regionGroups[0].rows[0].minDeliveryDays`) — its own segment
 *    ownership decides the section, the row and the input;
 * 2. an exact registered error code (`shipping.schedule.timezone.invalid`) — string equality, never a
 *    substring guess, so an unknown code can never be "explained" as a required name or bad time.
 *
 * A code outside the registered set keeps the honest generic reason of its owning field.
 */
import type { EditorPropertyError, EditorTransportFailure } from "../api/editor-document-api.ts";
import { authoringFormat, type AuthoringMessages } from "../messages/authoring-messages.ts";

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
  /** The server property path, when the rejection carried one. */
  readonly property: string | null;
  /** Index of the rejected GROUP inside the submitted `regionGroups`, when the path owns one. */
  readonly groupIndex: number | null;
  /** Index of the rejected rate row inside the submitted `regionGroups[i].rows`, if any. */
  readonly rowIndex: number | null;
  /** `data-testid` of the input to focus, when the screen renders one. */
  readonly testId: string | null;
}

/** True when this issue is scoped to one rate row of one group. */
export function isRowScoped(issue: ServerFieldIssue): boolean {
  return issue.target === "rates" && issue.groupIndex !== null && issue.rowIndex !== null;
}

/** Composite key of the rejected row: BOTH indices, so equal row indices in two groups stay distinct. */
export function rateRowKey(groupIndex: number, rowIndex: number): string {
  return `${groupIndex}:${rowIndex}`;
}

/**
 * The scoped DOM selector of exactly one rejected input: intended GROUP element, then intended ROW
 * element (when row-scoped), then the input itself. Never a document-wide Nth-match shortcut — that
 * would silently target another group's row.
 */
export function serverIssueSelector(issue: ServerFieldIssue): string | null {
  if (issue.testId === null) return null;
  const field = `[data-testid="${issue.testId}"]`;
  if (issue.groupIndex === null) return field;
  const group = `[data-testid="shipping-authoring-group-draft"][data-group-index="${issue.groupIndex}"]`;
  if (issue.rowIndex === null) return `${group} ${field}`;
  const row = `[data-testid="shipping-authoring-rate-row-edit"][data-row-index="${issue.rowIndex}"]`;
  return `${group} ${row} ${field}`;
}

/** TAB that owns each target, so the screen can reveal the failing section. */
export function targetTab(target: ServerFieldTarget): "general" | "regions" {
  return target === "regions" || target === "rates" ? "regions" : "general";
}

/** Static section/input ids per target (row-level rate inputs are resolved per property). */
const STATIC_TEST_ID: Readonly<Record<ServerFieldTarget, string | null>> = {
  name: "shipping-authoring-name",
  description: "shipping-authoring-description",
  settings: "shipping-authoring-freight-Prepaid",
  type: "shipping-authoring-type",
  weekTimeZone: "shipping-authoring-week-timezone",
  weekDays: "shipping-authoring-week",
  regions: "shipping-authoring-groups-panel",
  rates: "shipping-authoring-rate-row-edit",
  unknown: null,
};

/** Exact registered `regionGroups[i].rows[j].<segment>` → the row input test id. */
const ROW_FIELD_TEST_ID: Readonly<Record<string, string>> = {
  freightAmount: "shipping-authoring-rate-freight",
  packagingAmount: "shipping-authoring-rate-packaging",
  minDeliveryDays: "shipping-authoring-rate-min-days",
  maxDeliveryDays: "shipping-authoring-rate-max-days",
  minWeightGrams: "shipping-authoring-rate-min-weight",
  maxWeightGrams: "shipping-authoring-rate-max-weight",
  minItemCount: "shipping-authoring-rate-min-count",
  maxItemCount: "shipping-authoring-rate-max-count",
  minMerchandiseAmount: "shipping-authoring-rate-min-amount",
  maxMerchandiseAmount: "shipping-authoring-rate-max-amount",
};

/**
 * EXACT registered codes → reason + the section they belong to.
 *
 * No entry here is derived from a substring; a code that is not listed is treated as unknown.
 */
const REGISTERED: Readonly<Record<string, { target: ServerFieldTarget; key: keyof AuthoringMessages }>> = {
  "shipping.schedule.timezone.required": { target: "weekTimeZone", key: "weekTimeZoneRequired" },
  "shipping.schedule.timezone.invalid": { target: "weekTimeZone", key: "weekTimeZoneInvalidServer" },
  "shipping.validation.schedule_timezone_id_invalid": { target: "weekTimeZone", key: "weekTimeZoneInvalidServer" },
  "shipping.schedule.weekday.invalid": { target: "weekDays", key: "weekDayRangeInvalid" },
  "shipping.schedule.window.range_invalid": { target: "weekDays", key: "weekDayRangeInvalid" },
  "shipping.schedule.window.overlap": { target: "weekDays", key: "weekDayRangeInvalid" },
  "shipping.authoring.schedule_weekday_set_invalid": { target: "weekDays", key: "weekDayRangeInvalid" },
  "shipping.authoring.schedule_day_invalid": { target: "weekDays", key: "weekDayTimeInvalid" },
  "shipping.authoring.rate_amount_invalid": { target: "rates", key: "rateAmountServerError" },
  "shipping.authoring.rate_duration_invalid": { target: "rates", key: "rateDurationServerError" },
  "shipping.authoring.rate_condition_invalid": { target: "rates", key: "rateConditionServerError" },
  "shipping.authoring.rate_row_payload_invalid": { target: "rates", key: "rateRowServerError" },
  "shipping.authoring.rate_overlap": { target: "rates", key: "rateRowServerError" },
  "shipping.authoring.rate_currency_mismatch": { target: "rates", key: "rateRowServerError" },
  "shipping.authoring.region_payload_invalid": { target: "regions", key: "fieldRejectedServer" },
  "shipping.authoring.region_selection_overlap": { target: "regions", key: "fieldRejectedServer" },
  "shipping.authoring.region_group_not_found": { target: "regions", key: "fieldRejectedServer" },
  "shipping.authoring.presentation_payload_invalid": { target: "name", key: "fieldRejectedServer" },
  "shipping.authoring.presentation_language_invalid": { target: "name", key: "fieldRejectedServer" },
  "shipping.authoring.method_settings_invalid": { target: "settings", key: "fieldRejectedServer" },
  "shipping.authoring.type_assignment_invalid": { target: "type", key: "typeRejectedServer" },
  "shipping.authoring.type_selection_unavailable": { target: "type", key: "typeRejectedServer" },
};

interface ParsedPath {
  readonly target: ServerFieldTarget;
  readonly segment: string;
  readonly groupIndex: number | null;
  readonly rowIndex: number | null;
  readonly testId: string | null;
}

/** One path segment split into its registered name and its collection index (if any). */
function segmentOf(segment: string): { name: string; index: number | null } {
  const match = /^([A-Za-z_][A-Za-z0-9_]*)(?:\[(\d+)\])?$/.exec(segment.trim());
  if (!match) return { name: "", index: null };
  return { name: match[1]!, index: match[2] === undefined ? null : Number(match[2]) };
}

/** Ownership of one registered property path, decided by its own segments (never by a substring). */
function parseProperty(property: string): ParsedPath | null {
  const segments = property
    .split(".")
    .map(segmentOf)
    .filter((segment) => segment.name.length > 0);
  if (segments.length === 0) return null;
  const first = segments[0]!;
  const last = segments[segments.length - 1]!;
  const parent = segments.length > 1 ? segments[segments.length - 2]! : null;

  if (first.name === "regionGroups" && parent?.name === "rows" && parent.index !== null) {
    const testId = ROW_FIELD_TEST_ID[last.name] ?? "shipping-authoring-rate-row-edit";
    return { target: "rates", segment: last.name, groupIndex: first.index, rowIndex: parent.index, testId };
  }
  if (first.name === "regionGroups") {
    return {
      target: "regions",
      segment: last.name,
      groupIndex: first.index,
      rowIndex: null,
      testId: STATIC_TEST_ID.regions,
    };
  }
  if (first.name === "weeklySchedule") {
    if (last.name === "timeZoneId") {
      return {
        target: "weekTimeZone",
        segment: last.name,
        groupIndex: null,
        rowIndex: null,
        testId: STATIC_TEST_ID.weekTimeZone,
      };
    }
    return { target: "weekDays", segment: last.name, groupIndex: null, rowIndex: null, testId: STATIC_TEST_ID.weekDays };
  }
  if (first.name === "methodTranslations") {
    const target: ServerFieldTarget = last.name === "description" ? "description" : "name";
    return { target, segment: last.name, groupIndex: null, rowIndex: null, testId: STATIC_TEST_ID[target] };
  }
  if (first.name === "methodSettings") {
    return { target: "settings", segment: last.name, groupIndex: null, rowIndex: null, testId: STATIC_TEST_ID.settings };
  }
  if (first.name === "typeAssignment") {
    return { target: "type", segment: last.name, groupIndex: null, rowIndex: null, testId: STATIC_TEST_ID.type };
  }
  return { target: "unknown", segment: last.name, groupIndex: null, rowIndex: null, testId: null };
}

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
 * Human name of the rejection, scoped by the indices the path actually carried: group, then row, so
 * two groups' first rows read differently and no index is flattened away.
 */
function fieldName(
  target: ServerFieldTarget,
  groupIndex: number | null,
  rowIndex: number | null,
  messages: AuthoringMessages,
): string {
  const base = targetFieldLabel(target, messages);
  const parts: string[] = [];
  if (groupIndex !== null) parts.push(authoringFormat(messages.groupTitle, { index: String(groupIndex + 1) }));
  if (rowIndex !== null) parts.push(authoringFormat(messages.rowLabel, { index: String(rowIndex + 1) }));
  return parts.length === 0 ? base : `${base} — ${parts.join(" · ")}`;
}

function codeReason(code: string | null, messages: AuthoringMessages): string | null {
  if (code === null) return null;
  const entry = REGISTERED[code];
  return entry ? messages[entry.key] : null;
}

/**
 * The actionable issues of one rejected save, de-duplicated by (target, row, reason).
 *
 * @param failure The typed transport failure of the rejected PUT.
 * @param messages Localized messages of the interface locale.
 */
export function serverFieldIssues(
  failure: EditorTransportFailure,
  messages: AuthoringMessages,
): readonly ServerFieldIssue[] {
  const groups: readonly EditorPropertyError[] =
    failure.kind === "conflict" || failure.kind === "validation" || failure.kind === "httpError"
      ? failure.errors
      : [];
  const issues: ServerFieldIssue[] = [];
  const seen = new Set<string>();

  for (const group of groups) {
    const code = group.codes.find((candidate) => candidate.length > 0) ?? null;
    const parsed = parseProperty(group.property);
    const registered = code === null ? null : REGISTERED[code];
    // The path owns the SECTION/ROW; the exact registered code owns the REASON. A path this screen
    // cannot own stays `unknown` rather than being attached to a plausible-looking field.
    const target = parsed?.target ?? registered?.target ?? "unknown";
    const rowIndex = parsed?.rowIndex ?? null;
    const groupIndex = parsed?.groupIndex ?? null;
    const issue: ServerFieldIssue = {
      target,
      field: fieldName(target, groupIndex, rowIndex, messages),
      reason: codeReason(code, messages) ?? messages.fieldRejectedServer,
      property: group.property,
      groupIndex,
      rowIndex,
      testId: parsed?.testId ?? STATIC_TEST_ID[target],
    };
    // BOTH indices and the owning input are part of identity: equal row indices in two groups are two
    // real rejections, never one collapsed line.
    const key = `${issue.target}|${issue.groupIndex}|${issue.rowIndex}|${issue.testId}|${issue.reason}`;
    if (seen.has(key)) continue;
    seen.add(key);
    issues.push(issue);
  }

  if (issues.length === 0 && (failure.kind === "validation" || failure.kind === "httpError")) {
    const rootCode = failure.errorCode;
    const registered = rootCode === null ? null : REGISTERED[rootCode];
    const target = registered?.target ?? "unknown";
    issues.push({
      target,
      field: fieldName(target, null, null, messages),
      reason: codeReason(rootCode, messages) ?? messages.fieldRejectedServer,
      property: null,
      groupIndex: null,
      rowIndex: null,
      testId: STATIC_TEST_ID[target],
    });
  }
  return issues;
}

/** The technical codes of a rejection, for the collapsed diagnostics disclosure only. */
export function serverErrorCodes(failure: EditorTransportFailure): readonly string[] {
  const codes = new Set<string>();
  if (failure.kind === "conflict" || failure.kind === "validation" || failure.kind === "httpError") {
    for (const group of failure.errors) for (const code of group.codes) codes.add(code);
  }
  if ("errorCode" in failure && failure.errorCode) codes.add(failure.errorCode);
  return [...codes];
}
