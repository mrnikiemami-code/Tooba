import fs from "node:fs";
import path from "node:path";

const host = "http://127.0.0.1:5088";
const web = "http://127.0.0.1:3000";
const shopeiva = "http://127.0.0.1:3001";
const actor = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const outDir = path.resolve("docs/evidence/TB-P06-T015");

fs.mkdirSync(outDir, { recursive: true });

async function hostFetch(pathname, { method = "GET", body } = {}) {
  const headers = { Accept: "application/json", "X-Tooba-Dev-Actor-User-Id": actor };
  if (body) headers["Content-Type"] = "application/json";
  const response = await fetch(`${host}${pathname}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json };
}

async function httpStatus(url) {
  try {
    const response = await fetch(url, { redirect: "manual" });
    return response.status;
  } catch {
    return 0;
  }
}

const runtime = {
  hostLive: await httpStatus(`${host}/health/live`),
  hostReady: await httpStatus(`${host}/health/ready`),
  home: await httpStatus(`${web}/`),
  adminComposition: await httpStatus(`${web}/admin/page-composition`),
  blog: await httpStatus(`${web}/blogs`),
  customerDashboard: await httpStatus(`${web}/customer-panel`),
  sellerDashboard: await httpStatus(`${web}/vendor-panel`),
  adminDashboard: await httpStatus(`${web}/admin`),
  shopeivaHome: await httpStatus(`${shopeiva}/`),
};

const baseline = await hostFetch("/v1/admin/page-composition/home");
const baselinePublic = await fetch(`${host}/v1/storefront/home/composition`).then((r) => r.json());

const sectionIds = baseline.json?.sections?.map((s) => s.pageSectionId) ?? [];
const reordered = sectionIds.slice();
if (reordered.length >= 2) {
  [reordered[0], reordered[1]] = [reordered[1], reordered[0]];
}

const reorderResult = reordered.length >= 2
  ? await hostFetch("/v1/admin/page-composition/home/reorder", {
      method: "PUT",
      body: { sectionIds: reordered },
    })
  : { status: 0, json: null };

const afterReorderPublic = await fetch(`${host}/v1/storefront/home/composition`).then((r) => r.json());

const brandsSection = baseline.json?.sections?.find((s) => s.sectionType === "brands");
const hideBrands = brandsSection
  ? await hostFetch(`/v1/admin/page-composition/home/sections/${brandsSection.pageSectionId}`, {
      method: "PUT",
      body: { isVisible: false },
    })
  : { status: 0, json: null };

const afterHidePublic = await fetch(`${host}/v1/storefront/home/composition`).then((r) => r.json());

const restore = await hostFetch("/v1/admin/page-composition/home/restore-default", { method: "POST" });
const afterRestorePublic = await fetch(`${host}/v1/storefront/home/composition`).then((r) => r.json());

const rejectedConfig = brandsSection
  ? await hostFetch(`/v1/admin/page-composition/home/sections/${brandsSection.pageSectionId}`, {
      method: "PUT",
      body: { configurationJson: '{"css":"evil"}' },
    })
  : { status: 0, json: null };

const proof = {
  recordedAtUtc: new Date().toISOString(),
  runtime,
  baselineSectionTypes: baselinePublic.sections?.map((s) => s.sectionType) ?? [],
  reorderStatus: reorderResult.status,
  afterReorderFirstTwo: afterReorderPublic.sections?.slice(0, 2).map((s) => s.sectionType) ?? [],
  hideBrandsStatus: hideBrands.status,
  publicSectionTypesAfterHide: afterHidePublic.sections?.map((s) => s.sectionType) ?? [],
  brandsVisibleAfterHide: afterHidePublic.sections?.some((s) => s.sectionType === "brands") ?? false,
  restoreStatus: restore.status,
  afterRestoreSectionTypes: afterRestorePublic.sections?.map((s) => s.sectionType) ?? [],
  rejectedForbiddenConfigStatus: rejectedConfig.status,
};

fs.writeFileSync(path.join(outDir, "_composition-e2e-api-proof.json"), JSON.stringify(proof, null, 2));
console.log(JSON.stringify(proof, null, 2));
