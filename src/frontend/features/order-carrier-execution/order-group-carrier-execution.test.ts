import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { buildCarrierShipmentCreateBody } from "./build-carrier-shipment-create-body.ts";
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
const physicalEditor = readFileSync(join(dir, "../../app/admin/product-workspace-screen.tsx"), "utf8");
const hostClient = readFileSync(join(dir, "../../app/admin/host-client.ts"), "utf8");

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
  assert.match(componentSource, /buildCarrierShipmentCreateBody/);
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

test("create body sends Package for partial dispatches and optional packageDispatchId", () => {
  const executable = [
    { orderLineId: "line-a", remainingPackedQuantity: 2 },
    { orderLineId: "line-b", remainingPackedQuantity: 1 },
  ];
  const partial = buildCarrierShipmentCreateBody({
    checkoutId: "chk",
    shippingGroupId: "grp",
    lines: [{ orderLineId: "line-a", quantity: 1 }],
    executableLines: executable,
    package: { weightGrams: 500, lengthCentimeters: 20, widthCentimeters: 15, heightCentimeters: 10 },
    packageDispatchId: "11111111-1111-4111-8111-111111111111",
  });
  assert.deepEqual(partial.package, {
    weightGrams: 500,
    lengthCentimeters: 20,
    widthCentimeters: 15,
    heightCentimeters: 10,
  });
  assert.equal(partial.packageDispatchId, "11111111-1111-4111-8111-111111111111");
  assert.equal(partial.partialWithoutPackage, undefined);

  const full = buildCarrierShipmentCreateBody({
    checkoutId: "chk",
    shippingGroupId: "grp",
    lines: [
      { orderLineId: "line-a", quantity: 2 },
      { orderLineId: "line-b", quantity: 1 },
    ],
    executableLines: executable,
    package: null,
    packageDispatchId: "  ",
  });
  assert.equal(full.package, undefined);
  assert.equal(full.packageDispatchId, undefined);
  assert.equal(full.partialWithoutPackage, undefined);

  const partialMissingPkg = buildCarrierShipmentCreateBody({
    checkoutId: "chk",
    shippingGroupId: "grp",
    lines: [{ orderLineId: "line-a", quantity: 1 }],
    executableLines: executable,
    package: null,
  });
  assert.equal(partialMissingPkg.partialWithoutPackage, true);
});

test("create click path posts buildCarrierShipmentCreateBody and keeps Manual null", () => {
  assert.match(componentSource, /buildCarrierShipmentCreateBody\(\{/);
  assert.match(componentSource, /packageDispatchId: packageDispatchIdByGroup\[groupId\]/);
  assert.match(componentSource, /weightGrams: weight/);
  assert.match(componentSource, /delete body\.partialWithoutPackage/);
  // Simulated create interaction: encode → build → JSON round-trip (events without full React tree).
  const capability = decodeCapability({
    shippingGroupId: "g-create",
    transportMode: "CarrierApi",
    canCreate: true,
    executableLines: [{ orderLineId: "ol1", remainingPackedQuantity: 3 }],
    weightGrams: 900,
    lengthCentimeters: 30,
    widthCentimeters: 20,
    heightCentimeters: 12,
  });
  assert.ok(capability);
  assert.equal(isCarrierApiGroup(capability), true);
  let posted: unknown = null;
  const fakePost = (body: unknown) => {
    posted = body;
  };
  // Emulate onClick payload assembly the component performs.
  const lines = capability.executableLines.map((line) => ({
    orderLineId: line.orderLineId,
    quantity: line.remainingPackedQuantity,
  }));
  // Partial: ship qty 1 of 3 → Package required.
  const partialLines = [{ orderLineId: "ol1", quantity: 1 }];
  const body = buildCarrierShipmentCreateBody({
    checkoutId: "checkout-1",
    shippingGroupId: capability.shippingGroupId,
    lines: partialLines,
    executableLines: capability.executableLines,
    package: {
      weightGrams: Number(capability.weightGrams),
      lengthCentimeters: Number(capability.lengthCentimeters),
      widthCentimeters: Number(capability.widthCentimeters),
      heightCentimeters: Number(capability.heightCentimeters),
    },
    packageDispatchId: null,
  });
  fakePost(JSON.parse(JSON.stringify(body)));
  assert.ok(posted && typeof posted === "object");
  const record = posted as Record<string, unknown>;
  assert.equal(record.shippingGroupId, "g-create");
  assert.deepEqual(record.package, {
    weightGrams: 900,
    lengthCentimeters: 30,
    widthCentimeters: 20,
    heightCentimeters: 12,
  });
  assert.deepEqual(record.lines, partialLines);
  // Full remaining path still works without inventing package when dims absent.
  const fullBody = buildCarrierShipmentCreateBody({
    checkoutId: "checkout-1",
    shippingGroupId: capability.shippingGroupId,
    lines,
    executableLines: capability.executableLines,
    package: null,
  });
  assert.equal(fullBody.package, undefined);
});

test("physical-spec editor block edit/save/reload surface stays compact with units", () => {
  assert.match(physicalEditor, /data-testid="product-edit-physical-specification"/);
  assert.match(physicalEditor, /مشخصات فیزیکی/);
  assert.match(physicalEditor, /وزن \(گرم\)/);
  assert.match(physicalEditor, /طول \(سانتی‌متر\)/);
  assert.match(physicalEditor, /data-testid="product-edit-weight-grams"/);
  assert.match(physicalEditor, /data-testid="product-edit-length-cm"/);
  assert.match(physicalEditor, /data-testid="product-edit-width-cm"/);
  assert.match(physicalEditor, /data-testid="product-edit-height-cm"/);
  assert.match(physicalEditor, /updateAdminProductPhysicalSpecification/);
  assert.match(physicalEditor, /parseOptionalPositiveDecimal\(activeDraft\.weightGrams\)/);
  // Save path: dirty → PATCH → reload view into draft (edit/save/reload).
  assert.match(physicalEditor, /physicalChanged/);
  assert.match(physicalEditor, /physicalResult\.view/);
  assert.match(hostClient, /\/v1\/admin\/products\/\$\{productId\}\/physical-specification/);
  assert.match(hostClient, /method: "PATCH"/);
  assert.match(hostClient, /weightGrams: input\.weightGrams/);
  // Interaction: typing updates draft + marks dirty (event handlers present).
  assert.match(physicalEditor, /setDraft\(\{ \.\.\.activeDraft, weightGrams: event\.target\.value \}\)/);
  assert.match(physicalEditor, /markGeneralDirty\(\)/);
});
