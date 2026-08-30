import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  displayTypeLabel,
  FACET_DISPLAY_LABELS,
  isSearchableDisplayType,
  partitionEffectiveFacets,
  suggestFacetDisplayType,
  type EffectiveCategoryFacet,
} from "./catalog-facet-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const panelPath = path.join(root, "app/admin/category-facets-panel.tsx");
const screenPath = path.join(root, "app/admin/category-admin-screen.tsx");
const facetApiPath = path.join(root, "app/admin/catalog-facet-api.ts");

function facet(
  partial: Partial<EffectiveCategoryFacet> & Pick<EffectiveCategoryFacet, "definitionId" | "code">,
): EffectiveCategoryFacet {
  return {
    localizedName: partial.code,
    valueKind: "Enumeration",
    displayType: "CheckboxList",
    sortOrder: 0,
    isVisible: true,
    isSearchable: false,
    isCollapsedByDefault: false,
    showCounts: true,
    sourceCategoryId: "cat-root",
    isInherited: false,
    ...partial,
  };
}

test("display type labels are ordinary Persian without facet jargon", () => {
  assert.match(FACET_DISPLAY_LABELS.CheckboxList.fa, /چندانتخابی/);
  assert.match(FACET_DISPLAY_LABELS.Range.fa, /بازه عددی/);
  assert.match(FACET_DISPLAY_LABELS.BooleanToggle.fa, /بله\/خیر/);
  assert.match(FACET_DISPLAY_LABELS.SearchableSelect.fa, /جستجوی متنی/);
  assert.equal(displayTypeLabel("SearchableSelect", "fa").includes("SearchableSelect"), false);
  assert.equal(displayTypeLabel("Range", "en"), "Numeric range");
});

test("suggestFacetDisplayType maps value kinds for non-technical users", () => {
  assert.equal(suggestFacetDisplayType("Boolean"), "BooleanToggle");
  assert.equal(suggestFacetDisplayType("Number"), "Range");
  assert.equal(suggestFacetDisplayType("Text"), "SearchableSelect");
  assert.equal(suggestFacetDisplayType("Enumeration"), "CheckboxList");
});

test("searchable control only for checklist/select display types", () => {
  assert.equal(isSearchableDisplayType("CheckboxList"), true);
  assert.equal(isSearchableDisplayType("SearchableSelect"), true);
  assert.equal(isSearchableDisplayType("Range"), false);
  assert.equal(isSearchableDisplayType("BooleanToggle"), false);
});

test("partitionEffectiveFacets splits inherited vs local rows", () => {
  const rows = [
    facet({ definitionId: "d1", code: "brand", isInherited: true }),
    facet({ definitionId: "d2", code: "color", isInherited: false, sourceCategoryId: "cat-child" }),
  ];
  const { inherited, local } = partitionEffectiveFacets(rows);
  assert.equal(inherited.length, 1);
  assert.equal(local.length, 1);
  assert.equal(inherited[0]?.code, "brand");
  assert.equal(local[0]?.code, "color");
});

test("Facets tab: VIEW/EDIT, add filter, inherited/local, labels", () => {
  const panel = fs.readFileSync(panelPath, "utf8");
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /CategoryFacetsPanel/);
  assert.match(screen, /activeTab === "facets"/);
  assert.match(screen, /handleEnterFacetsEdit/);
  assert.match(screen, /editSurface === "facets"/);
  assert.match(screen, /id: "facets", label: "فیلترهای صفحه محصولات", implemented: true/);
  assert.match(panel, /category-facets-panel/);
  assert.match(panel, /فیلترهای صفحه محصولات/);
  assert.match(panel, /facets-helper-copy/);
  assert.match(panel, /mapAdminErrorMessage/);
  assert.equal(panel.includes("facets-enter-edit"), false);
  assert.equal(panel.includes("facets-cancel-edit"), false);
  assert.equal(panel.includes("onEnterEdit"), false);
  assert.match(panel, /facet-add-button/);
  assert.match(panel, /افزودن فیلتر/);
  assert.match(panel, /facet-override-/);
  assert.match(panel, /تنظیم برای این دسته/);
  assert.match(panel, /facet-remove-/);
  assert.match(panel, /حذف تنظیم این دسته/);
  assert.match(panel, /facet-source-category/);
  assert.match(panel, /facet-display-type/);
  assert.match(panel, /facet-searchable/);
  assert.match(panel, /canEdit/);
  assert.equal(panel.match(/data-testid=.*definitionId/g)?.length ?? 0, 0);
});

test("Facets panel hides searchable checkbox for range/boolean types", () => {
  const panel = fs.readFileSync(panelPath, "utf8");
  assert.match(panel, /isSearchableDisplayType\(draft\.displayType\)/);
  assert.match(panel, /valueKind === "Boolean"/);
  assert.match(panel, /valueKind === "Number"/);
});

test("facet API never prefers raw title Bad Request", () => {
  const api = fs.readFileSync(facetApiPath, "utf8");
  assert.match(api, /parseAdminProblemErrorCode/);
  assert.equal(api.includes('prop(item, "title", "Title")'), false);
});

test("no raw AgGridReact in facets panel", () => {
  const panel = fs.readFileSync(panelPath, "utf8");
  assert.equal(panel.includes("AgGridReact"), false);
  assert.equal(panel.includes("AppDataGrid"), false);
});
