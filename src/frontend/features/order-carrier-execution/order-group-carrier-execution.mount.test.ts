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

const CHECKOUT = "chk-249r6";
const GROUP = "grp-249r6";
const PREFIX = "order-group-carrier-execution";
const OCCURRENCE_A = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const OP_A = "op-aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

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
  onCreate?: (body: Record<string, unknown>, ledger: RequestLedger) => Response;
  onTrack?: () => Response;
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
      const row: ShipRow = {
        operationId: OP_A,
        shippingGroupId: GROUP,
        state: createMode === "replay" ? "Succeeded" : "Succeeded",
        canTrack: true,
        canCancel: true,
        packageDispatchId: occurrence,
        providerTrackingCode: "TRK-1",
        providerStatus: "Created",
        lastErrorCode: null,
      };
      if (createMode === "replay" || ships.some((s) => s.packageDispatchId === occurrence)) {
        // replay converges on same operation / occurrence
        ships = [{ ...row, operationId: OP_A, packageDispatchId: occurrence }];
      } else {
        ships = [...ships, row];
      }
      cap = {
        ...cap,
        canCreate: true,
        latestCreateOperation: {
          operationId: OP_A,
          state: "Succeeded",
          packageDispatchId: occurrence,
        },
        executableLines: [{ orderLineId: "ol-1", remainingPackedQuantity: 1 }],
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

async function mountCarrier() {
  return mount(
    createElement(OrderGroupCarrierExecution, {
      checkoutId: CHECKOUT,
      apiPrefix: "/v1/admin",
      requestHeaders: () => ({ Authorization: "Bearer test" }),
      testIdPrefix: PREFIX,
    }),
  );
}

test("mount: package → sender → readiness → create retains occurrence id and request counts", async () => {
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
    // readiness refresh after package completion
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
    // draft package dims retained for create body (dispatch fields hidden when completion already done)
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
    // First create had no client occurrence → server issued OCCURRENCE_A.
    assert.equal(createBody.body.packageDispatchId, undefined);

    // After Succeeded+canCreate refresh, retention clears for a NEW parcel (Defect A).
    // Replay of SAME occurrence is the Pending/Unknown recovery path exercised below via remount.
  } finally {
    await tree.unmount();
    fx.restore();
  }
});

test("mount: same-tick double-click create posts exactly once (inFlight lock)", async () => {
  const fx = installFetchFixture();
  fx.setCapability(
    baseCapability({
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
    }),
  );
  const tree = await mountCarrier();
  try {
    await waitFor(() => !!tree.container.querySelector(`[data-testid="${PREFIX}-create-${GROUP}"]`));
    const btn = q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`);
    // two clicks in one act turn — sync inFlight should drop the second
    await clickBurst(btn, 2);
    await waitFor(() => fx.ledger.create >= 1);
    await flush();
    await new Promise((r) => setTimeout(r, 50));
    assert.equal(fx.ledger.create, 1, "exact create request count under double-click");
  } finally {
    await tree.unmount();
    fx.restore();
  }
});

test("mount: Unknown track refusal and typed intent_conflict surface in status", async () => {
  const fx = installFetchFixture({
    onTrack: () => jsonResponse(409, { errorCode: "shipping.carrier.shipment.unknown" }),
  });
  fx.setCapability(
    baseCapability({
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
    }),
  );
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
  }
});

test("mount: Pending occurrence retained across refresh; replay create posts same packageDispatchId", async () => {
  const fx = installFetchFixture();
  const pendingId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
  fx.setCapability(
    baseCapability({
      canCreate: true,
      allowPackageCompletion: false,
      blockReasonCodes: [],
      ineligibilityReasonCode: null,
      weightGrams: 500,
      lengthCentimeters: 20,
      widthCentimeters: 15,
      heightCentimeters: 10,
      latestCreateOperation: {
        operationId: "op-pending",
        state: "Pending",
        packageDispatchId: pendingId,
      },
      configuredSender: {
        fullName: "Store",
        mobile: "09120000000",
        addressLine: "Addr",
        postalCode: "1111111111",
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

    fx.setCreateMode("replay");
    await click(q(tree.container, `[data-testid="${PREFIX}-create-${GROUP}"]`));
    await waitFor(() => fx.ledger.create === 1);
    const createBody = fx.ledger.bodies.find((b) => (b as { kind?: string }).kind === "create") as {
      body: Record<string, unknown>;
    };
    assert.equal(createBody.body.packageDispatchId, pendingId);
  } finally {
    await tree.unmount();
    fx.restore();
  }
});
