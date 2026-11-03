/**
 * Defect C — real React mount of OrderGroupCarrierExecution with controlled fetch fixtures.
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test features/order-carrier-execution/order-group-carrier-execution.mount.test.ts
 */
import "./install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import {
  click,
  clickBurst,
  flush,
  mount,
  q,
  setInputValue,
  waitFor,
} from "./test-dom-harness.ts";
import { OrderGroupCarrierExecution } from "./order-group-carrier-execution.tsx";

const CHECKOUT = "chk-249r8";
const GROUP = "grp-249r8";
const OWNER = "admin:test-owner-249r8";
const PREFIX = "order-group-carrier-execution";
const OCCURRENCE_A = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const OP_A = "op-aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

function clearOccurrenceSession() {
  try {
    const keys: string[] = [];
    for (let i = 0; i < sessionStorage.length; i += 1) {
      const key = sessionStorage.key(i);
      if (key && key.startsWith("tb.carrier.parcel-occurrence.v1:")) keys.push(key);
    }
    for (const key of keys) sessionStorage.removeItem(key);
  } catch {
    /* ignore */
  }
}

type CapState = {
  canCreate: boolean;
  allowPackageCompletion: boolean;
  hasEditableSenderContact: boolean;
  hasEditableSenderPostal: boolean;
  blockReasonCodes: string[];
  ineligibilityReasonCode: string | null;
  executableLines: Array<{ orderLineId: string; remainingPackedQuantity: number }>;
  weightGrams: number | null;
  lengthCentimeters: number | null;
  widthCentimeters: number | null;
  heightCentimeters: number | null;
  configuredSender: {
    fullName: string | null;
    mobile: string | null;
    addressLine: string | null;
    postalCode: string | null;
  };
  latestCreateOperation: null | {
    operationId: string;
    state: string;
    packageDispatchId: string;
  };
};

type ShipRow = {
  operationId: string;
  shippingGroupId: string;
  state: string;
  canTrack: boolean;
  canCancel: boolean;
  packageDispatchId: string | null;
  providerTrackingCode: string | null;
  providerStatus: string | null;
  lastErrorCode: string | null;
};

type RequestLedger = {
  capabilities: number;
  shipmentsGet: number;
  packageCompletion: number;
  senderCompletion: number;
  create: number;
  track: number;
  cancel: number;
  bodies: unknown[];
};

function baseCapability(overrides: Partial<CapState> = {}): CapState {
  return {
    canCreate: false,
    allowPackageCompletion: true,
    hasEditableSenderContact: true,
    hasEditableSenderPostal: true,
    blockReasonCodes: ["package_incomplete"],
    ineligibilityReasonCode: "package_incomplete",
    executableLines: [{ orderLineId: "ol-1", remainingPackedQuantity: 2 }],
    weightGrams: null,
    lengthCentimeters: null,
    widthCentimeters: null,
    heightCentimeters: null,
    configuredSender: {
      fullName: null,
      mobile: null,
      addressLine: null,
      postalCode: null,
    },
    latestCreateOperation: null,
    ...overrides,
  };
}

function capabilityDto(state: CapState) {
  return {
    shippingGroupId: GROUP,
    transportMode: "CarrierApi",
    methodLabel: "CarrierApi Tipax",
    methodCode: "tipax:carrier-standard",
    recipientName: "Buyer",
    cityName: "Tehran",
    originLocationCode: "101",
    destinationLocationCode: "201",
    declaredValue: 100000,
    canCreate: state.canCreate,
    allowPackageCompletion: state.allowPackageCompletion,
    hasEditableSenderContact: state.hasEditableSenderContact,
    hasEditableSenderPostal: state.hasEditableSenderPostal,
    blockReasonCodes: state.blockReasonCodes,
    ineligibilityReasonCode: state.ineligibilityReasonCode,
    executableLines: state.executableLines,
    weightGrams: state.weightGrams,
    lengthCentimeters: state.lengthCentimeters,
    widthCentimeters: state.widthCentimeters,
    heightCentimeters: state.heightCentimeters,
    configuredSender: state.configuredSender,
    latestCreateOperation: state.latestCreateOperation,
  };
}

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function installFetchFixture(options?: {
  onCreate?: (body: Record<string, unknown>, ledger: RequestLedger) => Response | Promise<Response>;
  onTrack?: () => Response;
  onShipmentsGet?: (ledger: RequestLedger) => Response | null;
}) {
  const ledger: RequestLedger = {
    capabilities: 0,
    shipmentsGet: 0,
    packageCompletion: 0,
    senderCompletion: 0,
    create: 0,
    track: 0,
    cancel: 0,
    bodies: [],
  };

  let cap = baseCapability();
  let ships: ShipRow[] = [];
  let createMode: "success" | "replay" | "intent_conflict" | "unknown_create" = "success";
  let allocationCount = 0;
  let providerCreates = 0;

  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(typeof input === "string" || input instanceof URL ? input : input.url);
    const method = (init?.method ?? "GET").toUpperCase();

    if (url.includes("/shipments/capabilities") && method === "GET") {
      ledger.capabilities += 1;
      return jsonResponse(200, [capabilityDto(cap)]);
    }
    if (url.includes("/shipments?") && method === "GET" && !url.includes("capabilities")) {
      ledger.shipmentsGet += 1;
      const override = options?.onShipmentsGet?.(ledger);
      if (override) return override;
      return jsonResponse(200, ships);
    }
    if (url.endsWith("/shipments/package-completion") && method === "POST") {
      ledger.packageCompletion += 1;
      const body = JSON.parse(String(init?.body ?? "{}")) as Record<string, unknown>;
      ledger.bodies.push({ kind: "package-completion", body });
      cap = {
        ...cap,
        weightGrams: Number(body.weightGrams),
        lengthCentimeters: Number(body.lengthCentimeters),
        widthCentimeters: Number(body.widthCentimeters),
        heightCentimeters: Number(body.heightCentimeters),
        allowPackageCompletion: false,
        blockReasonCodes: ["sender_incomplete"],
        ineligibilityReasonCode: "sender_incomplete",
        canCreate: false,
      };
      return jsonResponse(200, {
        operationId: "pkg-complete",
        shippingGroupId: GROUP,
        state: "Succeeded",
        canTrack: false,
        canCancel: false,
        packageDispatchId: null,
      });
    }
    if (url.endsWith("/shipments/sender-completion") && method === "POST") {
      ledger.senderCompletion += 1;
      const body = JSON.parse(String(init?.body ?? "{}")) as Record<string, unknown>;
      ledger.bodies.push({ kind: "sender-completion", body });
      cap = {
        ...cap,
        configuredSender: {
          fullName: String(body.fullName ?? ""),
          mobile: String(body.mobile ?? ""),
          addressLine: String(body.addressLine ?? ""),
          postalCode: String(body.postalCode ?? ""),
        },
        hasEditableSenderContact: true,
        hasEditableSenderPostal: true,
        blockReasonCodes: [],
        ineligibilityReasonCode: null,
        canCreate: true,
        allowPackageCompletion: false,
      };
      return jsonResponse(200, {
        operationId: "sender-complete",
        shippingGroupId: GROUP,
        state: "Succeeded",
        canTrack: false,
        canCancel: false,
        packageDispatchId: null,
      });
    }
    if (url.endsWith("/shipments") && method === "POST") {
      ledger.create += 1;
      const body = JSON.parse(String(init?.body ?? "{}")) as Record<string, unknown>;
      ledger.bodies.push({ kind: "create", body });
      if (options?.onCreate) return options.onCreate(body, ledger);
      if (createMode === "intent_conflict") {
        return jsonResponse(409, { errorCode: "shipping.carrier.shipment.intent_conflict" });
      }
      if (createMode === "unknown_create") {
        return jsonResponse(409, { errorCode: "shipping.carrier.shipment.unknown" });
      }
      const occurrence =
        typeof body.packageDispatchId === "string" && body.packageDispatchId.trim().length > 0
          ? body.packageDispatchId.trim()
          : OCCURRENCE_A;
      const existing = ships.find((s) => s.packageDispatchId === occurrence);
      if (existing || createMode === "replay") {
        // same occurrence → same operation; no second allocation/provider
        const row: ShipRow = {
          ...(existing ?? {
            operationId: OP_A,
            shippingGroupId: GROUP,
            state: "Succeeded",
            canTrack: true,
            canCancel: true,
            packageDispatchId: occurrence,
            providerTrackingCode: "TRK-1",
            providerStatus: "Created",
            lastErrorCode: null,
          }),
          state: "Succeeded",
          packageDispatchId: occurrence,
        };
        ships = ships.some((s) => s.packageDispatchId === occurrence)
          ? ships.map((s) => (s.packageDispatchId === occurrence ? row : s))
          : [...ships, row];
        cap = {
          ...cap,
          canCreate: true,
          latestCreateOperation: {
            operationId: row.operationId,
            state: "Succeeded",
            packageDispatchId: occurrence,
          },
        };
        return jsonResponse(200, row);
      }
      allocationCount += 1;
      providerCreates += 1;
      const opId = `op-${occurrence}`;
      const row: ShipRow = {
        operationId: opId,
        shippingGroupId: GROUP,
        state: "Succeeded",
        canTrack: true,
        canCancel: true,
        packageDispatchId: occurrence,
        providerTrackingCode: `TRK-${allocationCount}`,
        providerStatus: "Created",
        lastErrorCode: null,
      };
      ships = [...ships, row];
      const remaining = Math.max(0, (cap.executableLines[0]?.remainingPackedQuantity ?? 1) - 1);
      cap = {
        ...cap,
        canCreate: remaining > 0,
        latestCreateOperation: {
          operationId: opId,
          state: "Succeeded",
          packageDispatchId: occurrence,
        },
        executableLines: [{ orderLineId: "ol-1", remainingPackedQuantity: remaining }],
      };
      return jsonResponse(200, row);
    }
    if (url.includes("/shipments/") && url.endsWith("/track") && method === "POST") {
      ledger.track += 1;
      if (options?.onTrack) return options.onTrack();
      return jsonResponse(409, { errorCode: "shipping.carrier.shipment.unknown" });
    }
    if (url.includes("/shipments/") && url.endsWith("/cancel") && method === "POST") {
      ledger.cancel += 1;
      return jsonResponse(200, ships[0] ?? {});
    }
    return jsonResponse(404, { errorCode: "not_found" });
  }) as typeof fetch;

  return {
    ledger,
    get allocationCount() {
      return allocationCount;
    },
    get providerCreates() {
      return providerCreates;
    },
    get ships() {
      return ships;
    },
    applyServerAcceptedCreate(occurrence: string) {
      allocationCount += 1;
      providerCreates += 1;
      const opId = `op-${occurrence}`;
      const row: ShipRow = {
        operationId: opId,
        shippingGroupId: GROUP,
        state: "Succeeded",
        canTrack: true,
        canCancel: true,
        packageDispatchId: occurrence,
        providerTrackingCode: `TRK-${allocationCount}`,
        providerStatus: "Created",
        lastErrorCode: null,
      };
      ships = [...ships, row];
      const remaining = Math.max(0, (cap.executableLines[0]?.remainingPackedQuantity ?? 1) - 1);
      cap = {
        ...cap,
        canCreate: remaining > 0,
        latestCreateOperation: {
          operationId: opId,
          state: "Succeeded",
          packageDispatchId: occurrence,
        },
        executableLines: [{ orderLineId: "ol-1", remainingPackedQuantity: remaining }],
      };
      return row;
    },
    setCreateMode(mode: typeof createMode) {
      createMode = mode;
    },
    setCapability(next: CapState) {
      cap = next;
    },
    setShipments(next: ShipRow[]) {
      ships = next;
    },
    restore() {
      globalThis.fetch = original;
    },
  };
}

async function mountCarrier(ownerContextKey: string | null = OWNER) {
  return mount(
    createElement(OrderGroupCarrierExecution, {
      checkoutId: CHECKOUT,
      apiPrefix: "/v1/admin",
      ownerContextKey,
      requestHeaders: () => ({ Authorization: "Bearer test" }),
      testIdPrefix: PREFIX,
    }),
  );
}

function readyCapability(overrides: Partial<CapState> = {}): CapState {
  return baseCapability({
    canCreate: true,
    allowPackageCompletion: false,
    blockReasonCodes: [],
    ineligibilityReasonCode: null,
    weightGrams: 500,
    lengthCentimeters: 20,
    widthCentimeters: 15,
    heightCentimeters: 10,
    configuredSender: {
      fullName: "Store",
      mobile: "09120000000",
      addressLine: "Addr",
      postalCode: "1111111111",
    },
    ...overrides,
  });
}

test("mount: package → sender → readiness → first create already sends stable occurrence", async () => {
  clearOccurrenceSession();
  const fx = installFetchFixture();
  const tree = await mountCarrier();
  try {
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}"]`));
    assert.equal(fx.ledger.capabilities, 1);
    assert.equal(fx.ledger.shipmentsGet, 1);

    const block = q(tree.container, `[data-testid="${PREFIX}-block-${GROUP}"]`);
    assert.match(block.textContent ?? "", /ابعاد\/وزن|کامل نیست/);

    await setInputValue(q(tree.container, `#pkg-weight-${GROUP}`), "500");
    await setInputValue(q(tree.container, `#pkg-l-${GROUP}`), "20");
    await setInputValue(q(tree.container, `#pkg-w-${GROUP}`), "15");
    await setInputValue(q(tree.container, `#pkg-h-${GROUP}`), "10");
    await setInputValue(q(tree.container, `#pkg-val-${GROUP}`), "100000");
    await click(q(tree.container, `[data-testid="${PREFIX}-package-completion-${GROUP}"]`));

    await waitFor(() => fx.ledger.packageCompletion === 1);
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("مشخصات بسته ذخیره شد");
    });
    assert.equal(fx.ledger.capabilities, 2);
    assert.equal(fx.ledger.shipmentsGet, 2);

    await waitFor(() => !!tree.container.querySelector(`#sender-name-${GROUP}`));
    await setInputValue(q(tree.container, `#sender-name-${GROUP}`), "فروشگاه آرمان");
    await setInputValue(q(tree.container, `#sender-mobile-${GROUP}`), "09121234567");
    await setInputValue(q(tree.container, `#sender-address-${GROUP}`), "تهران");
    await setInputValue(q(tree.container, `#sender-postal-${GROUP}`), "1234567890");
    await click(q(tree.container, `[data-testid="${PREFIX}-sender-completion-${GROUP}"]`));

    await waitFor(() => fx.ledger.senderCompletion === 1);
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("اطلاعات فرستنده ذخیره شد");
    });
    assert.equal(fx.ledger.capabilities, 3);
    assert.equal(fx.ledger.shipmentsGet, 3);

    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
    await click(q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
    await waitFor(() => fx.ledger.create === 1);
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("حساب حامل ذخیره شد");
    });
    assert.equal(fx.ledger.capabilities, 4);
    assert.equal(fx.ledger.shipmentsGet, 4);

    const createBody = fx.ledger.bodies.find((b) => (b as { kind?: string }).kind === "create") as {
      body: Record<string, unknown>;
    };
    assert.equal(createBody.body.shippingGroupId, GROUP);
    assert.equal(createBody.body.checkoutId, CHECKOUT);
    const occurrence = String(createBody.body.packageDispatchId ?? "");
    assert.match(occurrence, /^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
    assert.equal(fx.allocationCount, 1);
    assert.equal(fx.providerCreates, 1);
  } finally {
    await tree.unmount();
    fx.restore();
    clearOccurrenceSession();
  }
});

test("mount: same-tick double-click create posts exactly once (inFlight lock)", async () => {
  clearOccurrenceSession();
  const fx = installFetchFixture();
  fx.setCapability(readyCapability());
  const tree = await mountCarrier();
  try {
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
    const btn = q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`);
    await clickBurst(btn, 2);
    await waitFor(() => fx.ledger.create >= 1);
    await flush();
    await new Promise((r) => setTimeout(r, 50));
    assert.equal(fx.ledger.create, 1, "exact create request count under double-click");
  } finally {
    await tree.unmount();
    fx.restore();
    clearOccurrenceSession();
  }
});

test("mount: Unknown track refusal and typed intent_conflict surface in status", async () => {
  clearOccurrenceSession();
  const fx = installFetchFixture({
    onTrack: () => jsonResponse(409, { errorCode: "shipping.carrier.shipment.unknown" }),
  });
  fx.setCapability(readyCapability({ executableLines: [{ orderLineId: "ol-1", remainingPackedQuantity: 1 }] }));
  fx.setShipments([
    {
      operationId: OP_A,
      shippingGroupId: GROUP,
      state: "Succeeded",
      canTrack: true,
      canCancel: false,
      packageDispatchId: OCCURRENCE_A,
      providerTrackingCode: "TRK-1",
      providerStatus: "Created",
      lastErrorCode: null,
    },
  ]);
  const tree = await mountCarrier();
  try {
    await waitFor(() => (tree.container.textContent ?? "").includes("پیگیری"));
    const trackBtn = Array.from(tree.container.querySelectorAll("button")).find((b) =>
      (b.textContent ?? "").includes("پیگیری"),
    );
    assert.ok(trackBtn);
    await click(trackBtn!);
    await waitFor(() => fx.ledger.track === 1);
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("نتیجه مبهم");
    });

    fx.setCreateMode("intent_conflict");
    await click(q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
    await waitFor(() => fx.ledger.create === 1);
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("ذخیره");
    });
  } finally {
    await tree.unmount();
    fx.restore();
    clearOccurrenceSession();
  }
});

test("mount: Pending canCreate=false exposes reconcile; zero create calls", async () => {
  clearOccurrenceSession();
  const fx = installFetchFixture();
  const pendingId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
  fx.setCapability(
    readyCapability({
      canCreate: false,
      blockReasonCodes: ["operation_pending"],
      ineligibilityReasonCode: "operation_pending",
      latestCreateOperation: {
        operationId: "op-pending",
        state: "Pending",
        packageDispatchId: pendingId,
      },
    }),
  );
  fx.setShipments([
    {
      operationId: "op-pending",
      shippingGroupId: GROUP,
      state: "Pending",
      canTrack: false,
      canCancel: false,
      packageDispatchId: pendingId,
      providerTrackingCode: null,
      providerStatus: null,
      lastErrorCode: null,
    },
  ]);
  const tree = await mountCarrier();
  try {
    await waitFor(() =>
      !!tree.container.querySelector(`[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`),
    );
    assert.match(tree.container.textContent ?? "", /در انتظار پاسخ/);
    const retained = q(
      tree.container,
      `[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`,
    ) as HTMLInputElement;
    assert.equal(retained.value, pendingId);
    assert.ok(tree.container.querySelector(`[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    assert.equal(tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`), null);
    await click(q(tree.container, `[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    await waitFor(() => fx.ledger.shipmentsGet >= 2);
    assert.equal(fx.ledger.create, 0, "Pending recovery must not substitute Create");
  } finally {
    await tree.unmount();
    fx.restore();
    clearOccurrenceSession();
  }
});

test("mount R8-A: outstanding first POST → unmount/remount → reconcile exact occurrence; no replacement", async () => {
  clearOccurrenceSession();
  let acceptedOccurrence: string | null = null;
  let releaseCreate: ((value: Response) => void) | null = null;
  let fx!: ReturnType<typeof installFetchFixture>;
  fx = installFetchFixture({
    onCreate: (body) => {
      const occurrence = String(body.packageDispatchId ?? "");
      acceptedOccurrence = occurrence;
      fx.applyServerAcceptedCreate(occurrence);
      return new Promise<Response>((resolve) => {
        releaseCreate = resolve;
      });
    },
  });
  fx.setCapability(readyCapability({ executableLines: [{ orderLineId: "ol-1", remainingPackedQuantity: 2 }] }));

  const tree1 = await mountCarrier();
  try {
    await waitFor(() => !!tree1.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
    await click(q(tree1.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
    await waitFor(() => fx.ledger.create === 1);
    await waitFor(() =>
      !!tree1.container.querySelector(`[data-testid="${PREFIX}-create-uncertain-${GROUP}"]`),
    );
    const hidden = q(
      tree1.container,
      `[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`,
    ) as HTMLInputElement;
    assert.equal(hidden.value, acceptedOccurrence);
    assert.equal(tree1.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`), null);
  } finally {
    await tree1.unmount();
  }

  const tree2 = await mountCarrier();
  try {
    await waitFor(() =>
      !!tree2.container.querySelector(`[data-testid="${PREFIX}-create-uncertain-${GROUP}"]`),
    );
    const hidden = q(
      tree2.container,
      `[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`,
    ) as HTMLInputElement;
    assert.equal(hidden.value, acceptedOccurrence);
    await click(q(tree2.container, `[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    await waitFor(() => {
      const status = tree2.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("آشتی");
    });
    assert.equal(fx.ledger.create, 1, "reconcile must not post create");
    assert.equal(fx.allocationCount, 1, "no replacement allocation");
    assert.equal(fx.providerCreates, 1, "no replacement provider call");
    assert.equal(
      tree2.container.querySelector(`[data-testid="${PREFIX}-create-uncertain-${GROUP}"]`),
      null,
      "exact occurrence success clears uncertainty",
    );
    // Create may reappear for remaining qty, but must not auto-resend the resolved occurrence.
    assert.equal(fx.ledger.create, 1);
  } finally {
    await tree2.unmount();
    // Resolve abandoned in-flight create only after unmount so it cannot rewrite session hints.
    if (releaseCreate && acceptedOccurrence) {
      const row = fx.ships.find((s) => s.packageDispatchId === acceptedOccurrence);
      releaseCreate(jsonResponse(200, row ?? {}));
    }
    fx.restore();
    clearOccurrenceSession();
  }
});

test("mount R8-B: 500/network/malformed200/mismatch remain unresolved with original id", async () => {
  clearOccurrenceSession();
  const cases: Array<{
    name: string;
    seedServer: boolean;
    onCreate: (body: Record<string, unknown>, fx: ReturnType<typeof installFetchFixture>) => Response | Promise<Response>;
  }> = [
    {
      name: "http500-after-accept",
      seedServer: true,
      onCreate: (body, fx) => {
        fx.applyServerAcceptedCreate(String(body.packageDispatchId ?? ""));
        return jsonResponse(500, { errorCode: "server.boom" });
      },
    },
    {
      name: "http502-after-accept",
      seedServer: true,
      onCreate: (body, fx) => {
        fx.applyServerAcceptedCreate(String(body.packageDispatchId ?? ""));
        return jsonResponse(502, { errorCode: "bad.gateway" });
      },
    },
    {
      name: "network-loss",
      seedServer: true,
      onCreate: (body, fx) => {
        fx.applyServerAcceptedCreate(String(body.packageDispatchId ?? ""));
        throw new TypeError("Failed to fetch");
      },
    },
    {
      name: "malformed-200",
      seedServer: false,
      onCreate: () => jsonResponse(200, { totally: "wrong" }),
    },
    {
      name: "mismatched-occurrence",
      seedServer: false,
      onCreate: () =>
        jsonResponse(200, {
          operationId: "op-other",
          shippingGroupId: GROUP,
          state: "Succeeded",
          canTrack: true,
          canCancel: false,
          packageDispatchId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
          providerTrackingCode: "TRK-X",
          providerStatus: "Created",
          lastErrorCode: null,
        }),
    },
  ];

  for (const scenario of cases) {
    clearOccurrenceSession();
    let fx!: ReturnType<typeof installFetchFixture>;
    fx = installFetchFixture({
      onCreate: (body) => scenario.onCreate(body, fx),
    });
    fx.setCapability(readyCapability({ executableLines: [{ orderLineId: "ol-1", remainingPackedQuantity: 1 }] }));
    const tree = await mountCarrier();
    try {
      await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
      await click(q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
      await waitFor(() =>
        !!tree.container.querySelector(`[data-testid="${PREFIX}-create-uncertain-${GROUP}"]`),
      );
      const occurrence = (
        q(tree.container, `[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`) as HTMLInputElement
      ).value;
      assert.ok(occurrence.length > 0, scenario.name);
      assert.equal(tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`), null, scenario.name);

      if (!scenario.seedServer) {
        // List has no exact occurrence → reconcile must preserve unresolved locator.
        await click(q(tree.container, `[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
        await waitFor(() =>
          !!tree.container.querySelector(`[data-testid="${PREFIX}-create-uncertain-${GROUP}"]`),
        );
        assert.equal(
          (q(tree.container, `[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`) as HTMLInputElement)
            .value,
          occurrence,
          `${scenario.name} recovery retains original id`,
        );
        assert.equal(tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`), null);
      } else {
        // Authoritative list can later resolve the same id — never mint a replacement meanwhile.
        assert.equal(
          (q(tree.container, `[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`) as HTMLInputElement)
            .value,
          occurrence,
          `${scenario.name} keeps original id while unresolved`,
        );
      }
    } finally {
      await tree.unmount();
      fx.restore();
      clearOccurrenceSession();
    }
  }
});

test("mount R8-C: Unknown/Pending canCreate=false reconcile; read500/malformed200 stay blocked with retry", async () => {
  clearOccurrenceSession();
  const unknownId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
  let listMode: "ok" | "http500" | "malformed200" = "ok";
  const fx = installFetchFixture({
    onShipmentsGet: () => {
      if (listMode === "http500") return jsonResponse(500, { errorCode: "boom" });
      if (listMode === "malformed200") return jsonResponse(200, { not: "array" });
      return null;
    },
  });
  fx.setCapability(
    readyCapability({
      canCreate: false,
      blockReasonCodes: ["operation_unknown"],
      ineligibilityReasonCode: "operation_unknown",
      latestCreateOperation: {
        operationId: "op-unknown",
        state: "Unknown",
        packageDispatchId: unknownId,
      },
    }),
  );
  fx.setShipments([
    {
      operationId: "op-unknown",
      shippingGroupId: GROUP,
      state: "Unknown",
      canTrack: false,
      canCancel: false,
      packageDispatchId: unknownId,
      providerTrackingCode: null,
      providerStatus: null,
      lastErrorCode: "shipping.carrier.shipment.unknown",
    },
  ]);
  const tree = await mountCarrier();
  try {
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    assert.equal(tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`), null);
    const createsBefore = fx.ledger.create;
    await click(q(tree.container, `[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    await waitFor(() => fx.ledger.shipmentsGet >= 2);
    assert.equal(fx.ledger.create, createsBefore);

    listMode = "http500";
    await click(q(tree.container, `[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("فهرست محموله‌ها");
    });
    assert.ok(tree.container.querySelector(`[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    assert.equal(tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`), null);

    listMode = "malformed200";
    await click(q(tree.container, `[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    await waitFor(() => fx.ledger.shipmentsGet >= 4);
    assert.ok(tree.container.querySelector(`[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    assert.equal(tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`), null);
    assert.equal(fx.ledger.create, 0);
  } finally {
    await tree.unmount();
    fx.restore();
    clearOccurrenceSession();
  }
});

test("mount R8-C loadError exposes explicit read retry", async () => {
  clearOccurrenceSession();
  let failCaps = true;
  const original = globalThis.fetch;
  let capGets = 0;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(typeof input === "string" || input instanceof URL ? input : input.url);
    if (url.includes("/shipments/capabilities")) {
      capGets += 1;
      if (failCaps) return jsonResponse(500, { errorCode: "boom" });
      return jsonResponse(200, [capabilityDto(readyCapability())]);
    }
    if (url.includes("/shipments?")) return jsonResponse(200, []);
    return jsonResponse(404, {});
  }) as typeof fetch;
  const tree = await mountCarrier();
  try {
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-error"]`));
    assert.ok(tree.container.querySelector(`[data-testid="${PREFIX}-read-retry"]`));
    failCaps = false;
    await click(q(tree.container, `[data-testid="${PREFIX}-read-retry"]`));
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}"]`));
    assert.ok(capGets >= 2);
  } finally {
    await tree.unmount();
    globalThis.fetch = original;
    clearOccurrenceSession();
  }
});

test("mount R8-D: typed pre-side-effect refusal editable; success then distinct new parcel", async () => {
  clearOccurrenceSession();
  const fx = installFetchFixture();
  fx.setCapability(readyCapability({ executableLines: [{ orderLineId: "ol-1", remainingPackedQuantity: 2 }] }));
  fx.setCreateMode("intent_conflict");
  const tree = await mountCarrier();
  try {
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
    await click(q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
    await waitFor(() => fx.ledger.create === 1);
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").length > 0;
    });
    // Typed refusal resolves uncertainty — create remains editable (not stuck on reconcile-only).
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
    assert.equal(tree.container.querySelector(`[data-testid="${PREFIX}-create-uncertain-${GROUP}"]`), null);

    fx.setCreateMode("success");
    await click(q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
    await waitFor(() => fx.ledger.create === 2);
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("حساب حامل ذخیره شد");
    });
    const firstSuccess = fx.ledger.bodies.filter((b) => (b as { kind?: string }).kind === "create")[1] as {
      body: Record<string, unknown>;
    };
    const firstId = String(firstSuccess.body.packageDispatchId);

    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
    await click(q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
    await waitFor(() => fx.ledger.create === 3);
    const creates = fx.ledger.bodies.filter((b) => (b as { kind?: string }).kind === "create") as Array<{
      body: Record<string, unknown>;
    }>;
    const secondId = String(creates[2].body.packageDispatchId);
    assert.notEqual(secondId, firstId);
    assert.equal(fx.allocationCount, 2);
    assert.equal(fx.providerCreates, 2);
  } finally {
    await tree.unmount();
    fx.restore();
    clearOccurrenceSession();
  }
});

test("mount R7 retained: list fail after uncertain create blocks resubmit and keeps occurrence", async () => {
  clearOccurrenceSession();
  let listFail = false;
  let fx!: ReturnType<typeof installFetchFixture>;
  fx = installFetchFixture({
    onCreate: (body) => {
      const occurrence = String(body.packageDispatchId ?? "");
      fx.applyServerAcceptedCreate(occurrence);
      throw new TypeError("Failed to fetch");
    },
    onShipmentsGet: () => (listFail ? jsonResponse(500, { errorCode: "shipping.carrier.list_failed" }) : null),
  });
  fx.setCapability(readyCapability());
  const tree = await mountCarrier();
  try {
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
    await click(q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
    await waitFor(() =>
      !!tree.container.querySelector(`[data-testid="${PREFIX}-create-uncertain-${GROUP}"]`),
    );
    const occurrence = (
      q(tree.container, `[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`) as HTMLInputElement
    ).value;
    assert.ok(occurrence.length > 0);

    listFail = true;
    await click(q(tree.container, `[data-testid="${PREFIX}-reconcile-${GROUP}"]`));
    await waitFor(() => {
      const status = tree.container.querySelector(`[data-testid="${PREFIX}-status"]`);
      return (status?.textContent ?? "").includes("فهرست محموله‌ها");
    });
    assert.ok(tree.container.querySelector(`[data-testid="${PREFIX}-create-uncertain-${GROUP}"]`));
    assert.equal(tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`), null);
    assert.equal(
      (q(tree.container, `[data-testid="${PREFIX}-package-dispatch-id-${GROUP}"]`) as HTMLInputElement)
        .value,
      occurrence,
    );
  } finally {
    await tree.unmount();
    fx.restore();
    clearOccurrenceSession();
  }
});
