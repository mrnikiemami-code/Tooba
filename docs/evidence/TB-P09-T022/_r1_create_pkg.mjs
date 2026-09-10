import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const checkout = "01a0898a-0d7d-7000-b5ed-7f5d85a29241";
const body = JSON.stringify({
  idempotencyKey: randomUUID().replaceAll("-", ""),
  code: "create_consolidated_package",
  shipmentIds: [
    "daae5ce8-276a-42c8-aa32-3d4637020b0c",
    "2e44551c-1bfd-40b4-9a28-5d69a61efa62",
  ],
  shippingMethodCode: "post",
  trackingReference: "CENTRAL-T022-R1",
});
const raw = execFileSync(
  "curl.exe",
  [
    "-sS",
    "-w",
    "\n%{http_code}",
    "-H",
    "Host: alpha.localhost",
    "-H",
    "X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db",
    "-H",
    "Content-Type: application/json",
    "-d",
    body,
    `http://127.0.0.1:5088/v1/admin/orders/${checkout}/operations`,
  ],
  { encoding: "utf8" },
);
console.log(raw);
