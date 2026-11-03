import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { buildCarrierShipmentCreateBody } from "./build-carrier-shipment-create-body.ts";
import {
  ensurePackageDispatchOccurrence,
  mintPackageDispatchOccurrenceId,
} from "./mint-package-dispatch-occurrence.ts";
import {
  decodeCapability,
  decodeShipment,
  isCarrierApiGroup,
  normalizeShipmentState,
} from "./order-group-carrier-execution-decode.ts";
import {
  classifyCarrierShipmentCreateOutcome,
  decodeAuthoritativeShipmentList,
  resolveExactOccurrenceFromList,
} from "./classify-carrier-shipment-create-outcome.ts";
import {
  clearParcelOccurrenceLocator,
  readParcelOccurrenceLocator,
  writeParcelOccurrenceLocator,
} from "./parcel-occurrence-locator-store.ts";
import { resolveRetainedPackageDispatchId } from "./retain-package-dispatch-occurrence.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const componentSource = readFileSync(join(dir, "order-group-carrier-execution.tsx"), "utf8");
const decodeSource = readFileSync(join(dir, "order-group-carrier-execution-decode.ts"), "utf8");
const classifySource = readFileSync(join(dir, "classify-carrier-shipment-create-outcome.ts"), "utf8");
const adminPanel = readFileSync(
  join(dir, "../../app/admin/admin-order-items-shipping-panel.tsx"),
  "utf8",
);
const adminDetail = readFileSync(join(dir, "../../app/admin/admin-order-detail-screen.tsx"), "utf8");
const physicalEditor = readFileSync(join(dir, "../../app/admin/product-workspace-screen.tsx"), "utf8");
const physicalFields = readFileSync(
  join(dir, "../../app/admin/product-physical-specification-fields.tsx"),
  "utf8",
);
const physicalParsers = readFileSync(
  join(dir, "../../app/admin/product-physical-specification.ts"),
  "utf8",
);
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

test("resolveRetainedPackageDispatchId keeps Pending and clears after Succeeded when canCreate", () => {
  assert.equal(
    resolveRetainedPackageDispatchId({
      current: null,
      canCreate: true,
      createResponsePackageDispatchId: "11111111-1111-4111-8111-111111111111",
    }),
    "11111111-1111-4111-8111-111111111111",
  );
  assert.equal(
    resolveRetainedPackageDispatchId({
      current: "old",
      canCreate: false,
      openOccurrenceId: "22222222-2222-4222-8222-222222222222",
      openState: "Pending",
    }),
    "22222222-2222-4222-8222-222222222222",
  );
  assert.equal(
    resolveRetainedPackageDispatchId({
      current: "old",
      canCreate: true,
      openOccurrenceId: "33333333-3333-4333-8333-333333333333",
      openState: "Succeeded",
    }),
    null,
  );
  assert.equal(
    resolveRetainedPackageDispatchId({
      current: "keep-unknown",
      canCreate: false,
      openOccurrenceId: "44444444-4444-4444-8444-444444444444",
      openState: "Unknown",
    }),
    "44444444-4444-4444-8444-444444444444",
  );
});

test("resolveRetainedPackageDispatchId keeps prebound id when uncertain or list read failed", () => {
  assert.equal(
    resolveRetainedPackageDispatchId({
      current: "prebound-aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
      canCreate: true,
      openOccurrenceId: null,
      openState: null,
      createUncertain: true,
      listReadOk: false,
    }),
    "prebound-aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
  );
  assert.equal(
    resolveRetainedPackageDispatchId({
      current: "prebound-bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbbb",
      canCreate: true,
      openOccurrenceId: null,
      openState: null,
      createUncertain: false,
      listReadOk: false,
    }),
    "prebound-bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbbb",
  );
});

test("mintPackageDispatchOccurrenceId is UuidV7 and ensure retains existing", () => {
  const id = mintPackageDispatchOccurrenceId(1_700_000_000_000);
  assert.match(id, /^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
  assert.equal(ensurePackageDispatchOccurrence("  keep-me  "), "keep-me");
  const minted = ensurePackageDispatchOccurrence(null);
  assert.match(minted, /^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
});

test("parcel occurrence locator store scopes owner+checkout+group and fails closed without owner", () => {
  const memory = new Map<string, string>();
  const g = globalThis as typeof globalThis & { sessionStorage?: Storage };
  const previous = g.sessionStorage;
  g.sessionStorage = {
    getItem: (k: string) => memory.get(k) ?? null,
    setItem: (k: string, v: string) => {
      memory.set(k, v);
    },
    removeItem: (k: string) => {
      memory.delete(k);
    },
    clear: () => memory.clear(),
    key: () => null,
    get length() {
      return memory.size;
    },
  } as Storage;
  try {
    clearParcelOccurrenceLocator("owner-a", "chk-r8", "grp-r8");
    assert.equal(readParcelOccurrenceLocator("owner-a", "chk-r8", "grp-r8"), null);
    writeParcelOccurrenceLocator(null, "chk-r8", "grp-r8", {
      occurrenceId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
      uncertain: true,
    });
    assert.equal(memory.size, 0, "no owner → no persist");
    assert.equal(readParcelOccurrenceLocator(null, "chk-r8", "grp-r8"), null);
    writeParcelOccurrenceLocator("owner-a", "chk-r8", "grp-r8", {
      occurrenceId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
      uncertain: true,
    });
    assert.deepEqual(readParcelOccurrenceLocator("owner-a", "chk-r8", "grp-r8"), {
      occurrenceId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
      uncertain: true,
    });
    assert.equal(readParcelOccurrenceLocator("owner-b", "chk-r8", "grp-r8"), null);
    assert.equal(readParcelOccurrenceLocator("owner-a", "chk-other", "grp-r8"), null);
    clearParcelOccurrenceLocator("owner-a", "chk-r8", "grp-r8");
    assert.equal(readParcelOccurrenceLocator("owner-a", "chk-r8", "grp-r8"), null);
  } finally {
    if (previous) g.sessionStorage = previous;
    else delete g.sessionStorage;
  }
});

test("classifyCarrierShipmentCreateOutcome matrix: 5xx/network/malformed/mismatch stay unknown", () => {
  const base = {
    expectedOccurrenceId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
    expectedShippingGroupId: "grp-1",
  };
  assert.equal(
    classifyCarrierShipmentCreateOutcome({ ...base, networkError: true }).outcome,
    "unknown",
  );
  assert.equal(
    classifyCarrierShipmentCreateOutcome({ ...base, httpStatus: 500, payload: {} }).outcome,
    "unknown",
  );
  assert.equal(
    classifyCarrierShipmentCreateOutcome({ ...base, httpStatus: 502, payload: {} }).outcome,
    "unknown",
  );
  assert.equal(
    classifyCarrierShipmentCreateOutcome({
      ...base,
      httpStatus: 200,
      payload: { not: "a shipment" },
    }).outcome,
    "unknown",
  );
  assert.equal(
    classifyCarrierShipmentCreateOutcome({
      ...base,
      httpStatus: 200,
      payload: {
        operationId: "op1",
        shippingGroupId: "grp-1",
        state: "Succeeded",
        packageDispatchId: "bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbbb",
      },
    }).outcome,
    "unknown",
  );
  assert.equal(
    classifyCarrierShipmentCreateOutcome({
      ...base,
      httpStatus: 409,
      errorCode: "shipping.carrier.shipment.unknown",
    }).outcome,
    "unknown",
  );
  assert.equal(
    classifyCarrierShipmentCreateOutcome({
      ...base,
      httpStatus: 409,
      errorCode: "shipping.carrier.shipment.intent_conflict",
    }).outcome,
    "refused",
  );
  assert.equal(
    classifyCarrierShipmentCreateOutcome({
      ...base,
      httpStatus: 400,
      errorCode: "shipping.carrier.shipment.request_invalid",
    }).outcome,
    "refused",
  );
  assert.equal(
    classifyCarrierShipmentCreateOutcome({
      ...base,
      httpStatus: 409,
      errorCode: "totally.unknown.code",
    }).outcome,
    "unknown",
  );
  const ok = classifyCarrierShipmentCreateOutcome({
    ...base,
    httpStatus: 200,
    payload: {
      operationId: "op1",
      shippingGroupId: "grp-1",
      state: "Succeeded",
      packageDispatchId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
    },
  });
  assert.equal(ok.outcome, "succeeded");
  assert.equal(ok.packageDispatchId, "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa");
});

test("decodeAuthoritativeShipmentList rejects malformed 200 and empty-looking garbage", () => {
  assert.equal(decodeAuthoritativeShipmentList(false, []).ok, false);
  assert.equal(decodeAuthoritativeShipmentList(true, { items: [] }).ok, false);
  assert.equal(decodeAuthoritativeShipmentList(true, [{ bad: true }]).ok, false);
  const empty = decodeAuthoritativeShipmentList(true, []);
  assert.equal(empty.ok, true);
  if (empty.ok) assert.equal(empty.ships.length, 0);
  const good = decodeAuthoritativeShipmentList(true, [
    {
      operationId: "op1",
      shippingGroupId: "g1",
      state: "Pending",
      packageDispatchId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
    },
  ]);
  assert.equal(good.ok, true);
  if (good.ok) assert.equal(good.ships.length, 1);
});

test("resolveExactOccurrenceFromList requires matching occurrence+group", () => {
  const ships = [
    {
      operationId: "op1",
      shippingGroupId: "g1",
      state: "Succeeded" as const,
      providerTrackingCode: null,
      providerStatus: null,
      lastErrorCode: null,
      canTrack: true,
      canCancel: false,
      packageDispatchId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
    },
  ];
  assert.ok(
    resolveExactOccurrenceFromList({
      ships,
      occurrenceId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
      shippingGroupId: "g1",
    }),
  );
  assert.equal(
    resolveExactOccurrenceFromList({
      ships,
      occurrenceId: "aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa",
      shippingGroupId: "g2",
    }),
    null,
  );
  assert.equal(
    resolveExactOccurrenceFromList({
      ships,
      occurrenceId: "bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbbb",
      shippingGroupId: "g1",
    }),
    null,
  );
});

test("decodeShipment maps capability flags and state", () => {
  const row = decodeShipment({
    operationId: "op1",
    shippingGroupId: "g1",
    state: 3,
    canTrack: true,
    canCancel: false,
    packageDispatchId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
  });
  assert.ok(row);
  assert.equal(row.state, "Unknown");
  assert.equal(row.canTrack, true);
  assert.equal(row.canCancel, false);
  assert.equal(row.packageDispatchId, "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
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
  assert.match(componentSource, /classifyCarrierShipmentCreateOutcome/);
  assert.match(componentSource, /resolveExactOccurrenceFromList/);
  assert.match(componentSource, /decodeAuthoritativeShipmentList/);
  // package/sender/track/cancel remain gated; create refreshes only after classified success.
  const gatedRefresh = componentSource.match(/if \(!result\.error\) await refresh\(\);/g) ?? [];
  assert.equal(gatedRefresh.length, 4, "package/sender/track/cancel refresh only on success");
  assert.match(componentSource, /reconcileGroup/);
  assert.match(componentSource, /groupNeedsReconcileSurface/);
  assert.match(componentSource, /showCreate = group\.canCreate && !uncertain/);
  assert.match(componentSource, /data-testid=\{`\$\{testIdPrefix\}-read-retry`\}/);
});

test("typed network error/retry preserves draft input and avoids status-driven refetch cascade", () => {
  assert.match(componentSource, /catch \{\s*setActionStatus\(messages\.carrierActionFailed\);\s*\}/);
  assert.match(componentSource, /markCreateUncertain\(groupId, occurrence\)/);
  assert.match(componentSource, /carrierCreateOutcomeUncertain/);
  // In-flight uncertain is persisted before POST leaves the browser.
  assert.match(
    componentSource,
    /markCreateUncertain\(groupId, occurrence\);\s*try \{\s*const executable/,
  );
  assert.match(classifySource, /status >= 500/);
  assert.match(classifySource, /PRE_SIDE_EFFECT_REFUSAL_CODES/);
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
  assert.match(componentSource, /if \(seq !== refreshSeqRef\.current\) \{\s*return \{ listOk: shipmentsListOkRef/);
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
  assert.match(componentSource, /ensurePackageDispatchOccurrence/);
  assert.match(componentSource, /packageDispatchId: occurrence/);
  assert.match(componentSource, /resolveRetainedPackageDispatchId/);
  assert.match(componentSource, /classified\.packageDispatchId/);
  assert.match(componentSource, /markCreateUncertain/);
  assert.match(componentSource, /carrierCreateOutcomeUncertain/);
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
  assert.match(physicalEditor, /ProductPhysicalSpecificationFields/);
  assert.match(physicalEditor, /updateAdminProductPhysicalSpecification/);
  assert.match(physicalEditor, /parseOptionalWholeGrams\(activeDraft\.weightGrams\)/);
  assert.match(physicalEditor, /physicalChanged/);
  assert.match(physicalEditor, /physicalResult\.view/);
  assert.match(physicalEditor, /markGeneralDirty/);
  assert.match(physicalFields, /data-testid="product-edit-physical-specification"/);
  assert.match(physicalFields, /مشخصات فیزیکی/);
  assert.match(physicalFields, /وزن \(گرم\)/);
  assert.match(physicalFields, /طول \(سانتی‌متر\)/);
  assert.match(physicalFields, /data-testid="product-edit-weight-grams"/);
  assert.match(physicalFields, /data-testid="product-edit-length-cm"/);
  assert.match(physicalFields, /data-testid="product-edit-width-cm"/);
  assert.match(physicalFields, /data-testid="product-edit-height-cm"/);
  assert.match(physicalParsers, /parseOptionalWholeGrams/);
  assert.match(physicalParsers, /Number\.isInteger/);
  assert.match(hostClient, /\/v1\/admin\/products\/\$\{productId\}\/physical-specification/);
  assert.match(hostClient, /method: "PATCH"/);
  assert.match(hostClient, /weightGrams: input\.weightGrams/);
});
