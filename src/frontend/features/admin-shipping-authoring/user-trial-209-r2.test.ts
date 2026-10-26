/**
 * TB-209-R2 focused regression: a rejected rate row is owned by BOTH its group and its row.
 *
 * Two-group fixture: group0 has two rows, group1 has one. Every assertion is about the actual submitted
 * property path, so a flattened row index can never pass.
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";
import { authoringMessages } from "./messages/authoring-messages.ts";
import {
  isRowScoped,
  rateRowKey,
  serverFieldIssues,
  serverIssueSelector,
  type ServerFieldIssue,
} from "./ui/server-field-error.ts";
import type { EditorTransportFailure } from "./api/editor-document-api.ts";

const FEATURE_DIR = dirname(fileURLToPath(import.meta.url));
const messages = authoringMessages("fa");

function failure(errors: readonly { property: string; codes: readonly string[] }[], errorCode: string | null = null) {
  return { kind: "validation", status: 400, errorCode, errors } as unknown as EditorTransportFailure;
}

/** Rejection of group1 row0 freight — the exact case that used to target group0 row0. */
const CROSS_GROUP = failure([
  { property: "regionGroups[1].rows[0].freightAmount", codes: ["shipping.authoring.rate_amount_invalid"] },
]);

test("R2-01 a rejection in group1 keeps groupIndex 1 and rowIndex 0", () => {
  const issues = serverFieldIssues(CROSS_GROUP, messages);
  assert.equal(issues.length, 1);
  const [issue] = issues as readonly ServerFieldIssue[];
  assert.equal(issue!.target, "rates");
  assert.equal(issue!.groupIndex, 1);
  assert.equal(issue!.rowIndex, 0);
  assert.equal(issue!.testId, "shipping-authoring-rate-freight");
  assert.equal(isRowScoped(issue!), true);
});

test("R2-02 the focus selector is scoped group1 -> row0 -> freight, never a global Nth match", () => {
  const [issue] = serverFieldIssues(CROSS_GROUP, messages) as readonly ServerFieldIssue[];
  assert.equal(
    serverIssueSelector(issue!),
    '[data-testid="shipping-authoring-group-draft"][data-group-index="1"] ' +
      '[data-testid="shipping-authoring-rate-row-edit"][data-row-index="0"] ' +
      '[data-testid="shipping-authoring-rate-freight"]',
  );
});

test("R2-03 group0 row1 delivery stays on group0 row1 while group1 row0 freight stays separate", () => {
  const issues = serverFieldIssues(
    failure([
      { property: "regionGroups[0].rows[1].minDeliveryDays", codes: ["shipping.authoring.rate_duration_invalid"] },
      { property: "regionGroups[1].rows[0].freightAmount", codes: ["shipping.authoring.rate_amount_invalid"] },
    ]),
    messages,
  );
  assert.deepEqual(
    issues.map((issue) => [issue.groupIndex, issue.rowIndex, issue.testId]),
    [
      [0, 1, "shipping-authoring-rate-min-days"],
      [1, 0, "shipping-authoring-rate-freight"],
    ],
  );
  assert.deepEqual(
    issues.map((issue) => issue.reason),
    [messages.rateDurationServerError, messages.rateAmountServerError],
  );
});

test("R2-04 equal code and row0 in two groups yields TWO issues, not one collapsed line", () => {
  const issues = serverFieldIssues(
    failure([
      { property: "regionGroups[0].rows[0].freightAmount", codes: ["shipping.authoring.rate_amount_invalid"] },
      { property: "regionGroups[1].rows[0].freightAmount", codes: ["shipping.authoring.rate_amount_invalid"] },
    ]),
    messages,
  );
  assert.equal(issues.length, 2);
  assert.deepEqual(
    issues.map((issue) => rateRowKey(issue.groupIndex!, issue.rowIndex!)),
    ["0:0", "1:0"],
  );
  assert.notEqual(issues[0]!.field, issues[1]!.field, "the two lines stay distinguishable in prose");
});

test("R2-05 composite keys never collide and are not the bare row index", () => {
  assert.notEqual(rateRowKey(0, 1), rateRowKey(1, 0));
  assert.notEqual(rateRowKey(0, 0), rateRowKey(1, 0));
  assert.equal(rateRowKey(2, 3), "2:3");
});

test("R2-06 an out-of-range or absent index is kept honestly, never redirected to row0", () => {
  const outOfRange = serverFieldIssues(
    failure([{ property: "regionGroups[9].rows[7].maxWeightGrams", codes: ["shipping.authoring.rate_condition_invalid"] }]),
    messages,
  );
  const [issue] = outOfRange as readonly ServerFieldIssue[];
  assert.equal(issue!.groupIndex, 9);
  assert.equal(issue!.rowIndex, 7);
  assert.notEqual(issue!.groupIndex, 0);
  // The screen finds nothing for it and stays silent rather than highlighting an unrelated row.
  assert.equal(
    serverIssueSelector(issue!),
    '[data-testid="shipping-authoring-group-draft"][data-group-index="9"] ' +
      '[data-testid="shipping-authoring-rate-row-edit"][data-row-index="7"] ' +
      '[data-testid="shipping-authoring-rate-max-weight"]',
  );
});

test("R2-07 a group-level path (no row) scopes to the group container, not to a row", () => {
  const [issue] = serverFieldIssues(
    failure([{ property: "regionGroups[1].areas", codes: ["shipping.authoring.region_payload_invalid"] }]),
    messages,
  ) as readonly ServerFieldIssue[];
  assert.equal(issue!.target, "regions");
  assert.equal(issue!.groupIndex, 1);
  assert.equal(issue!.rowIndex, null);
  assert.doesNotMatch(serverIssueSelector(issue!) ?? "", /rate-row-edit/);
});

test("R2-08 non-region paths stay global (no invented group scoping)", () => {
  const [issue] = serverFieldIssues(
    failure([{ property: "methodTranslations[0].description", codes: ["shipping.authoring.presentation_payload_invalid"] }]),
    messages,
  ) as readonly ServerFieldIssue[];
  assert.equal(issue!.groupIndex, null);
  assert.equal(serverIssueSelector(issue!), '[data-testid="shipping-authoring-description"]');
});

test("R2-09 the screen opens ONLY the enclosing disclosure and never edits a value", () => {
  const screen = readFileSync(join(FEATURE_DIR, "ui", "authoring-screen.tsx"), "utf8");
  // Scoped lookup, not a document-wide Nth match.
  assert.match(screen, /querySelector<HTMLElement>\(selector\)/);
  assert.doesNotMatch(screen, /querySelectorAll<HTMLElement>\(`\[data-testid=/);
  // Walk up from the target and open the details elements that contain it.
  assert.match(screen, /parent instanceof HTMLDetailsElement && !parent\.open/);
  const reveal = screen.slice(screen.indexOf("const revealServerIssue"), screen.indexOf("const revealedFailure"));
  assert.doesNotMatch(reveal, /onChange|commit\(|blockedNow|\.value =/, "revealing is presentation only");
});

test("R2-10 group and row ownership is carried into the rendered attributes and map lookup", () => {
  const groups = readFileSync(join(FEATURE_DIR, "ui", "authoring-groups-editor.tsx"), "utf8");
  const rates = readFileSync(join(FEATURE_DIR, "ui", "authoring-rates-editor.tsx"), "utf8");
  const modal = readFileSync(join(FEATURE_DIR, "ui", "authoring-rate-modal.tsx"), "utf8");
  assert.match(groups, /data-group-index=\{position\}/);
  assert.match(groups, /serverRateErrors\?\.get\(rateRowKey\(position, rowIndex\)\)/);
  // TB-225: the editable row is a summary whose ONLY write path is the modal, keyed by the stable row key.
  assert.match(rates, /data-testid="shipping-authoring-rate-row"/);
  // TB-225-R1: the edit modal receives the EXACT stored tokens and re-derives fraction tolerance on confirm.
  assert.match(rates, /storedRateAmounts\(/);
  assert.match(modal, /rateConfirmIssue\(row, otherRows, currencyCode, storedAmounts, editable\)/);
  // Self-exclusion by stable key now lives in the ONE confirm decision the modal calls.
  const ratesModel = readFileSync(join(FEATURE_DIR, "model", "editor-rates.ts"), "utf8");
  assert.match(ratesModel, /other\.rowKey !== row\.rowKey/, "self is excluded by stable key, never by index");
  assert.doesNotMatch(groups, /serverRateErrors\?\.get\(rowIndex\)/);
});
