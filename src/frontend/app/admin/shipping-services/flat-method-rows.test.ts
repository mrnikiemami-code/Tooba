import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import { buildFlatMethodRows } from "./flat-method-rows.ts";
import type { OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

const FA = "11111111-1111-4111-8111-111111111111";
const EN = "22222222-2222-4222-8222-222222222222";

function method(partial: Partial<OneLevelMethodRow> & { methodCode: string }): OneLevelMethodRow {
  return {
    providerKind: "",
    isActive: true,
    legacyServiceCode: null,
    legacyOptionCode: null,
    translations: [],
    ...partial,
  };
}

test("the flat list has exactly one row per canonical method, in server order", () => {
  const rows = buildFlatMethodRows(
    [
      method({ methodCode: "post:express", isActive: false }),
      method({ methodCode: "post:standard" }),
      method({ methodCode: "tipax:express" }),
      method({ methodCode: "tipax:standard" }),
      method({ methodCode: "store_courier" }),
      method({ methodCode: "in_person" }),
    ],
    FA,
  );

  assert.deepEqual(rows.map((r) => r.methodCode), [
    "post:express",
    "post:standard",
    "tipax:express",
    "tipax:standard",
    "store_courier",
    "in_person",
  ]);
  // هویت پایدار گرید همان کد canonical است.
  assert.deepEqual(rows.map((r) => r.id), rows.map((r) => r.methodCode));
});

test("a duplicate canonical code never produces a second row", () => {
  const rows = buildFlatMethodRows(
    [method({ methodCode: "post:standard" }), method({ methodCode: "post:standard" }), method({ methodCode: "" })],
    FA,
  );
  assert.equal(rows.length, 1);
});

test("the label comes only from the real translation of the active language", () => {
  const rows = buildFlatMethodRows(
    [
      method({
        methodCode: "post:standard",
        translations: [
          { languageId: FA, name: " معمولی ", description: null },
          { languageId: EN, name: "Standard", description: null },
        ],
      }),
      method({ methodCode: "post:express", translations: [] }),
    ],
    FA,
  );

  assert.equal(rows[0]?.label, "معمولی");
  assert.equal(rows[0]?.hasLabel, true);
  // نبود ترجمهٔ زبان فعال ⇒ هیچ نامی اختراع نمی‌شود.
  assert.equal(rows[1]?.label, "");
  assert.equal(rows[1]?.hasLabel, false);

  // زبان دیگر برچسب زبان فعال را عوض نمی‌کند.
  const english = buildFlatMethodRows(
    [method({
      methodCode: "post:standard",
      translations: [
        { languageId: FA, name: "معمولی", description: null },
        { languageId: EN, name: "Standard", description: null },
      ],
    })],
    EN,
  );
  assert.equal(english[0]?.label, "Standard");
});

test("inactive methods stay inactive and are never dropped or silently activated", () => {
  const rows = buildFlatMethodRows(
    [
      method({ methodCode: "post:express", isActive: false }),
      method({ methodCode: "snapp_courier", isActive: false }),
      method({ methodCode: "post:standard", isActive: true }),
    ],
    FA,
  );
  assert.equal(rows.length, 3);
  assert.equal(rows.find((r) => r.methodCode === "post:express")?.isActive, false);
  assert.equal(rows.find((r) => r.methodCode === "snapp_courier")?.isActive, false);
  assert.equal(rows.find((r) => r.methodCode === "post:standard")?.isActive, true);
});

test("the row model carries no provider/option identity and never splits a method code", () => {
  const source = fs.readFileSync(new URL("./flat-method-rows.ts", import.meta.url), "utf8");
  const model = source.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");
  // هیچ تقسیم متنی/ساخت providerCode-optionCode در مرورگر وجود ندارد.
  assert.doesNotMatch(model, /\.split\(/);
  assert.doesNotMatch(model, /serviceCode|optionCode|providerKind/);

  const rows = buildFlatMethodRows([method({ methodCode: "tipax:standard" })], FA);
  assert.deepEqual(Object.keys(rows[0] ?? {}).sort(), [
    "hasLabel",
    "id",
    "isActive",
    "label",
    "methodCode",
  ]);
});

test("the client catalog reader uses only the canonical admin read path with fail-closed mapping", () => {
  const source = fs.readFileSync(new URL("./flat-method-catalog-api.ts", import.meta.url), "utf8");
  const api = source.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");
  assert.match(api, /ONE_LEVEL_CATALOG_ADMIN_READ_PATH/);
  assert.match(api, /resolveOneLevelCatalogGateFromHost\(\{ status: response\.status, payload \}\)/);
  assert.match(api, /mapHostOneLevelMethods\(payload\)/);
  assert.match(api, /response\.status === 401 \|\| response\.status === 403/);
  assert.doesNotMatch(api, /shipping-methods"|shipping-services"/);
  assert.doesNotMatch(api, /localStorage|URLSearchParams/);
});
