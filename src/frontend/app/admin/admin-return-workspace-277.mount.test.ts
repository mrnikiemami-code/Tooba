/**
 * TB-277 — the operator review surface of one return case: dialogs, bounded per-unit quantities and the explicit
 * separation of "approve" from "start the refund".
 *
 * These cases pin what the connected surface must do, mounted on the REAL workspace:
 *
 * 1. no review form lives inline on the page: every published action opens ONE design-system dialog, and closing it
 *    sends nothing;
 * 2. a quantity above the line's remaining ceiling is refused INSIDE the dialog, before any envelope is stored or any
 *    request leaves;
 * 3. the field is bounded by the ORDERING UNIT's precision (whole pieces vs fractional grams) and normalizes Persian
 *    digits and the Persian decimal separator;
 * 4. the usable and damaged portions of a line share ONE ceiling — neither portion funds the other;
 * 5. "approve" records only the decision (an Order operation, no money route), and the first refund is a separate
 *    reviewed command that travels to its own Returns route;
 * 6. a rejection reason shorter than three characters cannot be submitted.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-277.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { act, createElement } from "react";
import test from "node:test";
import {
  AdminReturnWorkspace,
  evaluateQuantityEntry,
  normalizeQuantityInput,
  quantityStep,
} from "./admin-return-workspace.tsx";
import {
  click,
  ensureDom,
  flush,
  mount,
  q,
  qq,
  setInputValue,
  waitFor,
} from "../../features/order-carrier-execution/test-dom-harness.ts";
import { returnIntentStorageKey, type ReturnReviewIntent } from "../returns/return-review-intent.ts";

ensureDom();

const CASE_ID = "rr-277-a";
const CHECKOUT_ID = "checkout-277-a";

type Call = { url: string; method: string; body: Record<string, unknown> | null };

function respond(status: number, payload: unknown): Response {
  return { ok: status >= 200 && status < 300, status, json: async () => payload } as unknown as Response;
}

type FixtureItem = {
  returnItemId: string;
  requested: number;
  received?: number;
  unitDecimalPlaces?: number | null;
  unitLabel?: string | null;
  productLabel?: string | null;
  /** TB-277: the OWNING stored disposition and its exact restocked portion. */
  disposition?: string;
  saleableRestockedQuantity?: number | null;
};

type FixtureWork = Record<string, unknown>;

function fixture(items: FixtureItem[], work: FixtureWork = {}) {
  return {
    returnRequestId: CASE_ID,
    sellerOrderId: "so-277",
    checkoutId: CHECKOUT_ID,
    sellerPartyId: "party-277",
    requestedByUserId: "user-277",
    status: "Requested",
    reason: "کالا مطابق سفارش نبود",
    currency: "IRR",
    refundAmount: 2407839,
    paymentId: "pay-277",
    refundDestination: "OriginalPayment",
    createdAt: "2026-11-16T08:00:00Z",
    updatedAt: "2026-11-16T08:00:00Z",
    origin: "CustomerDelivered",
    recordedDecision: null,
    items: items.map((item) => ({
      returnItemId: item.returnItemId,
      orderLineId: `ol-${item.returnItemId}`,
      quantity: item.requested,
      unitPriceSnapshot: 100000,
      currency: "IRR",
      condition: "Usable",
      productLabel: item.productLabel ?? `کالای ${item.returnItemId}`,
      unitLabel: item.unitLabel ?? "pcs",
      unitDecimalPlaces: item.unitDecimalPlaces ?? 0,
      disposition: item.disposition ?? "None",
      saleableRestockedQuantity: item.saleableRestockedQuantity ?? null,
    })),
    refundAttempts: [],
    receipts: [],
    receiving: {
      lines: items.map((item) => ({
        returnItemId: item.returnItemId,
        requested: item.requested,
        received: item.received ?? 0,
        usable: 0,
        damaged: 0,
      })),
      isComplete: false,
      isCoherent: true,
    },
    work: {
      categoryCode: "receiving",
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
      actionCodes: ["record_receipt"],
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
    if (method === "GET") return respond(200, detail);
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

function storedIntent(): ReturnReviewIntent | null {
  const raw = window.sessionStorage.getItem(returnIntentStorageKey(CASE_ID));
  return raw == null ? null : (JSON.parse(raw) as ReturnReviewIntent);
}

function openDialogs(container: HTMLElement): HTMLElement[] {
  return qq(container, "dialog[open]");
}

async function openAction(container: HTMLElement, actionId: string) {
  await waitFor(() => qq(container, `[data-testid='admin-return-action-${actionId}']`).length === 1);
  await click(q(container, `[data-testid='admin-return-action-${actionId}']`));
  await flush();
}

/** The harness helper types into `<input>`; a rejection reason is a `<textarea>`, so set it explicitly. */
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

test("no review form is rendered inline: every published action opens ONE dialog and sends nothing until confirmed", async () => {
  window.sessionStorage.clear();
  const snapshot = fixture([{ returnItemId: "ri-1", requested: 2 }], {
    categoryCode: "receipt",
    canRecordReceipt: true,
    canDecide: true,
    canStartRefund: true,
    canResumeRefund: true,
    canRetryRefund: true,
    canSettlePhysical: true,
    actionCodes: ["record_receipt", "approve_return", "reject_return", "start_refund", "resume_pending_refund", "retry_refund", "settle_physical_disposition"],
  });
  const stub = stubFetch(snapshot);
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);

    // Nothing is open, and no action form exists anywhere on the page before a deliberate click.
    assert.equal(openDialogs(tree.container).length, 0);
    assert.equal(qq(tree.container, "[data-testid$='-form']").length, 0);

    const expectations: Array<[string, string]> = [
      ["record-receipt", "admin-return-receipt-form"],
      ["approve", "admin-return-approve-form"],
      ["reject", "admin-return-reject-form"],
      ["start-refund", "admin-return-start-refund-form"],
      ["resume-refund", "admin-return-resume-form"],
      ["retry-refund", "admin-return-retry-form"],
      ["settle-physical", "admin-return-settle-form"],
    ];
    for (const [actionId, formTestId] of expectations) {
      await openAction(tree.container, actionId);
      // Exactly ONE modal surface, and the form lives inside it — never on the page body.
      assert.equal(openDialogs(tree.container).length, 1, `${actionId} must open exactly one dialog`);
      const form = q(tree.container, `[data-testid='${formTestId}']`);
      assert.notEqual(form.closest("dialog"), null, `${actionId} review belongs to a dialog`);
      assert.equal(stub.posts().length, 0, `${actionId} must not send on open`);
      // Cancel closes it and still sends nothing.
      const cancel = qq(tree.container, "dialog[open] button").find((button) => (button.textContent ?? "").trim() === "انصراف");
      assert.ok(cancel, `${actionId} must offer an explicit cancel`);
      await click(cancel as HTMLElement);
      await flush();
      assert.equal(openDialogs(tree.container).length, 0, `${actionId} closes without sending`);
      assert.equal(stub.posts().length, 0);
      assert.equal(storedIntent(), null);
    }
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("an over-ceiling quantity is refused inside the receipt dialog, before storage and before the request", async () => {
  window.sessionStorage.clear();
  const snapshot = fixture([{ returnItemId: "ri-1", requested: 1, unitLabel: "pcs" }], {
    categoryCode: "receipt",
    canRecordReceipt: true,
    actionCodes: ["record_receipt"],
  });
  const stub = stubFetch(snapshot);
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await openAction(tree.container, "record-receipt");
    assert.match(
      q(tree.container, "[data-testid='admin-return-receipt-ceiling-0']").textContent ?? "",
      /باقی‌ماندهٔ قابل دریافت/,
    );
    await setInputValue(q(tree.container, "[data-testid='admin-return-receipt-qty-0']"), "5");
    const confirm = q(tree.container, "[data-testid='admin-return-receipt-confirm']") as HTMLButtonElement;
    assert.equal(confirm.disabled, true, "an over-ceiling amount must disable the confirm action locally");
    await click(confirm);
    await flush();

    assert.equal(stub.posts().length, 0, "an over-ceiling amount must never reach the owning route");
    assert.equal(storedIntent(), null, "nothing may be frozen for an invalid quantity");
    assert.equal(openDialogs(tree.container).length, 1, "the dialog stays open so the amount can be corrected");
    // The bound is explained at the field itself, before any send.
    const problem = q(tree.container, "[data-testid='admin-return-receipt-line-0'] [role='alert']");
    assert.match(problem.textContent ?? "", /بیشتر است/);
    // The field itself is marked invalid, so the operator sees which control is wrong.
    assert.equal(q(tree.container, "[data-testid='admin-return-receipt-qty-0']").getAttribute("aria-invalid"), "true");
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("the unit's own precision bounds the field, and Persian digits/separator are normalized", async () => {
  window.sessionStorage.clear();
  const snapshot = fixture(
    [
      { returnItemId: "ri-pcs", requested: 3, unitLabel: "pcs", unitDecimalPlaces: 0 },
      { returnItemId: "ri-g", requested: 5, unitLabel: "g", unitDecimalPlaces: 3 },
    ],
    { categoryCode: "receipt", canRecordReceipt: true, actionCodes: ["record_receipt"] },
  );
  const stub = stubFetch(snapshot);
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await openAction(tree.container, "record-receipt");

    // A piece-count unit refuses a fractional amount…
    await setInputValue(q(tree.container, "[data-testid='admin-return-receipt-qty-0']"), "1.5");
    assert.equal(
      (q(tree.container, "[data-testid='admin-return-receipt-confirm']") as HTMLButtonElement).disabled,
      true,
      "a fractional piece-count amount must disable the confirm action locally",
    );
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "a fractional piece-count amount must be refused");
    assert.match(
      q(tree.container, "[data-testid='admin-return-receipt-line-0'] [role='alert']").textContent ?? "",
      /فقط مقدار صحیح/,
    );

    // …a fractional unit accepts the same exact amount…
    await setInputValue(q(tree.container, "[data-testid='admin-return-receipt-qty-0']"), "2");
    await setInputValue(q(tree.container, "[data-testid='admin-return-receipt-qty-1']"), "۱٫۲۵۰");
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await waitFor(() => stub.posts().length === 1);

    const post = stub.posts()[0];
    assert.equal(post.url, `/v1/admin/returns/${CASE_ID}/receipt`);
    const items = (post.body?.items ?? []) as Array<{ returnItemId: string; quantity: number; condition: string }>;
    assert.deepEqual(
      items.map((item) => [item.returnItemId, item.quantity, item.condition]),
      [
        ["ri-pcs", 2, "Usable"],
        // Persian digits and the Persian decimal separator became the exact stored amount, not a rounded one.
        ["ri-g", 1.25, "Usable"],
      ],
    );
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("usable and damaged portions of ONE line share the same ceiling", async () => {
  window.sessionStorage.clear();
  const snapshot = fixture([{ returnItemId: "ri-1", requested: 2 }], {
    categoryCode: "receipt",
    canRecordReceipt: true,
    actionCodes: ["record_receipt"],
  });
  const stub = stubFetch(snapshot);
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await openAction(tree.container, "record-receipt");

    // 2 usable + 1 damaged is 3 against a ceiling of 2: the damaged portion may not ride on the usable one.
    await setInputValue(q(tree.container, "[data-testid='admin-return-receipt-qty-0']"), "2");
    await setInputValue(q(tree.container, "[data-testid='admin-return-receipt-damaged-0']"), "1");
    assert.equal(
      (q(tree.container, "[data-testid='admin-return-receipt-confirm']") as HTMLButtonElement).disabled,
      true,
      "an over-ceiling usable+damaged total must disable the confirm action locally",
    );
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0);
    assert.match(
      q(tree.container, "[data-testid='admin-return-receipt-sum-problem']").textContent ?? "",
      /مجموع سالم و معیوب/,
    );

    // 1 + 1 is exactly the ceiling: both portions travel as separate inspected facts.
    await setInputValue(q(tree.container, "[data-testid='admin-return-receipt-qty-0']"), "1");
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    const items = (stub.posts()[0].body?.items ?? []) as Array<{ quantity: number; condition: string }>;
    assert.deepEqual(
      items.map((item) => [item.quantity, item.condition]),
      [
        [1, "Usable"],
        [1, "Damaged"],
      ],
    );
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("approve records the decision only, and the first refund is a separate reviewed command", async () => {
  window.sessionStorage.clear();
  const snapshot = fixture([{ returnItemId: "ri-1", requested: 2, received: 2 }], {
    categoryCode: "decision",
    canDecide: true,
    canStartRefund: true,
    actionCodes: ["approve_return", "reject_return", "start_refund"],
  });
  const stub = stubFetch(snapshot);
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await openAction(tree.container, "approve");
    const approveDialog = q(tree.container, "[data-testid='admin-return-approve-form']");
    assert.notEqual(approveDialog.closest("dialog"), null);
    // The dialog STATES that no money moves here — approval is not the refund.
    assert.match(approveDialog.textContent ?? "", /فقط تصمیم تأیید/);
    assert.match(approveDialog.textContent ?? "", /هیچ وجهی جابه‌جا نمی‌شود/);

    await click(q(tree.container, "[data-testid='admin-return-approve-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    const approvePost = stub.posts()[0];
    // The decision travels through the owning Order operations route, and NOTHING financial is called.
    assert.equal(approvePost.url, `/v1/admin/orders/${CHECKOUT_ID}/operations`);
    assert.equal(approvePost.body?.code, "approve_return");
    assert.equal(stub.posts().filter((call) => /refund|recover/.test(call.url)).length, 0);

    // The separate financial action is its own reviewed command on the Returns-owned route.
    await openAction(tree.container, "start-refund");
    const refundDialog = q(tree.container, "[data-testid='admin-return-start-refund-form']");
    assert.notEqual(refundDialog.closest("dialog"), null);
    assert.match(refundDialog.textContent ?? "", /اولین تلاش بازگشت وجه/);
    await click(q(tree.container, "[data-testid='admin-return-start-refund-confirm']"));
    await waitFor(() => stub.posts().length === 2);
    const refundPost = stub.posts()[1];
    assert.equal(refundPost.url, `/v1/admin/returns/${CASE_ID}/start-refund`);
    assert.equal(JSON.stringify(refundPost.body), JSON.stringify({ kind: "start_refund" }));
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a rejection reason shorter than three characters cannot be submitted", async () => {
  window.sessionStorage.clear();
  const snapshot = fixture([{ returnItemId: "ri-1", requested: 1, received: 1 }], {
    categoryCode: "decision",
    canDecide: true,
    actionCodes: ["approve_return", "reject_return"],
  });
  const stub = stubFetch(snapshot);
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await openAction(tree.container, "reject");
    const confirm = q(tree.container, "[data-testid='admin-return-reject-confirm']") as HTMLButtonElement;
    assert.equal(confirm.disabled, true, "an empty reason cannot be submitted");

    await setTextareaValue(q(tree.container, "[data-testid='admin-return-reject-reason']"), "اب");
    await flush();
    assert.equal((q(tree.container, "[data-testid='admin-return-reject-confirm']") as HTMLButtonElement).disabled, true);
    await click(q(tree.container, "[data-testid='admin-return-reject-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0);

    await setTextareaValue(q(tree.container, "[data-testid='admin-return-reject-reason']"), "کالا سالم بود");
    await click(q(tree.container, "[data-testid='admin-return-reject-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    assert.equal(stub.posts()[0].body?.code, "reject_return");
    assert.equal(stub.posts()[0].body?.reason, "کالا سالم بود");
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a case's items show their own stored disposition, so undisposed goods are never read as settled", async () => {
  window.sessionStorage.clear();

  // 1) The money side is done but nothing is disposed yet: the line states that honestly and the physical
  //    action stays offered, with a dialog that says what happens to each portion and that no money moves.
  const pending = fixture([{ returnItemId: "ri-1", requested: 2, received: 2, disposition: "None" }], {
    categoryCode: "disposition",
    canSettlePhysical: true,
    actionCodes: ["settle_physical_disposition"],
  });
  let stub = stubFetch(pending);
  let tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-item-disposition-0']").length === 1);
    assert.match(
      q(tree.container, "[data-testid='admin-return-item-disposition-0']").textContent ?? "",
      /تعیین‌نشده/,
      "a line with no stored disposition must never read as settled",
    );

    await openAction(tree.container, "settle-physical");
    const form = q(tree.container, "[data-testid='admin-return-settle-form']");
    assert.match(form.textContent ?? "", /موجودی قابل‌فروش/);
    assert.match(form.textContent ?? "", /هیچ وجهی/);
    assert.equal(stub.posts().length, 0, "opening the physical dialog sends nothing");

    await click(q(tree.container, "[data-testid='admin-return-settle-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    assert.equal(stub.posts()[0].url, `/v1/admin/returns/${CASE_ID}/recover`);
    assert.equal(JSON.stringify(stub.posts()[0].body), JSON.stringify({ kind: "settle_physical_disposition" }));
  } finally {
    await tree.unmount();
    stub.restore();
  }

  // 2) After settlement the SAME line reports the exact stored disposition and its restocked portion, and no
  //    review action is offered on a settled case.
  const settled = fixture(
    [
      {
        returnItemId: "ri-1",
        requested: 2,
        received: 2,
        disposition: "SaleableRestock",
        saleableRestockedQuantity: 1,
      },
    ],
    { categoryCode: "settled", isOpen: false, needsOperator: false, actionCodes: [] },
  );
  stub = stubFetch(settled);
  tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_ID }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-item-disposition-0']").length === 1);
    const cell = q(tree.container, "[data-testid='admin-return-item-disposition-0']").textContent ?? "";
    assert.match(cell, /برگشت به موجودی قابل فروش/, "the stored disposition is shown verbatim");
    assert.match(cell, /۱/, "the exact restocked portion is shown in Persian digits");
    assert.equal(
      qq(tree.container, "[data-testid^='admin-return-action-']").length,
      0,
      "a settled case offers no review action",
    );
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("the quantity guard is unit-aware and normalizes Persian input", () => {
  assert.equal(normalizeQuantityInput("۱٫۲۵۰"), "1.250");
  assert.equal(normalizeQuantityInput(" ۲ "), "2");
  assert.equal(parseOrNull("1.234567"), 1.234567);
  assert.equal(parseOrNull("0"), null, "zero is not a quantity of received goods");
  assert.equal(parseOrNull("1.0000001"), null, "the persisted scale is never rounded");

  // Whole pieces refuse a fraction; grams accept an exact fractional amount. Ceilings are EXACT millionths.
  const tenPieces = 10_000_000;
  assert.equal(evaluateQuantityEntry("1.5", 0, tenPieces).problem, "precision");
  assert.equal(evaluateQuantityEntry("1.5", 3, tenPieces).value, 1.5);
  assert.equal(evaluateQuantityEntry("1.2345", 3, tenPieces).problem, "precision");

  // The ceiling is inclusive, never a suggestion.
  assert.equal(evaluateQuantityEntry("10", 0, tenPieces).value, 10);
  assert.equal(evaluateQuantityEntry("11", 0, tenPieces).problem, "over_ceiling");

  assert.equal(quantityStep(0), "1");
  assert.equal(quantityStep(3), "0.001");
});

function parseOrNull(raw: string): number | null {
  const result = evaluateQuantityEntry(raw, 6, 10_000_000);
  return result.problem === null ? result.value : null;
}
