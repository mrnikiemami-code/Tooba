import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  decodeCapability,
  decodeShipment,
  isCarrierApiGroup,
  normalizeShipmentState,
} from "./order-group-carrier-execution-decode.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const componentSource = readFileSync(join(dir, "order-group-carrier-execution.tsx"), "utf8");
const decodeSource = readFileSync(join(dir, "order-group-carrier-execution-decode.ts"), "utf8");
const adminPanel = readFileSync(
  join(dir, "../../app/admin/admin-order-items-shipping-panel.tsx"),
  "utf8",
);
const adminDetail = readFileSync(join(dir, "../../app/admin/admin-order-detail-screen.tsx"), "utf8");

test("normalizeShipmentState accepts numeric and string enums", () => {
  assert.equal(normalizeShipmentState(3), "Unknown");
  assert.equal(normalizeShipmentState("Unknown"), "Unknown");
  assert.equal(normalizeShipmentState(1), "Succeeded");
});

test("isCarrierApiGroup ignores methodCode heuristics", () => {
  const row = decodeCapability({
    shippingGroupId: "g1",
    transportMode: "Manual",
    methodCode: "tipax:carrier-standard",
  });
  assert.ok(row);
  assert.equal(isCarrierApiGroup(row), false);
  const api = decodeCapability({ shippingGroupId: "g2", transportMode: "CarrierApi" });
  assert.ok(api);
  assert.equal(isCarrierApiGroup(api), true);
});

test("decodeShipment maps capability flags and state", () => {
  const row = decodeShipment({
    operationId: "op1",
    shippingGroupId: "g1",
    state: 3,
    canTrack: true,
    canCancel: false,
  });
  assert.ok(row);
  assert.equal(row.state, "Unknown");
  assert.equal(row.canTrack, true);
  assert.equal(row.canCancel, false);
});

test("decodeCapability reads configured sender without recipient postal fallback", () => {
  const row = decodeCapability({
    shippingGroupId: "g1",
    transportMode: "CarrierApi",
    postalCode: "9999999999",
    configuredSender: { fullName: "Store", locationCode: "101" },
    canCreate: false,
    blockReasonCodes: ["payment_locked"],
  });
  assert.ok(row);
  assert.equal(row.configuredSender.postalCode, null);
  assert.deepEqual(row.blockReasonCodes, ["payment_locked"]);
});

test("completion→ready→execution→tracked/unknown action surface is wired once per group", () => {
  assert.match(componentSource, /package-completion/);
  assert.match(componentSource, /sender-completion/);
  assert.match(componentSource, /shipments\/\$\{row\.operationId\}\/track/);
  assert.match(componentSource, /carrierShipmentCreate/);
  assert.match(componentSource, /case "Unknown"/);
  assert.match(componentSource, /shipmentStateLabel\(row\.state\)/);
  assert.match(componentSource, /allowPackageCompletion/);
  assert.match(componentSource, /group\.canCreate/);
  assert.match(componentSource, /executableLines/);
  // Every post-action refresh is gated; failures keep drafts and skip refresh.
  const gatedRefresh = componentSource.match(/if \(!failed\) await refresh\(\);/g) ?? [];
  const bareRefresh = componentSource.match(/await refresh\(\);/g) ?? [];
  assert.equal(gatedRefresh.length, 5, "package/sender/create/track/cancel refresh only on success");
  assert.equal(bareRefresh.length, gatedRefresh.length, "no ungated await refresh after actions");
});

test("typed network error/retry preserves draft input and avoids status-driven refetch cascade", () => {
  assert.match(componentSource, /catch \{\s*setActionStatus\(messages\.carrierActionFailed\);\s*\}/);
  // Draft maps survive refresh: refresh only fills missing group keys.
  assert.match(componentSource, /if \(next\[row\.shippingGroupId\]\) continue;/);
  assert.match(componentSource, /setSenderByGroup/);
  assert.match(componentSource, /setPackageByGroup/);
  // No polling / status-effect cascade that re-fetches on actionStatus.
  assert.doesNotMatch(componentSource, /useEffect\([^\)]*actionStatus/);
  assert.doesNotMatch(componentSource, /setInterval/);
  assert.doesNotMatch(componentSource, /actionStatus[\s\S]{0,80}refresh\(/);
});

test("same-tick lock and stale-response guard are owned by the shared component", () => {
  assert.match(componentSource, /inFlightRef = useRef\(new Set<string>\(\)\)/);
  assert.match(componentSource, /if \(inFlightRef\.current\.has\(groupId\)\) return;/);
  assert.match(componentSource, /syncBusy\(groupId, true\)/);
  assert.match(componentSource, /refreshSeqRef = useRef\(0\)/);
  assert.match(componentSource, /const seq = \+\+refreshSeqRef\.current;/);
  assert.match(componentSource, /if \(seq !== refreshSeqRef\.current\) return;/);
  assert.match(componentSource, /refreshSeqRef\.current \+= 1/);
});

test("Manual+PendingPayment stays on normal items/payment UI without carrier form", () => {
  const manual = decodeCapability({
    shippingGroupId: "g-manual",
    transportMode: "Manual",
    canCreate: false,
    blockReasonCodes: ["payment_locked"],
  });
  assert.ok(manual);
  assert.equal(isCarrierApiGroup(manual), false);
  assert.match(componentSource, /if \(carrierGroups\.length === 0\) \{\s*return null;/);
  assert.match(componentSource, /capabilities\.filter\(isCarrierApiGroup\)/);
  assert.match(adminPanel, /AdminOrderGroupCarrierExecution/);
  assert.match(adminPanel, /paymentLocked/);
  assert.match(adminPanel, /PAYMENT_LOCKED_BANNER_FA|admin-order-seller-payment-locked/);
  assert.match(adminDetail, /بخش مالی سفارش|AdminOrderItemsShippingPanel/);
  assert.doesNotMatch(adminDetail, /AdminCarrierShipmentPanel/);
  // Decode authority for Manual remains transportMode, not method heuristics.
  assert.match(decodeSource, /transportMode === "CarrierApi"/);
});
