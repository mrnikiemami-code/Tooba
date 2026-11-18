/**
 * TB-280-R1 — closes the three Architect UX findings on the REAL AdminReturnWorkspace:
 * UX1 reviewed money display, UX2 condition-bound resolution dialog, UX3 per-product unit summary.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-280-r1.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { AdminReturnWorkspace } from "./admin-return-workspace.tsx";
import { click, ensureDom, flush, mount, q, qq, setInputValue, waitFor } from "../../features/order-carrier-execution/test-dom-harness.ts";
import { returnIntentStorageKey } from "../returns/return-review-intent.ts";

ensureDom();

type Call = { url: string; method: string; body: Record<string, unknown> | null };

function respond(status: number, payload: unknown): Response {
  return { ok: status >= 200 && status < 300, status, json: async () => payload } as unknown as Response;
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

function moneyCase(opts: {
  returnRequestId: string;
  refundAmount: number;
  approvedAmount: number | null;
  canStartRefund?: boolean;
  pendingId?: string | null;
  failedId?: string | null;
  attempts?: Array<Record<string, unknown>>;
}) {
  const actionCodes: string[] = [];
  if (opts.canStartRefund) actionCodes.push("start_refund");
  if (opts.pendingId) actionCodes.push("resume_pending_refund");
  if (opts.failedId) actionCodes.push("retry_refund");
  return {
    returnRequestId: opts.returnRequestId,
    sellerOrderId: "so-280r1",
    checkoutId: "checkout-280r1",
    sellerPartyId: "party-280r1",
    requestedByUserId: "user-280r1",
    status: "Approved",
    reason: "دلیل",
    currency: "IRR",
    refundAmount: opts.refundAmount,
    approvedAmount: opts.approvedAmount,
    paymentId: "pay-280r1",
    refundDestination: "OriginalPayment",
    createdAt: "2026-11-19T08:00:00Z",
    updatedAt: "2026-11-19T08:00:00Z",
    origin: "CustomerDelivered",
    recordedDecision: "Approve",
    effectiveDecision: "Approve",
    decisionVersion: 1,
    decisionHistory: [],
    items: [
      {
        returnItemId: "ri-1",
        orderLineId: "ol-1",
        quantity: 1,
        unitPriceSnapshot: opts.refundAmount,
        currency: "IRR",
        condition: "Usable",
        productLabel: "کالا",
        unitLabel: "pcs",
        unitDecimalPlaces: 0,
        disposition: "None",
        saleableRestockedQuantity: null,
      },
    ],
    resolution: {
      version: 1,
      isComplete: true,
      approvedAmount: opts.approvedAmount ?? 0,
      fromReviewedPlan: opts.approvedAmount !== null,
      lines: [
        {
          returnItemId: "ri-1",
          requested: 1,
          received: 1,
          usable: 1,
          damaged: 0,
          accepted: 1,
          rejected: 0,
          acceptedUsable: 1,
          acceptedDamaged: 0,
          rejectedReceivedUsable: 0,
          rejectedReceivedDamaged: 0,
          rejectedUnreceived: 0,
          reason: "پذیرفته شد",
          version: 1,
        },
      ],
      blockerCode: null,
    },
    custody: [],
    correction: { canReopenForReview: false, blockerCode: null },
    refundAttempts: opts.attempts ?? [],
    receiving: {
      lines: [{ returnItemId: "ri-1", requested: 1, received: 1, usable: 1, damaged: 0 }],
      isComplete: true,
      isCoherent: true,
    },
    receipts: [],
    work: {
      categoryCode: opts.failedId ? "refund_failed" : "refund",
      blockerCode: null,
      pendingRefundAttemptId: opts.pendingId ?? null,
      failedRefundAttemptId: opts.failedId ?? null,
      canRecordReceipt: false,
      canDecide: false,
      canResumeRefund: !!opts.pendingId,
      canRetryRefund: !!opts.failedId,
      canSettlePhysical: false,
      canStartRefund: !!opts.canStartRefund,
      isOpen: true,
      needsOperator: true,
      actionCodes,
    },
  };
}

function resolutionCase(opts: {
  returnRequestId: string;
  usable: number;
  damaged: number;
  unitLabel?: string;
  unitDecimalPlaces?: number;
  productLabel?: string;
  extraItem?: {
    returnItemId: string;
    productLabel: string;
    unitLabel: string;
    unitDecimalPlaces: number;
    requested: number;
    received: number;
    usable: number;
    damaged: number;
  } | null;
}) {
  const received = opts.usable + opts.damaged;
  const items = [
    {
      returnItemId: "ri-1",
      orderLineId: "ol-1",
      quantity: Math.max(received, 1),
      unitPriceSnapshot: 1000,
      currency: "IRR",
      condition: "Usable",
      productLabel: opts.productLabel ?? "کالای اصلی",
      unitLabel: opts.unitLabel ?? "pcs",
      unitDecimalPlaces: opts.unitDecimalPlaces ?? 0,
      disposition: "None",
      saleableRestockedQuantity: null,
    },
  ];
  const receivingLines = [
    {
      returnItemId: "ri-1",
      requested: Math.max(received, 1),
      received,
      usable: opts.usable,
      damaged: opts.damaged,
    },
  ];
  const resolutionLines = [
    {
      returnItemId: "ri-1",
      requested: Math.max(received, 1),
      received,
      usable: opts.usable,
      damaged: opts.damaged,
      accepted: received,
      rejected: Math.max(received, 1) - received,
      acceptedUsable: opts.usable,
      acceptedDamaged: opts.damaged,
      rejectedReceivedUsable: 0,
      rejectedReceivedDamaged: 0,
      rejectedUnreceived: Math.max(received, 1) - received,
      reason: null,
      version: 0,
    },
  ];
  if (opts.extraItem) {
    const x = opts.extraItem;
    items.push({
      returnItemId: x.returnItemId,
      orderLineId: "ol-2",
      quantity: x.requested,
      unitPriceSnapshot: 500,
      currency: "IRR",
      condition: "Usable",
      productLabel: x.productLabel,
      unitLabel: x.unitLabel,
      unitDecimalPlaces: x.unitDecimalPlaces,
      disposition: "None",
      saleableRestockedQuantity: null,
    });
    receivingLines.push({
      returnItemId: x.returnItemId,
      requested: x.requested,
      received: x.received,
      usable: x.usable,
      damaged: x.damaged,
    });
    resolutionLines.push({
      returnItemId: x.returnItemId,
      requested: x.requested,
      received: x.received,
      usable: x.usable,
      damaged: x.damaged,
      accepted: x.received,
      rejected: x.requested - x.received,
      acceptedUsable: x.usable,
      acceptedDamaged: x.damaged,
      rejectedReceivedUsable: 0,
      rejectedReceivedDamaged: 0,
      rejectedUnreceived: x.requested - x.received,
      reason: null,
      version: 0,
    });
  }
  return {
    returnRequestId: opts.returnRequestId,
    sellerOrderId: "so-280r1-res",
    checkoutId: "checkout-280r1-res",
    sellerPartyId: "party-280r1-res",
    requestedByUserId: "user-280r1-res",
    status: "Requested",
    reason: "دلیل",
    currency: "IRR",
    refundAmount: 3000,
    approvedAmount: null,
    paymentId: "pay-280r1-res",
    refundDestination: "OriginalPayment",
    createdAt: "2026-11-19T08:00:00Z",
    updatedAt: "2026-11-19T08:00:00Z",
    origin: "CustomerDelivered",
    recordedDecision: null,
    effectiveDecision: null,
    decisionVersion: 0,
    decisionHistory: [],
    items,
    resolution: {
      version: 0,
      isComplete: true,
      approvedAmount: 0,
      fromReviewedPlan: false,
      lines: resolutionLines,
      blockerCode: null,
    },
    custody: [],
    correction: { canReopenForReview: false, blockerCode: null },
    refundAttempts: [],
    receiving: {
      lines: receivingLines,
      isComplete: true,
      isCoherent: true,
    },
    receipts: [{ receiptId: "rc-1", recordedAt: "2026-11-19T08:01:00Z", actorUserId: "op", occurrenceKey: "occ-1", lines: [] }],
    work: {
      categoryCode: "decision",
      blockerCode: null,
      pendingRefundAttemptId: null,
      failedRefundAttemptId: null,
      canRecordReceipt: false,
      canDecide: true,
      canResumeRefund: false,
      canRetryRefund: false,
      canSettlePhysical: false,
      canStartRefund: false,
      isOpen: true,
      needsOperator: true,
      actionCodes: ["approve_return", "reject_return", "publish_resolution"],
    },
  };
}

test("M1 first-refund confirmation shows approved 2000 not requested 3000; approved zero stays zero; missing amount refuses send", async () => {
  {
    window.sessionStorage.clear();
    const caseId = "rr-280r1-m1a";
    const stub = stubFetch(moneyCase({ returnRequestId: caseId, refundAmount: 3000, approvedAmount: 2000, canStartRefund: true }));
    const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
    try {
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-start-refund']").length === 1);
      await click(q(tree.container, "[data-testid='admin-return-action-start-refund']"));
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-start-refund-amount']").length === 1);
      const amount = q(tree.container, "[data-testid='admin-return-start-refund-amount']").textContent ?? "";
      assert.match(amount, /۲٬?۰۰۰|2000/);
      assert.doesNotMatch(amount, /۳٬?۰۰۰|3000/);
      await click(q(tree.container, "[data-testid='admin-return-start-refund-confirm']"));
      await waitFor(() => stub.posts().length === 1);
      assert.match(stub.posts()[0]!.url, /start-refund/);
    } finally {
      await tree.unmount();
      stub.restore();
      window.sessionStorage.clear();
    }
  }
  {
    window.sessionStorage.clear();
    const caseId = "rr-280r1-m1b";
    const stub = stubFetch(moneyCase({ returnRequestId: caseId, refundAmount: 3000, approvedAmount: 0, canStartRefund: true }));
    const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
    try {
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-start-refund']").length === 1);
      await click(q(tree.container, "[data-testid='admin-return-action-start-refund']"));
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-start-refund-amount']").length === 1);
      const amount = q(tree.container, "[data-testid='admin-return-start-refund-amount']").textContent ?? "";
      assert.match(amount, /۰|0/);
      assert.doesNotMatch(amount, /۳٬?۰۰۰|3000/);
      assert.equal(stub.posts().length, 0);
    } finally {
      await tree.unmount();
      stub.restore();
      window.sessionStorage.clear();
    }
  }
  {
    window.sessionStorage.clear();
    const caseId = "rr-280r1-m1c";
    const stub = stubFetch(moneyCase({ returnRequestId: caseId, refundAmount: 3000, approvedAmount: null, canStartRefund: true }));
    const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
    try {
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-start-refund']").length === 1);
      await click(q(tree.container, "[data-testid='admin-return-action-start-refund']"));
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-start-refund-unavailable']").length === 1);
      const confirm = q(tree.container, "[data-testid='admin-return-start-refund-confirm']") as HTMLButtonElement;
      assert.equal(confirm.disabled, true);
      await click(confirm);
      await flush();
      assert.equal(stub.posts().length, 0);
    } finally {
      await tree.unmount();
      stub.restore();
      window.sessionStorage.clear();
    }
  }
});

test("M2 resume/retry show the bound attempt amount; mismatched binding cannot send", async () => {
  {
    window.sessionStorage.clear();
    const caseId = "rr-280r1-m2a";
    const stub = stubFetch(
      moneyCase({
        returnRequestId: caseId,
        refundAmount: 3000,
        approvedAmount: 2000,
        pendingId: "att-pending",
        attempts: [
          {
            refundAttemptId: "att-pending",
            paymentId: "pay-280r1",
            status: "Pending",
            amount: 1500,
            currency: "IRR",
            idempotencyKey: "key-pending",
            createdAt: "2026-11-19T09:00:00Z",
          },
        ],
      }),
    );
    const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
    try {
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-resume-refund']").length === 1);
      await click(q(tree.container, "[data-testid='admin-return-action-resume-refund']"));
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-resume-amount']").length === 1);
      const amount = q(tree.container, "[data-testid='admin-return-resume-amount']").textContent ?? "";
      assert.match(amount, /۱٬?۵۰۰|1500/);
      assert.doesNotMatch(amount, /۳٬?۰۰۰|3000/);
      assert.equal(stub.posts().length, 0);
    } finally {
      await tree.unmount();
      stub.restore();
      window.sessionStorage.clear();
    }
  }
  {
    window.sessionStorage.clear();
    const caseId = "rr-280r1-m2b";
    const stub = stubFetch(
      moneyCase({
        returnRequestId: caseId,
        refundAmount: 3000,
        approvedAmount: 2000,
        failedId: "att-failed",
        attempts: [
          {
            refundAttemptId: "att-other",
            paymentId: "pay-280r1",
            status: "Failed",
            amount: 1500,
            currency: "IRR",
            idempotencyKey: "key-other",
            createdAt: "2026-11-19T09:00:00Z",
          },
        ],
      }),
    );
    const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
    try {
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-retry-refund']").length === 1);
      await click(q(tree.container, "[data-testid='admin-return-action-retry-refund']"));
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-retry-unavailable']").length === 1);
      const confirm = q(tree.container, "[data-testid='admin-return-retry-confirm']") as HTMLButtonElement;
      assert.equal(confirm.disabled, true);
      await click(confirm);
      await flush();
      assert.equal(stub.posts().length, 0);
    } finally {
      await tree.unmount();
      stub.restore();
      window.sessionStorage.clear();
    }
  }
});

test("M3 damaged-only implicit baseline opens clean with only damaged field", async () => {
  window.sessionStorage.clear();
  const caseId = "rr-280r1-m3";
  const stub = stubFetch(resolutionCase({ returnRequestId: caseId, usable: 0, damaged: 1 }));
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-publish-resolution']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-publish-resolution']"));
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-resolution-form']").length === 1);
    assert.equal(qq(tree.container, "[role='alert']").length, 0);
    assert.equal(qq(tree.container, "[data-testid='admin-return-resolution-accepted-usable-0']").length, 0);
    const damaged = q(tree.container, "[data-testid='admin-return-resolution-accepted-damaged-0']") as HTMLInputElement;
    assert.match(damaged.value, /۱|1/);
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "reason missing => no POST");
    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-reason-0']"), "قلم معیوب است");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    const body = stub.posts()[0]!.body as { lines: Array<Record<string, number>> };
    assert.equal(body.lines[0]!.acceptedUsable, 0);
    assert.equal(body.lines[0]!.acceptedDamaged, 1);
    assert.equal(body.lines[0]!.rejectedReceivedUsable, 0);
    assert.equal(body.lines[0]!.rejectedReceivedDamaged, 0);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("M4 mixed implicit baseline: clean open, over-ceiling refuses, lawful Persian/fractional body", async () => {
  window.sessionStorage.clear();
  const caseId = "rr-280r1-m4";
  const stub = stubFetch(
    resolutionCase({
      returnRequestId: caseId,
      usable: 2.5,
      damaged: 1.25,
      unitLabel: "g",
      unitDecimalPlaces: 2,
      productLabel: "ادویه مخلوط",
    }),
  );
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-publish-resolution']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-publish-resolution']"));
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-resolution-accepted-usable-0']").length === 1);
    assert.equal(qq(tree.container, "[role='alert']").length, 0);
    const usable = q(tree.container, "[data-testid='admin-return-resolution-accepted-usable-0']") as HTMLInputElement;
    const damaged = q(tree.container, "[data-testid='admin-return-resolution-accepted-damaged-0']") as HTMLInputElement;
    assert.match(usable.value, /۲|2/);
    assert.match(damaged.value, /۱|1/);
    await setInputValue(usable, "۳");
    await flush();
    assert.equal(usable.getAttribute("aria-invalid"), "true");
    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-reason-0']"), "بررسی مخلوط");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "over-ceiling refuses send");
    await setInputValue(usable, "۱٫۵");
    await setInputValue(damaged, "۱٫۲۵");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    const body = stub.posts()[0]!.body as { lines: Array<Record<string, number>> };
    assert.equal(body.lines[0]!.acceptedUsable, 1.5);
    assert.equal(body.lines[0]!.acceptedDamaged, 1.25);
    assert.equal(body.lines[0]!.rejectedReceivedUsable, 1);
    assert.equal(body.lines[0]!.rejectedReceivedDamaged, 0);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("M5 zero-received sibling item has no quantity inputs and stays in the five-portion body", async () => {
  window.sessionStorage.clear();
  const caseId = "rr-280r1-m5";
  const stub = stubFetch(
    resolutionCase({
      returnRequestId: caseId,
      usable: 1,
      damaged: 0,
      extraItem: {
        returnItemId: "ri-2",
        productLabel: "قلم نرسیده",
        unitLabel: "pcs",
        unitDecimalPlaces: 0,
        requested: 1,
        received: 0,
        usable: 0,
        damaged: 0,
      },
    }),
  );
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-publish-resolution']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-publish-resolution']"));
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-resolution-not-received-1']").length === 1);
    assert.equal(qq(tree.container, "[data-testid='admin-return-resolution-accepted-usable-1']").length, 0);
    assert.equal(qq(tree.container, "[data-testid='admin-return-resolution-accepted-damaged-1']").length, 0);
    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-reason-0']"), "سالم پذیرفته شد");
    await setInputValue(q(tree.container, "[data-testid='admin-return-resolution-reason-1']"), "این قلم نرسید");
    await click(q(tree.container, "[data-testid='admin-return-resolution-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    const body = stub.posts()[0]!.body as { lines: Array<Record<string, number | string>> };
    assert.equal(body.lines.length, 2);
    assert.equal(body.lines[1]!.acceptedUsable, 0);
    assert.equal(body.lines[1]!.acceptedDamaged, 0);
    assert.equal(body.lines[1]!.rejectedUnreceived, 1);
    assert.equal(body.lines[1]!.reason, "این قلم نرسید");
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("M6 pcs + grams never becomes 501 of 501", async () => {
  window.sessionStorage.clear();
  const caseId = "rr-280r1-m6";
  const stub = stubFetch(
    resolutionCase({
      returnRequestId: caseId,
      usable: 1,
      damaged: 0,
      unitLabel: "pcs",
      unitDecimalPlaces: 0,
      productLabel: "کالای اصلی",
      extraItem: {
        returnItemId: "ri-2",
        productLabel: "ادویه",
        unitLabel: "g",
        unitDecimalPlaces: 0,
        requested: 500,
        received: 500,
        usable: 500,
        damaged: 0,
      },
    }),
  );
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-received-summary']").length === 1);
    const summary = q(tree.container, "[data-testid='admin-return-received-summary']").textContent ?? "";
    assert.doesNotMatch(summary, /۵۰۱|501/);
    assert.match(summary, /کالای اصلی/);
    assert.match(summary, /ادویه/);
    assert.match(summary, /عدد/);
    assert.match(summary, /گرم/);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("M7 zero-received primary remains receipt; cancel stores nothing; settled stays read-only", async () => {
  {
    window.sessionStorage.clear();
    const caseId = "rr-280r1-m7a";
    const payload = {
      ...resolutionCase({ returnRequestId: caseId, usable: 0, damaged: 0 }),
      receiving: {
        lines: [{ returnItemId: "ri-1", requested: 1, received: 0, usable: 0, damaged: 0 }],
        isComplete: false,
        isCoherent: true,
      },
      resolution: { version: 0, isComplete: false, approvedAmount: 0, fromReviewedPlan: false, lines: [], blockerCode: null },
      receipts: [],
      items: [
        {
          returnItemId: "ri-1",
          orderLineId: "ol-1",
          quantity: 1,
          unitPriceSnapshot: 1000,
          currency: "IRR",
          condition: "Usable",
          productLabel: "کالا",
          unitLabel: "pcs",
          unitDecimalPlaces: 0,
          disposition: "None",
          saleableRestockedQuantity: null,
        },
      ],
      work: {
        categoryCode: "receiving",
        blockerCode: null,
        pendingRefundAttemptId: null,
        failedRefundAttemptId: null,
        canRecordReceipt: true,
        canDecide: false,
        canResumeRefund: false,
        canRetryRefund: false,
        canSettlePhysical: false,
        canStartRefund: false,
        isOpen: true,
        needsOperator: true,
        actionCodes: ["record_receipt"],
      },
    };
    const stub = stubFetch(payload);
    const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
    try {
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
      assert.equal(qq(tree.container, "[data-testid='admin-return-action-publish-resolution']").length, 0);
      await click(q(tree.container, "[data-testid='admin-return-action-record-receipt']"));
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-receipt-form']").length === 1);
      assert.equal(qq(tree.container, "[role='alert']").length, 0);
      const cancel = qq(tree.container, "button").find((button) => (button.textContent ?? "").trim() === "انصراف");
      assert.ok(cancel);
      await click(cancel!);
      await flush();
      assert.equal(stub.posts().length, 0);
      assert.equal(window.sessionStorage.getItem(returnIntentStorageKey(caseId)), null);
    } finally {
      await tree.unmount();
      stub.restore();
      window.sessionStorage.clear();
    }
  }
  {
    window.sessionStorage.clear();
    const caseId = "rr-280r1-m7b";
    const payload = {
      ...moneyCase({ returnRequestId: caseId, refundAmount: 2000, approvedAmount: 2000 }),
      status: "Completed",
      work: {
        categoryCode: "settled",
        blockerCode: null,
        pendingRefundAttemptId: null,
        failedRefundAttemptId: null,
        canRecordReceipt: false,
        canDecide: false,
        canResumeRefund: false,
        canRetryRefund: false,
        canSettlePhysical: false,
        canStartRefund: false,
        isOpen: false,
        needsOperator: false,
        actionCodes: [],
      },
      refundAttempts: [
        {
          refundAttemptId: "att-ok",
          paymentId: "pay-280r1",
          status: "Succeeded",
          amount: 2000,
          currency: "IRR",
          idempotencyKey: "key-ok",
          createdAt: "2026-11-19T09:00:00Z",
        },
      ],
    };
    const stub = stubFetch(payload);
    const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: caseId }));
    try {
      await waitFor(() => qq(tree.container, "[data-testid='admin-return-workspace-detail']").length === 1);
      assert.equal(qq(tree.container, "[data-testid^='admin-return-action-']").length, 0);
      assert.equal(stub.posts().length, 0);
    } finally {
      await tree.unmount();
      stub.restore();
      window.sessionStorage.clear();
    }
  }
});
