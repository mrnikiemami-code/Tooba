import assert from "node:assert/strict";
import test from "node:test";
import { normalizeStoryColumnOrder, applyStoryListColumnConstraints } from "./story-column-order.ts";

test("title stays first and actions last even when old saved order put actions early", () => {
  const order = normalizeStoryColumnOrder(["actions", "updatedAt", "title", "shopDisplayName", "title"]);
  assert.deepEqual(order[0], "title");
  assert.deepEqual(order[order.length - 1], "actions");
  assert.ok(order.indexOf("shopDisplayName") > 0);
  assert.equal(order.filter((id) => id === "title").length, 1);
});

test("unrelated filter/sort fields outside column order are untouched by constraints helper", () => {
  const next = applyStoryListColumnConstraints({
    columnOrder: ["actions", "items", "title"],
    pinned: { title: "left", items: "right" },
  });
  assert.deepEqual(next.columnOrder, ["title", "items", "actions"]);
  assert.deepEqual(next.pinned, { items: "right" });
});
