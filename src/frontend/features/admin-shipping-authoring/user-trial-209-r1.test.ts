/**
 * TB-209-R1 focused regressions: exact server-error placement, truthful view rows and the
 * handler-boundary mutation guard. Pure logic + narrow source guards; no network, no snapshot.
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import { authoringMessages } from "./messages/authoring-messages.ts";
import { serverErrorCodes, serverFieldIssues, targetTab } from "./ui/server-field-error.ts";

const FEATURE_DIR = dirname(fileURLToPath(import.meta.url));
const messages = authoringMessages("fa");

function validation(errors: readonly { property: string; codes: readonly string[] }[], errorCode: string | null = null) {
  return { kind: "validation" as const, status: 400, errorCode, errors } as never;
}

test("description is owned by the description field, never by name", () => {
  const issues = serverFieldIssues(
    validation([{ property: "methodTranslations[0].description", codes: ["shipping.authoring.presentation_payload_invalid"] }]),
    messages,
  );
  assert.equal(issues.length, 1);
  assert.equal(issues[0]!.target, "description");
  assert.equal(issues[0]!.testId, "shipping-authoring-description");
});

test("a nested rate deliveryDays path targets that RATE ROW, not the weekly schedule", () => {
  const issues = serverFieldIssues(
    validation([{ property: "regionGroups[0].rows[0].minDeliveryDays", codes: ["shipping.authoring.rate_duration_invalid"] }]),
    messages,
  );
  assert.equal(issues[0]!.target, "rates");
  assert.equal(issues[0]!.rowIndex, 0);
  assert.equal(issues[0]!.testId, "shipping-authoring-rate-min-days");
  assert.equal(issues[0]!.reason, messages.rateDurationServerError);
  assert.equal(targetTab(issues[0]!.target), "regions");
});

test("the rejected row index and its own input are preserved for focus", () => {
  const issues = serverFieldIssues(
    validation([
      { property: "regionGroups[0].rows[2].freightAmount", codes: ["shipping.authoring.rate_amount_invalid"] },
      { property: "regionGroups[0].rows[1].maxWeightGrams", codes: ["shipping.authoring.rate_condition_invalid"] },
    ]),
    messages,
  );
  assert.deepEqual(
    issues.map((issue) => [issue.rowIndex, issue.testId]),
    [
      [2, "shipping-authoring-rate-freight"],
      [1, "shipping-authoring-rate-max-weight"],
    ],
  );
  assert.equal(issues[0]!.field.endsWith("3"), true, "row number in the label is 1-based");
});

test("timezone.required and timezone.invalid stay distinct and land on the time zone", () => {
  const required = serverFieldIssues(
    validation([], "shipping.schedule.timezone.required"),
    messages,
  );
  const invalid = serverFieldIssues(
    validation([{ property: "weeklySchedule.timeZoneId", codes: ["shipping.schedule.timezone.invalid"] }]),
    messages,
  );
  assert.equal(required[0]!.target, "weekTimeZone");
  assert.equal(required[0]!.reason, messages.weekTimeZoneRequired);
  assert.equal(invalid[0]!.target, "weekTimeZone");
  assert.equal(invalid[0]!.reason, messages.weekTimeZoneInvalidServer);
  assert.notEqual(required[0]!.reason, invalid[0]!.reason);
});

test("an unknown code containing name/rate/time/type is never explained by a substring", () => {
  const issues = serverFieldIssues(
    validation([], "vendor.experimental.name.rate.time.type.check"),
    messages,
  );
  assert.equal(issues[0]!.target, "unknown");
  assert.equal(issues[0]!.reason, messages.fieldRejectedServer);
  for (const invented of [messages.nameRequired, messages.rateAmountServerError, messages.weekTimeZoneInvalidServer, messages.typeRejectedServer]) {
    assert.notEqual(issues[0]!.reason, invented);
  }
});

test("a known property keeps its own section even when the code is unregistered", () => {
  const issues = serverFieldIssues(
    validation([{ property: "regionGroups[0].rows[0].minDeliveryDays", codes: ["vendor.custom"] }]),
    messages,
  );
  assert.equal(issues[0]!.target, "rates");
  assert.equal(issues[0]!.reason, messages.fieldRejectedServer);
});

test("technical codes stay out of prose but remain available for diagnostics", () => {
  const failure = validation([], "shipping.schedule.timezone.invalid");
  assert.deepEqual(serverErrorCodes(failure), ["shipping.schedule.timezone.invalid"]);
});

test("view mode cannot render an editable rate row, and every mutation is guarded at the handler", () => {
  const screen = readFileSync(join(FEATURE_DIR, "ui", "authoring-screen.tsx"), "utf8");
  const groups = readFileSync(join(FEATURE_DIR, "ui", "authoring-groups-editor.tsx"), "utf8");
  assert.match(screen, /mutationBlocked\.current = mode !== "edit" \|\| session\.save === "saving" \|\| session\.reconcile === "pending"/);
  assert.ok((screen.match(/blockedNow\(\)/g) ?? []).length >= 10, "each mutation callback is guarded");
  // TB-225: the row list is a READ-ONLY summary in view mode, and the only write path is the isolated modal.
  assert.match(groups, /<RateRowSummary/);
  assert.match(groups, /data-testid="shipping-authoring-rate-row-edit"/);
  assert.match(groups, /<AuthoringRateModal/);
  assert.doesNotMatch(groups, /MoneyField/, "no money input is rendered inline in the area card");
  assert.match(groups, /storedTitle\?\.title \?\? titleEntry\?\.title/);
  assert.match(groups, /data-testid="shipping-authoring-group-title-text"/, "view shows the stored title as text");
});

test("optional conditions are edited in the modal, with no disclosure mutation path", () => {
  const rates = readFileSync(join(FEATURE_DIR, "ui", "authoring-rates-editor.tsx"), "utf8");
  const modal = readFileSync(join(FEATURE_DIR, "ui", "authoring-rate-modal.tsx"), "utf8");
  // The row summary always states the ACTIVE condition, so a collapsed editor never hides a configured axis.
  assert.match(rates, /rateDraftConditionText\(row, locale, messages\)/);
  const conditions = modal.slice(modal.indexOf("messages.conditionsToggle"));
  assert.ok(conditions.length > 0);
  assert.doesNotMatch(conditions, /onToggle=/);
  assert.match(conditions, /testId="shipping-authoring-rate-min-weight"/);
});

test("time zone suggestions do not claim to be market-derived", () => {
  const options = readFileSync(join(FEATURE_DIR, "ui", "time-zone-options.ts"), "utf8");
  assert.doesNotMatch(options, /by relevance to the configured markets/);
  assert.match(options, /NOT derived from configured markets/);
});
