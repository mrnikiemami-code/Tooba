/**
 * TB-279 — the three NEW operator reviews of one return case, mounted on the REAL workspace:
 *
 * 1. an audited correction opens ONE design-system dialog, requires a reviewed reason, and sends the reviewed version;
 * 2. the per-item resolution dialog refuses an over-received split INSIDE the dialog (nothing stored, nothing sent) and
 *    sends one frozen body of accepted/rejected portions plus the exact resolution version;
 * 3. the custody dialog requires a proof reference and a bounded quantity before it will send;
 * 4. closing any of them sends nothing and stores no envelope.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-279.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { act, createElement } from "react";
import test from "node:test";
import { AdminReturnWorkspace } from "./admin-return-workspace.tsx";
import { click, ensureDom, flush, mount, q, qq, setInputValue, setSelectValue, waitFor } from "../../features/order-carrier-execution/test-dom-harness.ts";
import { returnIntentStorageKey } from "../returns/return-review-intent.ts";

ensureDom();

const CASE_ID = "rr-279-a";
const CHECKOUT_ID = "checkout-279-a";

type Call = { url: string; method: string; body: Record<string, unknown> | null };

function respond(status: number, payload: unknown): Response {
  return { ok: status >= 200 && status < 300, status, json: async () => payload } as unknown as Response;
}

function fixture(work: Record<string, unknown>) {
  return {
    returnRequestId: CASE_ID,
    sellerOrderId: "so-279",
    checkoutId: CHECKOUT_ID,
    sellerPartyId: "party-279",
    requestedByUserId: "user-279",
    status: "Requested",
    reason: "دو قلم از سه قلم سالم نبود",
    currency: "IRR",
    refundAmount: 300000,
    paymentId: "pay-279",
    refundDestination: "OriginalPayment",
    createdAt: "2026-11-17T08:00:00Z",
    updatedAt: "2026-11-17T08:00:00Z",
    origin: "CustomerDelivered",
    recordedDecision: "Approve",
    effectiveDecision: "Approve",
    decisionVersion: 2,
    decisionHistory: [
      { version: 1, decision: "Approve", reason: "بررسی اولیه", actorUserId: "u-1", recordedAt: "2026-11-17T09:00:00Z" },
    ],
    approvedAmount: 200000,
    correction: { canReopenForReview: true, blockerCode: null },
    items: [
      {
        returnItemId: "ri-1",
        orderLineId: "ol-1",
        quantity: 3,
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
    resolution: {
      version: 1,
      isComplete: false,
      approvedAmount: 100000,
      fromReviewedPlan: true,
      lines: [
        {
          returnItemId: "ri-1",
          requested: 3,
          received: 3,
          usable: 2,
          damaged: 1,
          accepted: 1,
          rejected: 2,
          acceptedUsable: 1,
          acceptedDamaged: 0,
          rejectedReceivedUsable: 1,
          rejectedReceivedDamaged: 1,
          rejectedUnreceived: 0,
          reason: "یکی معیوب",
          version: 1,
        },
      ],
      blockerCode: "return.resolution.incomplete",
    },
    custody: [],
    refundAttempts: [],
    receipts: [],
    receiving: {
      lines: [{ returnItemId: "ri-1", requested: 3, received: 3, usable: 2, damaged: 1 }],
      isComplete: true,
      isCoherent: true,
    },
    work: {
      categoryCode: "disposition",
      blockerCode: null,
      pendingRefundAttemptId: null,
      failedRefundAttemptId: null,
      canRecordReceipt: false,
      canDecide: false,
      canResumeRefund: false,
      canRetryRefund: false,
      canSettlePhysical: false,
      canStartRefund: false,
      isOpen: true,
      needsOperator: true,
      actionCodes: ["reopen_for_review", "publish_resolution", "record_custody"],
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

function storedIntent(): unknown {
  const raw = window.sessionStorage.getItem(returnIntentStorageKey(CASE_ID));
  return raw == null ? null : JSON.parse(raw);
}

function openDialogs(container: HTMLElement): HTMLElement[] {
  return qq(container, "dialog[open]");
}

async function openAction(container: HTMLElement, actionId: string) {
  await waitFor(() => qq(container, `[data-testid='admin-return-action-${actionId}']`).length === 1);
  await click(q(container, `[data-testid='admin-return-action-${actionId}']`));
  await flush();
}

async function setTextareaValue(el: Element, value: string) {
  const textarea = el as HTMLTextAreaElement;
  const setter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value")?.set;
  if (!setter) throw new Error("HTMLTextAreaElement value setter missing");
  await act(async () => {
    setter.call(textarea, value);
    textarea.dispatchEvent(new Event("input", { bubbles: true }));
  });
  await flush();
}

test("all three TB-279 reviews open ONE dialog and send nothing until confirmed", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch(fixture({}));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-reopen-for-review']").length === 1);
    assert.equal(openDialogs(tree.container).length, 0);

    for (const [actionId, formTestId] of [
      ["reopen-for-review", "admin-return-reopen-form"],
      ["publish-resolution", "admin-return-resolution-form"],
      ["record-custody", "admin-return-custody-form"],
    ]) {
      await openAction(tree.container, actionId);
      assert.equal(openDialogs(tree.container).length, 1, `${actionId} opens exactly one dialog`);
      assert.notEqual(q(tree.container, `[data-testid='${formTestId}']`).closest("dialog"), null);
      assert.equal(stub.posts().length, 0, `${actionId} sends nothing on open`);

      const cancel = qq(tree.container, "dialog[open] button").find((button) => (button.textContent ?? "").trim() === "انصراف");
      assert.ok(cancel, `${actionId} offers an explicit cancel`);
      await click(cancel as HTMLElement);
      await flush();
      assert.equal(openDialogs(tree.container).length, 0);
      assert.equal(stub.posts().length, 0);
      assert.equal(storedIntent(), null);
    }
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("the correction sends the reviewed reason and the exact expected decision version", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch(fixture({}));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-reopen-for-review']").length === 1);
    await openAction(tree.container, "reopen-for-review");

    const confirm = q(tree.container, "[data-testid='admin-return-reopen-confirm']") as HTMLButtonElement;
    assert.equal(confirm.disabled, true, "no reviewed reason means no send");

    await setTextareaValue(q(tree.container, "[data-testid='admin-return-reopen-reason']"), "تصمیم اولیه نادرست بود");
    assert.equal((q(tree.container, "[data-testid='admin-return-reopen-confirm']") as HTMLButtonElement).disabled, false);

    await click(q(tree.container, "[data-testid='admin-return-reopen-confirm']"));
    await flush();

    const posts = stub.posts();
    assert.equal(posts.length, 1);
    assert.match(posts[0].url, /\/v1\/admin\/returns\/rr-279-a\/reopen$/);
    assert.equal(posts[0].body?.reason, "تصمیم اولیه نادرست بود");
    assert.equal(posts[0].body?.expectedDecisionVersion, 2);
    assert.ok(typeof posts[0].body?.occurrenceKey === "string" && String(posts[0].body?.occurrenceKey).length > 0);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("the resolution dialog refuses an over-received split locally and sends one frozen body when lawful", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch(fixture({}));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-publish-resolution']").length === 1);
    await openAction(tree.container, "publish-resolution");

    // 3 accepted against a received-usable 2 exceeds THIS condition's ceiling: refused inside the dialog.
    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-accepted-usable-0']"), "3");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "an over-received split never leaves the surface");
    assert.match(
      (q(tree.container, "[data-testid='admin-return-notice']").closest("dialog") ?? q(tree.container, "[data-testid='admin-return-notice']")).textContent ?? "",
      /دریافت‌شده/,
    );

    // A damaged acceptance above the damaged ceiling is refused by the SAME per-condition rule.
    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-accepted-usable-0']"), "1");
    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-accepted-damaged-0']"), "2");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "a damaged acceptance can never borrow the usable ceiling");

    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-accepted-damaged-0']"), "0");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await flush();

    const posts = stub.posts();
    assert.equal(posts.length, 1);
    assert.match(posts[0].url, /\/v1\/admin\/returns\/rr-279-a\/resolution$/);
    assert.equal(posts[0].body?.expectedResolutionVersion, 1);
    const lines = posts[0].body?.lines as Array<Record<string, unknown>>;
    assert.equal(lines.length, 1);
    assert.equal(lines[0].returnItemId, "ri-1");
    assert.equal(lines[0].acceptedUsable, 1);
    assert.equal(lines[0].acceptedDamaged, 0);
    assert.equal(lines[0].rejectedReceivedUsable, 1);
    assert.equal(lines[0].rejectedReceivedDamaged, 1);
    assert.equal(lines[0].rejectedUnreceived, 0);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("custody needs a real proof reference and stays inside the rejected remainder", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch(fixture({}));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-custody']").length === 1);
    await openAction(tree.container, "record-custody");

    // The rejected RECEIVED portion is the only thing a handback can discharge; the line must be chosen deliberately.
    await setSelectValue(q(tree.container, "[data-testid='admin-return-custody-item']"), "ri-1");
    await setInputValue(q(tree.container, "[data-testid='admin-return-custody-quantity']"), "1");
    await click(q(tree.container, "[data-testid='admin-return-custody-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "a handback without evidence is not a fact");

    await setInputValue(q(tree.container, "[data-testid='admin-return-custody-quantity']"), "2");
    await setInputValue(q(tree.container, "[data-testid='admin-return-custody-proof']"), "RCPT-9911");
    await click(q(tree.container, "[data-testid='admin-return-custody-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "over-handing the rejected usable portion is refused locally");

    // A USABLE handback can never discharge the DAMAGED obligation: the two ceilings are separate.
    await setSelectValue(q(tree.container, "[data-testid='admin-return-custody-condition']"), "Damaged");
    await click(q(tree.container, "[data-testid='admin-return-custody-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "the damaged ceiling is one, not the whole rejected remainder");

    await setSelectValue(q(tree.container, "[data-testid='admin-return-custody-condition']"), "Usable");
    await setInputValue(q(tree.container, "[data-testid='admin-return-custody-quantity']"), "1");
    await click(q(tree.container, "[data-testid='admin-return-custody-confirm']"));
    await flush();

    const posts = stub.posts();
    assert.equal(posts.length, 1);
    assert.match(posts[0].url, /\/v1\/admin\/returns\/rr-279-a\/custody$/);
    assert.equal(posts[0].body?.returnItemId, "ri-1");
    assert.equal(posts[0].body?.quantity, 1);
    assert.equal(posts[0].body?.condition, "Usable");
    assert.equal(posts[0].body?.proofReference, "RCPT-9911");
    assert.equal(posts[0].body?.proofKind, "operator_attestation");
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});
