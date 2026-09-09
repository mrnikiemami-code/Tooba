import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";

const BASE = "http://127.0.0.1:5088";
const PRODUCT = "01a05387-fbd0-7000-acd3-4382ce92c773";
const OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const UNIT_KG = "01900000-0000-7000-8000-000000000002";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";

const headers = {
  Host: "alpha.localhost",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
  "Content-Type": "application/json",
};

async function req(method, path, body, extra = {}) {
  const res = await fetch(BASE + path, {
    method,
    headers: { ...headers, ...extra },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status: res.status, json };
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q],
    { encoding: "utf8" },
  ).trim();
}

const out = { ok: true, steps: [] };
function note(name, data) {
  out.steps.push({ name, ...data });
}

const beforeLines = sql(
  `SELECT line_id||'|'||quantity||'|'||COALESCE(unit_code_snapshot,'')||'|'||quantity_decimal_places_snapshot||'|'||COALESCE(quantity_step_snapshot::text,'')
   FROM "order".order_lines
   ORDER BY line_id
   LIMIT 5;`,
);
note("historical-before", { rows: beforeLines.split("\n").filter(Boolean) });

const product = await req("GET", `/v1/admin/products/${PRODUCT}`);
if (product.status !== 200) {
  throw new Error("product get " + product.status);
}
let expected = product.json.catalogUpdatedAt;
const patched = await req("PATCH", `/v1/admin/products/${PRODUCT}/quantity-policy`, {
  unitOfMeasureId: UNIT_KG,
  decimalPlaces: 2,
  step: 0.25,
  expectedUpdatedAt: expected,
});
note("patch-step", { status: patched.status, step: patched.json.quantityStep, places: patched.json.quantityDecimalPlaces });
if (patched.status !== 200) {
  throw new Error("patch step failed");
}
expected = patched.json.catalogUpdatedAt ?? expected;

async function setRounding(mode) {
  const r = await req("PUT", "/v1/admin/settings/quantity-rounding", { globalRoundingMode: mode });
  note("rounding-" + mode, { status: r.status, mode: r.json.globalRoundingMode, fa: r.json.labelFa });
  if (r.status !== 200) {
    throw new Error("rounding " + mode);
  }
}

async function addQty(qty) {
  const cart = await req("POST", "/v1/storefront/cart");
  if (cart.status !== 200 && cart.status !== 201) {
    throw new Error("cart create " + cart.status);
  }
  const secret = cart.json.guestSecret;
  const added = await req(
    "POST",
    `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${cart.json.version}`,
    { offerId: OFFER, quantity: qty },
    { "X-Tooba-Guest-Secret": secret },
  );
  const line = (added.json.lines ?? [])[0];
  return { status: added.status, quantity: line?.quantity, cartId: cart.json.cartId, error: added.json };
}

await setRounding("Floor");
const floor = await addQty(1.37);
note("floor-1.37", floor);
if (Number(floor.quantity) !== 1.25) {
  out.ok = false;
}

await setRounding("Ceiling");
const ceiling = await addQty(1.37);
note("ceiling-1.37", ceiling);
if (Number(ceiling.quantity) !== 1.5) {
  out.ok = false;
}

await setRounding("Nearest");
const nearest = await addQty(1.37);
note("nearest-1.37", nearest);
if (Number(nearest.quantity) !== 1.25) {
  out.ok = false;
}

await setRounding("Ceiling");
const afterPatch = await req("PATCH", `/v1/admin/products/${PRODUCT}/quantity-policy`, {
  unitOfMeasureId: UNIT_KG,
  decimalPlaces: 2,
  step: 0.5,
  expectedUpdatedAt: expected,
});
note("patch-step-0.5", { status: afterPatch.status, step: afterPatch.json.quantityStep });

const afterLines = sql(
  `SELECT line_id||'|'||quantity||'|'||COALESCE(unit_code_snapshot,'')||'|'||quantity_decimal_places_snapshot||'|'||COALESCE(quantity_step_snapshot::text,'')
   FROM "order".order_lines
   ORDER BY line_id
   LIMIT 5;`,
);
note("historical-after", { rows: afterLines.split("\n").filter(Boolean) });
if (beforeLines !== afterLines) {
  out.ok = false;
  note("immutability", { changed: true });
} else {
  note("immutability", { changed: false });
}

const newer = await addQty(1.37);
note("new-op-after-policy-change", newer);

await setRounding("Nearest");
const restore = await req("GET", `/v1/admin/products/${PRODUCT}`);
const restorePatch = await req("PATCH", `/v1/admin/products/${PRODUCT}/quantity-policy`, {
  unitOfMeasureId: UNIT_KG,
  decimalPlaces: 2,
  step: null,
  expectedUpdatedAt: restore.json.catalogUpdatedAt,
});
note("restore-step-null", { status: restorePatch.status, step: restorePatch.json.quantityStep });

writeFileSync("docs/evidence/TB-P09-T013/r1-runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
if (!out.ok) {
  process.exit(1);
}
