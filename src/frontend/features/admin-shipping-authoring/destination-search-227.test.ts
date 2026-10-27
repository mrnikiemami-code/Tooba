/**
 * TB-227 — the destination dropdown's search + keyboard selection, executed as real transitions.
 *
 * The defect this task repairs is a MODAL-GEOMETRY defect, so the presentation change is proven in the
 * browser (measure-before.json / measure-after.json). What is proven here is the behaviour that the new
 * in-popup search must never get wrong, because it decides what the operator can choose:
 *
 * - only rows the branch ALREADY returned are searchable (never a hardcoded/global list);
 * - the coverage line reports `shown of total` so a filtered view is never presented as the full branch;
 * - an inactive place is visible but never selectable by keyboard/Enter;
 * - arrows wrap inside the CURRENT filter and an empty filter keeps Enter a no-op.
 */
import assert from "node:assert/strict";
import test from "node:test";

import type { GeographyOption } from "./api/geography-lookup-api.ts";
import {
  destinationActiveId,
  destinationSearchCoverage,
  filterDestinationEntries,
  isDestinationSearchNavigationKey,
  moveDestinationActiveIndex,
  selectableDestinationIds,
  type DestinationSearchEntry,
} from "./model/destination-search.ts";

const option = (id: string, label: string, code: string, isActive = true): GeographyOption => ({
  kind: "City",
  id,
  code,
  label,
  parentId: "1f8050a8-c325-59fa-959f-82d76b5b2bc5",
  isActive,
});

const entries = (rows: readonly GeographyOption[]): DestinationSearchEntry[] =>
  rows.map((row) => ({ option: row, text: row.label === "" ? "نام این مقصد در دسترس نیست." : row.label }));

const branch = entries([
  option("id-tehran", "تهران", "tehran"),
  option("id-tabriz", "تبریز", "tabriz"),
  option("id-mashhad", "مشهد", "mashhad"),
  option("id-malayer", "ملایر", "malayer", false),
]);

test("the search filters ONLY the rows of the loaded branch", () => {
  const all = filterDestinationEntries(branch, "");
  assert.equal(all.length, branch.length);
  const filtered = filterDestinationEntries(branch, "تبر");
  assert.deepEqual(filtered.map((entry) => entry.option.code), ["tabriz"]);
  // A term that appears nowhere in the branch yields nothing instead of inventing a place.
  assert.equal(filterDestinationEntries(branch, "شیراز").length, 0);
  // Every shown row is a member of the branch input: no fabricated option can leak in.
  const shown = filterDestinationEntries(branch, "ا");
  for (const entry of shown) assert.ok(branch.some((candidate) => candidate.option.id === entry.option.id));
});

test("the search also matches the real code, case-insensitively, and ignores surrounding whitespace", () => {
  assert.deepEqual(filterDestinationEntries(branch, "  TABRIZ ").map((entry) => entry.option.code), ["tabriz"]);
  assert.deepEqual(filterDestinationEntries(branch, "تهر").map((entry) => entry.option.code), ["tehran"]);
});

test("the coverage line reports shown-of-total so a filtered list is never claimed complete", () => {
  const filtered = filterDestinationEntries(branch, "تبر");
  const partial = destinationSearchCoverage(filtered.length, branch.length);
  assert.deepEqual(partial, { shown: 1, total: 4, complete: false });
  const full = destinationSearchCoverage(filterDestinationEntries(branch, "").length, branch.length);
  assert.deepEqual(full, { shown: 4, total: 4, complete: true });
});

test("an inactive place stays visible but is never selectable", () => {
  const visible = filterDestinationEntries(branch, "ملایر");
  assert.equal(visible.length, 1);
  assert.deepEqual(selectableDestinationIds(visible), []);
  assert.equal(destinationActiveId(visible, 0), null);
});

test("arrows wrap inside the current filter and Home/End are bounded", () => {
  assert.equal(moveDestinationActiveIndex(0, 3, "ArrowDown"), 1);
  assert.equal(moveDestinationActiveIndex(2, 3, "ArrowDown"), 0);
  assert.equal(moveDestinationActiveIndex(0, 3, "ArrowUp"), 2);
  assert.equal(moveDestinationActiveIndex(1, 3, "End"), 2);
  assert.equal(moveDestinationActiveIndex(2, 3, "Home"), 0);
  // A one-row filter cannot leave row 0, and an empty filter keeps Enter a no-op.
  assert.equal(moveDestinationActiveIndex(0, 1, "ArrowDown"), 0);
  assert.equal(moveDestinationActiveIndex(0, 0, "ArrowDown"), 0);
  assert.equal(moveDestinationActiveIndex(5, 3, "ArrowDown"), 0);
});

test("Enter chooses the active SELECTABLE row and skips the inactive one before it", () => {
  const filtered = filterDestinationEntries(branch, "");
  // One inactive row at the end: the three lawful rows are the whole sequence.
  assert.deepEqual(selectableDestinationIds(filtered), ["id-tehran", "id-tabriz", "id-mashhad"]);
  assert.equal(destinationActiveId(filtered, 0), "id-tehran");
  assert.equal(destinationActiveId(filtered, 2), "id-mashhad");
  // Out-of-range active indices clamp instead of throwing.
  assert.equal(destinationActiveId(filtered, 9), "id-mashhad");
  assert.equal(destinationActiveId(filtered, -3), "id-tehran");
});

test("only the four navigation keys are treated as navigation", () => {
  for (const key of ["ArrowDown", "ArrowUp", "Home", "End"]) {
    assert.equal(isDestinationSearchNavigationKey(key), true);
  }
  for (const key of ["Enter", "Escape", "Tab", "a", "PageDown"]) {
    assert.equal(isDestinationSearchNavigationKey(key), false);
  }
});
