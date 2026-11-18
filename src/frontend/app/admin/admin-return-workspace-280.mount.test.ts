/**
 * TB-280 — the shopkeeper surface of the REAL return workspace:
 *
 * 1. a case whose goods have NOT arrived offers «ثبت دریافت کالا» only — no review form full of zero ceilings;
 * 2. the moment goods arrive the review action appears, with the plain label «بررسی مرجوعی»;
 * 3. ONE action is presented as the next step (a labelled primary button), the rest stay secondary;
 * 4. a resolution line without a meaningful reason never leaves the surface, and the same dialog sends once it has one;
 * 5. the operator-visible copy carries no implementation nouns (decision version, split plan, occurrence id) and the
 *    chronological logs live behind the «سابقه» disclosure instead of the working area.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-280.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { act, createElement } from "react";
import test from "node:test";
import { AdminReturnWorkspace } from "./admin-return-workspace.tsx";
import { click, ensureDom, flush, mount, q, qq, setInputValue, waitFor } from "../../features/order-carrier-execution/test-dom-harness.ts";

ensureDom();

const CASE_ID = "rr-280-a";
const CHECKOUT_ID = "checkout-280-a";

type Call = { url: string; method: string; body: Record<string, unknown> | null };

function respond(status: number, payload: unknown): Response {
  return { ok: status >= 200 && status < 300, status, json: async () => payload } as unknown as Response;
}

type FixtureOptions = {
  received: number;
  work?: Record<string, unknown>;
  /** A stored reviewed plan on a case that was already inspected. */
  withResolution?: boolean;
  canPublishResolution?: boolean;
};

function fixture({ received, work = {}, withResolution = false, canPublishResolution = false }: FixtureOptions) {
  const actionCodes: string[] = [];
  if (received === 0) actionCodes.push("record_receipt");
  else actionCodes.push("record_receipt", "approve_return", "reject_return");
  if (canPublishResolution) actionCodes.push("publish_resolution");

  return {
    returnRequestId: CASE_ID,
    sellerOrderId: "so-280",
    checkoutId: CHECKOUT_ID,
    sellerPartyId: "party-280",
    requestedByUserId: "user-280",
    status: "Requested",
    reason: "کالا سالم نبود",
    currency: "IRR",
    refundAmount: 300000,
    paymentId: "pay-280",
    refundDestination: "OriginalPayment",
    createdAt: "2026-11-18T08:00:00Z",
    updatedAt: "2026-11-18T08:00:00Z",
    origin: "CustomerDelivered",
    recordedDecision: null,
    effectiveDecision: null,
    decisionVersion: 0,
    decisionHistory: [],
    approvedAmount: null,
    correction: { canReopenForReview: false, blockerCode: null },
    items: [
      {
        returnItemId: "ri-1",
        orderLineId: "ol-1",
        quantity: 2,
        unitPriceSnapshot: 100000,
        currency: "IRR",
        condition: "Usable",
        productLabel: "کالای اول",
        unitLabel: "pcs",
        unitDecimalPlaces: 0,
        disposition: "None",
        saleableRestockedQuantity: null,
      },
    ],
    resolution: withResolution
      ? {
          version: 1,
          isComplete: false,
          approvedAmount: 0,
          fromReviewedPlan: true,
          lines: [
            {
              returnItemId: "ri-1",
              requested: 2,
              received: 1,
              usable: 1,
              damaged: 0,
              accepted: 1,
              rejected: 1,
              acceptedUsable: 1,
              acceptedDamaged: 0,
              rejectedReceivedUsable: 0,
              rejectedReceivedDamaged: 0,
              rejectedUnreceived: 1,
              reason: "یک عدد پذیرفته شد",
              version: 1,
            },
          ],
          blockerCode: null,
        }
      : null,
    custody: [],
    refundAttempts: [],
    receipts: [],
    receiving: {
      lines: [{ returnItemId: "ri-1", requested: 2, received, usable: received, damaged: 0 }],
      isComplete: received >= 2,
      isCoherent: true,
    },
    work: {
      categoryCode: received === 0 ? "receiving" : "decision",
      blockerCode: null,
      pendingRefundAttemptId: null,
      failedRefundAttemptId: null,
      canRecordReceipt: received < 2,
      canDecide: received > 0,
      canResumeRefund: false,
      canRetryRefund: false,
      canSettlePhysical: false,
      canStartRefund: false,
      isOpen: true,
      needsOperator: true,
      actionCodes,
      ...work,
    },
  };
}

function stubFetch(detail: unknown) {
  const original = globalThis.fetch;
  const calls: Call[] = [];
  globalThis.fetch = (async (input: unknown, init?: { method?: string; body?: unknown }) => {
    const url = typeof input === "string" ? input : String((input as { url?: string })?.url ?? input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = typeof init?.body === "string" ? (JSON.parse(init.body) as Record<string, unknown>) : null;
    calls.push({ url, method, body });
    return respond(200, detail);
  }) as typeof fetch;
  return {
    calls,
    posts: () => calls.filter((call) => call.method === "POST"),
    restore() {
      globalThis.fetch = original;
    },
  };
}

function primaryActionId(container: HTMLElement): string | null {
  const nextStep = container.querySelector("[data-testid='admin-return-next-step']");
  if (!nextStep) return null;
  const button = Array.from(nextStep.querySelectorAll("button")).find((entry) =>
    String(entry.getAttribute("data-testid") ?? "").startsWith("admin-return-action-"),
  );
  return button ? String(button.getAttribute("data-testid")).replace("admin-return-action-", "") : null;
}

test("no goods received: the only next step is recording the arrival, never a zero-ceiling review", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch(fixture({ received: 0 }));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);

    assert.equal(
      qq(tree.container, "[data-testid='admin-return-action-publish-resolution']").length,
      0,
      "a review of nothing is not offered",
    );
    assert.equal(primaryActionId(tree.container), "record-receipt", "receiving is the obvious next step");

    // Opening the receipt dialog must not greet the operator with red errors on fields nobody touched.
    await click(q(tree.container, "[data-testid='admin-return-action-record-receipt']"));
    await flush();
    const receiptDialog = q(tree.container, "[data-testid='admin-return-receipt-form']").closest("dialog");
    assert.notEqual(receiptDialog, null);
    assert.equal(qq(receiptDialog as HTMLElement, "[role='alert']").length, 0, "no red error before any interaction");
    assert.match(receiptDialog?.textContent ?? "", /دریافت/);
    const cancel = qq(receiptDialog as HTMLElement, "button").find((button) => (button.textContent ?? "").trim() === "انصراف");
    await click(cancel as HTMLElement);
    await flush();
    assert.equal(stub.posts().length, 0, "cancel sends and stores nothing");
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("goods arrived: the reviewed split is offered as «بررسی مرجوعی» and a required reason gates the send", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch(fixture({ received: 1, canPublishResolution: true }));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-publish-resolution']").length === 1);

    const reviewButton = q(tree.container, "[data-testid='admin-return-action-publish-resolution']");
    assert.match(reviewButton.textContent ?? "", /بررسی مرجوعی/);
    assert.doesNotMatch(reviewButton.textContent ?? "", /تفکیک|برنامه/);

    await click(reviewButton);
    await flush();

    const reason = q(tree.container, "[data-testid='admin-return-resolution-reason-0']");
    assert.ok(reason, "each line owns a reason field");
    assert.doesNotMatch(q(tree.container, "[data-testid='admin-return-resolution-form']").textContent ?? "", /اختیاری/);

    // The accepted portion is lawful, but the owning writer refuses a plan line without a meaningful reason.
    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-accepted-usable-0']"), "1");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "a reasonless review never leaves the surface");
    assert.match(
      q(tree.container, "[data-testid='admin-return-notice']").textContent ?? "",
      /دلیل بررسی را بنویسید/,
    );

    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-reason-0']"), "سالم بود");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await flush();

    const posts = stub.posts();
    assert.equal(posts.length, 1);
    assert.match(posts[0].url, /\/v1\/admin\/returns\/rr-280-a\/resolution$/);
    const lines = posts[0].body?.lines as Array<Record<string, unknown>>;
    assert.equal(lines.length, 1);
    assert.equal(lines[0].reason, "سالم بود");
    assert.equal(lines[0].acceptedUsable, 1);
    assert.equal(lines[0].rejectedUnreceived, 1, "the never-received remainder stays a stored physical fact");
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("one action is presented as the next step and the rest stay secondary", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch(fixture({ received: 1 }));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-approve']").length === 1);

    assert.equal(primaryActionId(tree.container), "record-receipt", "goods-in stays the first step while it is offered");
    const others = q(tree.container, "[data-testid='admin-return-other-actions']");
    assert.ok(others, "the remaining actions are grouped, not equally weighted");
    assert.equal(
      qq(others, "[data-testid='admin-return-action-approve']").length,
      1,
      "the decision stays reachable from the secondary group",
    );
    const primary = q(tree.container, "[data-testid='admin-return-action-record-receipt']");
    assert.match(primary.textContent ?? "", /ثبت دریافت کالا/);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("the working area speaks shop language and the audit logs stay behind «سابقه»", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch(fixture({ received: 2, withResolution: true }));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-workspace-detail']").length === 1);

    const history = q(tree.container, "[data-testid='admin-return-history']") as HTMLDetailsElement;
    assert.ok(history, "the audit log is one disclosure");
    assert.equal(history.open, false, "logs stay collapsed while the operator works");

    // Everything OUTSIDE the disclosure is the shopkeeper copy: no plan/version/occurrence nouns.
    const working = tree.container.cloneNode(true) as HTMLElement;
    working.querySelectorAll("[data-testid='admin-return-history']").forEach((node) => node.remove());
    const copy = working.textContent ?? "";
    assert.doesNotMatch(copy, /نسخهٔ/);
    assert.doesNotMatch(copy, /تفکیک/);
    assert.doesNotMatch(copy, /occurrence/i);
    assert.doesNotMatch(copy, /idempot/i);
    assert.match(copy, /قدم بعدی/, "the next step is announced in words");
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});
