import { readFileSync, writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const fx = JSON.parse(readFileSync("docs/evidence/TB-P09-T022/r2-fixture.json", "utf8"));
const HOST = "http://127.0.0.1:5088";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";

function host(method, path, body, extra = {}) {
  const headers = {
    Host: "alpha.localhost",
    "Content-Type": "application/json",
    "X-Tooba-Dev-Actor-User-Id": ADMIN,
    ...extra,
  };
  const args = ["-sS", "-w", "\n%{http_code}", "-X", method, `${HOST}${path}`];
  for (const [k, v] of Object.entries(headers)) args.push("-H", `${k}: ${v}`);
  if (body !== undefined) args.push("--data-binary", JSON.stringify(body));
  const raw = execFileSync("curl.exe", args, { encoding: "utf8" });
  const i = raw.lastIndexOf("\n");
  let json;
  try {
    json = JSON.parse(raw.slice(0, i) || "null");
  } catch {
    json = raw.slice(0, i);
  }
  return { status: Number(raw.slice(i + 1)), json };
}

const cancel = host("POST", `/v1/admin/orders/${fx.checkoutId}/operations`, {
  idempotencyKey: randomUUID().replaceAll("-", ""),
  code: "cancel_consolidated_package",
  consolidatedPackageId: fx.packageId,
});
const create = host("POST", `/v1/admin/orders/${fx.checkoutId}/operations`, {
  idempotencyKey: randomUUID().replaceAll("-", ""),
  code: "create_consolidated_package",
  shipmentIds: fx.members.map((m) => m.shipmentId),
  shippingMethodCode: "tipax",
  trackingReference: "CENTRAL-T022-R2-REBUILT",
});
const ful = host("GET", `/v1/customer/orders/${fx.checkoutId}/fulfillments`, undefined, {
  Authorization: `Bearer ${fx.accessToken}`,
  "X-Tooba-Dev-Actor-User-Id": undefined,
});
// fix undefined header - rebuild headers properly
const ful2 = (() => {
  const args = [
    "-sS",
    "-w",
    "\n%{http_code}",
    "-X",
    "GET",
    `${HOST}/v1/customer/orders/${fx.checkoutId}/fulfillments`,
    "-H",
    "Host: alpha.localhost",
    "-H",
    `Authorization: Bearer ${fx.accessToken}`,
  ];
  const raw = execFileSync("curl.exe", args, { encoding: "utf8" });
  const i = raw.lastIndexOf("\n");
  return { status: Number(raw.slice(i + 1)), json: JSON.parse(raw.slice(0, i)) };
})();

const out = {
  cancelStatus: cancel.status,
  createStatus: create.status,
  packageNumber: create.json?.packageNumber ?? create.json?.PackageNumber,
  preferred: ful2.json.preferredCustomerTrackingReference,
  packagePreferred: ful2.json.preferredCustomerTrackingPackageNumber,
  fulStatus: ful2.status,
};
writeFileSync("docs/evidence/TB-P09-T022/r2-rebuild-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
if (out.preferred !== "CENTRAL-T022-R2-REBUILT") process.exit(1);
