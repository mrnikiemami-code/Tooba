import assert from "node:assert/strict";
import test from "node:test";
import {
  buildCategoryForest,
  buildCategoryPath,
  buildParentMap,
  buildTranslationStatuses,
  canAddCategoryChild,
  collectAncestorIds,
  countDirectChildren,
  filterCategoryForest,
  getCategoryTreeLevel,
  isSelfOrDescendant,
  isValidCategoryDrop,
  listSiblingIds,
  resolveCategoryDropPlan,
  resolveTranslationReadiness,
  splitHighlight,
  type AppCategoryTreeNode,
} from "./tree-model.ts";

function sampleFlat(): AppCategoryTreeNode[] {
  return [
    {
      id: "root-a",
      parentId: null,
      name: "کالای دیجیتال",
      slug: "digital",
      status: "Published",
      sortOrder: 0,
      isVisible: true,
      hasChildren: true,
      productCount: 10,
    },
    {
      id: "child-a1",
      parentId: "root-a",
      name: "موبایل",
      slug: "mobile",
      status: "Draft",
      sortOrder: 0,
      isVisible: true,
      hasChildren: true,
      productCount: 4,
    },
    {
      id: "grand-a11",
      parentId: "child-a1",
      name: "گوشی هوشمند",
      slug: "smartphones",
      status: "Published",
      sortOrder: 0,
      isVisible: true,
      hasChildren: false,
      productCount: 2,
    },
    {
      id: "root-b",
      parentId: null,
      name: "کتاب",
      slug: "books",
      status: "Draft",
      sortOrder: 1,
      isVisible: true,
      hasChildren: false,
      productCount: 0,
    },
  ];
}

test("canAddCategoryChild respects custom maxDepth for content taxonomy", () => {
  const flat = sampleFlat();
  assert.equal(canAddCategoryChild(flat, "root-a", 2), true);
  assert.equal(canAddCategoryChild(flat, "child-a1", 2), false);
  assert.equal(
    isValidCategoryDrop(flat, { dragId: "root-b", dropId: "child-a1", position: "inside" }, 2),
    false,
  );
});

test("canAddCategoryChild blocks level-3 parents", () => {
  const flat = sampleFlat();
  assert.equal(canAddCategoryChild(flat, null), true);
  assert.equal(canAddCategoryChild(flat, "root-a"), true);
  assert.equal(canAddCategoryChild(flat, "child-a1"), true);
  assert.equal(canAddCategoryChild(flat, "grand-a11"), false);
  assert.equal(getCategoryTreeLevel(flat, "grand-a11"), 3);
  assert.equal(
    isValidCategoryDrop(flat, { dragId: "root-b", dropId: "grand-a11", position: "inside" }),
    false,
  );
});

test("buildCategoryForest nests children by parentId", () => {
  const forest = buildCategoryForest(sampleFlat());
  assert.equal(forest.length, 2);
  assert.equal(forest[0]!.id, "root-a");
  assert.equal(forest[0]!.children?.length, 1);
  assert.equal(forest[0]!.children?.[0]?.children?.[0]?.id, "grand-a11");
});

test("search filters and keeps ancestors + autoExpand", () => {
  const result = filterCategoryForest(sampleFlat(), "هوشمند");
  assert.equal(result.matchedIds.has("grand-a11"), true);
  assert.equal(result.autoExpandKeys.includes("child-a1"), true);
  assert.equal(result.autoExpandKeys.includes("root-a"), true);
  assert.equal(result.filteredForest.length, 1);
  assert.equal(result.filteredForest[0]!.children?.[0]?.children?.[0]?.id, "grand-a11");
});

test("clear search restores full forest", () => {
  const result = filterCategoryForest(sampleFlat(), "");
  assert.equal(result.matchedIds.size, 0);
  assert.equal(result.filteredForest.length, 2);
});

test("highlight splits matched segment", () => {
  const parts = splitHighlight("گوشی هوشمند", "هوش");
  assert.deepEqual(
    parts.map((p) => ({ ...p })),
    [
      { text: "گوشی ", match: false },
      { text: "هوش", match: true },
      { text: "مند", match: false },
    ],
  );
});

test("self and descendant moves are invalid", () => {
  const flat = sampleFlat();
  assert.equal(
    isValidCategoryDrop(flat, { dragId: "root-a", dropId: "grand-a11", position: "inside" }),
    false,
  );
  assert.equal(
    isValidCategoryDrop(flat, { dragId: "child-a1", dropId: "child-a1", position: "before" }),
    false,
  );
  assert.equal(
    isValidCategoryDrop(flat, { dragId: "root-b", dropId: "root-a", position: "inside" }),
    true,
  );
});

test("isSelfOrDescendant walks parent chain", () => {
  const map = buildParentMap(sampleFlat());
  assert.equal(isSelfOrDescendant(map, "root-a", "grand-a11"), true);
  assert.equal(isSelfOrDescendant(map, "root-b", "grand-a11"), false);
});

test("resolveCategoryDropPlan inside and reorder", () => {
  const flat = sampleFlat();
  const inside = resolveCategoryDropPlan(flat, {
    dragId: "root-b",
    dropId: "root-a",
    position: "inside",
  });
  assert.ok(inside);
  assert.equal(inside!.newParentId, "root-a");
  assert.equal(inside!.needsMove, true);
  assert.deepEqual(inside!.orderedSiblingIds, ["child-a1", "root-b"]);

  const before = resolveCategoryDropPlan(flat, {
    dragId: "root-b",
    dropId: "root-a",
    position: "before",
  });
  assert.ok(before);
  assert.equal(before!.newParentId, null);
  assert.deepEqual(before!.orderedSiblingIds, ["root-b", "root-a"]);
});

test("deep link ancestors and path", () => {
  const flat = sampleFlat();
  const ancestors = collectAncestorIds(buildParentMap(flat), "grand-a11");
  assert.deepEqual(ancestors, ["child-a1", "root-a"]);
  assert.deepEqual(buildCategoryPath(flat, "grand-a11"), ["کالای دیجیتال", "موبایل", "گوشی هوشمند"]);
  assert.equal(countDirectChildren(flat, "root-a"), 1);
  assert.deepEqual(listSiblingIds(flat, null), ["root-a", "root-b"]);
});

test("translation readiness statuses", () => {
  assert.equal(resolveTranslationReadiness("نام", "slug"), "complete");
  assert.equal(resolveTranslationReadiness("نام", ""), "partial");
  assert.equal(resolveTranslationReadiness("", ""), "missing");

  const statuses = buildTranslationStatuses(
    [
      { locale: "fa-IR", name: "کتاب", slug: "ketab" },
      { locale: "en-US", name: "Books", slug: "" },
    ],
    ["fa-IR", "en-US", "ar-SA"],
  );
  assert.equal(statuses[0]!.readiness, "complete");
  assert.equal(statuses[1]!.readiness, "partial");
  assert.equal(statuses[2]!.readiness, "missing");
});
