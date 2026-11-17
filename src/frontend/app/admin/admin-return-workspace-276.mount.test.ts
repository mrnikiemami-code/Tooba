/**
 * TB-276 — the CONNECTED reviewed workflow of one return case, mounted on the real workspace.
 *
 * TB-275 proved the pieces in isolation. These cases pin what the connected surface must do:
 *
 * 1. the reviewed envelope (case + route + action + occurrence + EXACT body) is in storage — verified by read-back —
 *    BEFORE the first request leaves, and the wire body is that frozen body;
 * 2. one click burst cannot produce two commands;
 * 3. a transport/5xx/unknown answer NEVER clears the envelope; the operator retries the identical bytes and the
 *    workspace can only be released by an authoritative or proven-no-effect answer;
 * 4. a proven no-effect refusal releases it with a Persian notice;
 * 5. a fresh mount recovers the retained envelope from storage without sending anything;
 * 6. a late answer for an OLD case can never paint on, or release, the NEW case's surface (presentation epoch).
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-276.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { AdminReturnWorkspace } from "./admin-return-workspace.tsx";
import {
  click,
  clickBurst,
  ensureDom,
  flush,
  mount,
  q,
  qq,
  setInputValue,
  waitFor,
} from "../../features/order-carrier-execution/test-dom-harness.ts";
import {
  buildReturnReviewIntent,
  returnIntentStorageKey,
  serializeReturnReviewIntent,
  type ReturnReviewIntent,
} from "../returns/return-review-intent.ts";

ensureDom();

const CASE_A = "rr-276-a";
const CASE_B = "rr-276-b";
const CHECKOUT_A = "checkout-276-a";

type Call = { url: string; method: string; body: Record<string, unknown> | null };

type Reply = { status: number; payload: unknown };

function respond(status: number, payload: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => payload,
  } as unknown as Response;
}

/** A stored case snapshot whose OWN work description says a receipt is the next lawful action. */
function snapshot(returnRequestId: string, overrides: Record<string, unknown> = {}) {
  return {
    returnRequestId,
    sellerOrderId: "so-276",
    checkoutId: CHECKOUT_A,
    sellerPartyId: "party-276",
    requestedByUserId: "user-276",
    status: "Requested",
    reason: "کالا مطابق سفارش نبود",
    currency: "IRR",
    refundAmount: 1200000,
    paymentId: "pay-276",
    refundDestination: "OriginalPayment",
    createdAt: "2026-11-16T08:00:00Z",
    updatedAt: "2026-11-16T08:00:00Z",
    origin: "CustomerDelivered",
    items: [
      {
        returnItemId: "ri-1",
        orderLineId: "ol-1",
        quantity: 2,
        unitPriceSnapshot: 600000,
        currency: "IRR",
        condition: "Usable",
      },
    ],
    refundAttempts: [],
    receipts: [],
    receiving: null,
    work: {
      categoryCode: "receiving",
      blockerCode: null,
      pendingRefundAttemptId: null,
      failedRefundAttemptId: null,
      canRecordReceipt: true,
      canDecide: true,
      canResumeRefund: false,
      canRetryRefund: false,
      canSettlePhysical: false,
      isOpen: true,
      needsOperator: true,
      actionCodes: ["record_receipt"],
    },
    ...overrides,
  };
}

/**
 * Installs a fetch stub for the workspace. Every call is recorded; `answer` decides what the owning route returns.
 * The detail GET is answered from `detail` so a test can also control what the fresh read says.
 */
function stubFetch(options: {
  detail?: (returnRequestId: string) => unknown;
  answer?: (call: Call) => Reply | Promise<Reply>;
}) {
  const original = globalThis.fetch;
  const calls: Call[] = [];
  globalThis.fetch = (async (input: unknown, init?: { method?: string; body?: unknown }) => {
    const url = typeof input === "string" ? input : String((input as { url?: string })?.url ?? input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = typeof init?.body === "string" ? (JSON.parse(init.body) as Record<string, unknown>) : null;
    const call: Call = { url, method, body };
    calls.push(call);
    if (method === "GET" && url.startsWith("/v1/admin/returns/")) {
      const id = decodeURIComponent(url.slice("/v1/admin/returns/".length));
      return respond(200, options.detail ? options.detail(id) : snapshot(id));
    }
    const reply = await (options.answer ? options.answer(call) : { status: 200, payload: snapshot(CASE_A) });
    return respond(reply.status, reply.payload);
  }) as typeof fetch;
  return {
    calls,
    posts: () => calls.filter((call) => call.method === "POST"),
    restore() {
      globalThis.fetch = original;
    },
  };
}

function stored(returnRequestId: string): ReturnReviewIntent | null {
  const raw = window.sessionStorage.getItem(returnIntentStorageKey(returnRequestId));
  if (raw == null) return null;
  return JSON.parse(raw) as ReturnReviewIntent;
}

function seed(returnRequestId: string, intent: ReturnReviewIntent) {
  window.sessionStorage.setItem(returnIntentStorageKey(returnRequestId), serializeReturnReviewIntent(intent));
}

/** Fills the receipt dialog's usable portion for the single fixture line and returns the body the workspace freezes. */
async function fillReceipt(container: HTMLElement) {
  // TB-277: the receipt dialog states the usable and damaged portions separately under one line ceiling.
  await setInputValue(q(container, "[data-testid='admin-return-receipt-qty-0']"), "2");
  await flush();
}

test("the reviewed receipt is durable BY READ-BACK before the first request, and the wire body is that frozen body", async () => {
  window.sessionStorage.clear();
  let storageAtSend: string | null = null;
  const stub = stubFetch({
    answer: (call) => {
      // Read the storage INSIDE the request: this is the only truthful place to prove "durable before first send".
      storageAtSend = window.sessionStorage.getItem(returnIntentStorageKey(CASE_A));
      return { status: 200, payload: snapshot(CASE_A, { status: "Requested" }) };
    },
  });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-record-receipt']"));
    await fillReceipt(tree.container);
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await waitFor(() => stub.posts().length === 1);

    assert.ok(storageAtSend, "the envelope must exist in storage before the request runs");
    const persisted = JSON.parse(storageAtSend) as ReturnReviewIntent;
    assert.equal(persisted.version, "v2");
    assert.equal(persisted.action, "receipt");
    assert.equal(persisted.target, "returns");
    assert.equal(persisted.returnRequestId, CASE_A);
    assert.equal(persisted.routeTargetId, null);

    const post = stub.posts()[0];
    assert.equal(post.url, `/v1/admin/returns/${CASE_A}/receipt`);
    // The transport body IS the frozen body: no field is rebuilt from the newer snapshot.
    assert.equal(JSON.stringify(post.body), persisted.requestBody);

    // An authoritative answer releases the exact command.
    await waitFor(() => stored(CASE_A) === null);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("one click burst produces exactly one command", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch({
    answer: () => new Promise<Reply>((resolve) => setTimeout(() => resolve({ status: 200, payload: snapshot(CASE_A) }), 20)),
  });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-record-receipt']"));
    await fillReceipt(tree.container);
    await clickBurst(q(tree.container, "[data-testid='admin-return-receipt-confirm']"), 3);
    await waitFor(() => stub.posts().length >= 1);
    await flush();
    assert.equal(stub.posts().length, 1);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a 5xx/unknown answer keeps the exact envelope, and the retry re-sends the identical bytes", async () => {
  window.sessionStorage.clear();
  let attempt = 0;
  const stub = stubFetch({
    answer: () => {
      attempt += 1;
      // No typed errorCode at all: a gateway-style body is uncertainty, never a proven no-effect refusal.
      return attempt === 1 ? { status: 503, payload: { title: "Unavailable" } } : { status: 200, payload: snapshot(CASE_A) };
    },
  });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-record-receipt']"));
    await fillReceipt(tree.container);
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await waitFor(() => stub.posts().length === 1);

    await waitFor(() => qq(tree.container, "[data-testid='admin-return-unresolved-intent']").length === 1);
    const retained = stored(CASE_A);
    assert.ok(retained, "an unknown answer must never clear the reviewed command");
    assert.equal(qq(tree.container, "[data-testid='admin-return-notice']").length, 1);

    // The retained card is the ONLY way forward: it re-sends the SAME reviewed command, not a new one.
    await click(q(tree.container, "[data-testid='admin-return-unresolved-retry']"));
    await waitFor(() => stub.posts().length === 2);
    assert.equal(JSON.stringify(stub.posts()[1].body), retained.requestBody);
    assert.equal(stub.posts()[1].url, stub.posts()[0].url);
    await waitFor(() => stored(CASE_A) === null);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a proven no-effect refusal releases the command and tells the operator in Persian", async () => {
  window.sessionStorage.clear();
  const stub = stubFetch({
    answer: () => ({ status: 409, payload: { errorCode: "return.receipt.over_received" } }),
  });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-record-receipt']"));
    await fillReceipt(tree.container);
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    await waitFor(() => stored(CASE_A) === null);

    const notice = q(tree.container, "[data-testid='admin-return-notice']");
    assert.match(notice.textContent ?? "", /بیشتر است/);
    // No raw machine token and no English title reaches the operator.
    assert.doesNotMatch(notice.textContent ?? "", /return\.receipt|Bad Request|error/i);
    assert.equal(qq(tree.container, "[data-testid='admin-return-unresolved-intent']").length, 0);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a fresh mount recovers the retained envelope from storage and sends nothing by itself", async () => {
  window.sessionStorage.clear();
  const intent = buildReturnReviewIntent({
    action: "receipt",
    target: "returns",
    returnRequestId: CASE_A,
    occurrenceKey: "occurrence-276-retained",
    body: { occurrenceKey: "occurrence-276-retained", items: [{ returnItemId: "ri-1", quantity: 2, condition: "Usable" }] },
  });
  assert.ok(intent);
  seed(CASE_A, intent);

  const stub = stubFetch({ answer: () => ({ status: 200, payload: snapshot(CASE_A) }) });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-unresolved-intent']").length === 1);
    // Recovery is a READ: mounting must never re-send a possibly-applied command.
    assert.equal(stub.posts().length, 0);
    assert.equal(stored(CASE_A)?.occurrenceKey, "occurrence-276-retained");

    await click(q(tree.container, "[data-testid='admin-return-unresolved-retry']"));
    await waitFor(() => stub.posts().length === 1);
    assert.equal(JSON.stringify(stub.posts()[0].body), intent.requestBody);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a late answer for the OLD case never paints on, or releases, the NEW case surface", async () => {
  window.sessionStorage.clear();
  let release: (() => void) | null = null;
  const stub = stubFetch({
    detail: (id) =>
      id === CASE_A
        ? snapshot(id, {
            work: {
              ...(snapshot(id).work as Record<string, unknown>),
              categoryCode: "decision",
              canRecordReceipt: false,
              actionCodes: ["approve_return", "reject_return"],
            },
          })
        : snapshot(id),
    answer: () =>
      new Promise<Reply>((resolve) => {
        release = () => resolve({ status: 200, payload: snapshot(CASE_A) });
      }),
  });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-approve']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-approve']"));
    await click(q(tree.container, "[data-testid='admin-return-approve-confirm']"));
    await waitFor(() => stub.posts().length === 1);
    const approved = stored(CASE_A);
    assert.ok(approved, "the reviewed decision is durable before the request");

    // RETARGET: the same surface now shows another case while the old answer is still in flight.
    await tree.rerender(createElement(AdminReturnWorkspace, { returnRequestId: CASE_B }));
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
    assert.ok(release, "the old request is still unresolved");

    (release as () => void)();
    await flush();
    await flush();

    // The old answer is fenced out: it may not release the old envelope or toast on the new case.
    assert.ok(stored(CASE_A), "a fenced answer must not clear its own envelope on a newer surface");
    assert.equal(qq(tree.container, "[data-testid='admin-return-notice']").length, 0);
    assert.equal(stub.posts().length, 1);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a malformed success payload is never treated as an applied command", async () => {
  window.sessionStorage.clear();
  // 200 with no snapshot: a transport-level "ok" that proves nothing about the reviewed command.
  const stub = stubFetch({ answer: () => ({ status: 200, payload: null }) });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-record-receipt']"));
    await fillReceipt(tree.container);
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await waitFor(() => stub.posts().length === 1);

    await waitFor(() => qq(tree.container, "[data-testid='admin-return-unresolved-intent']").length === 1);
    assert.ok(stored(CASE_A), "an empty answer must keep the exact reviewed command");
    assert.equal(qq(tree.container, "[data-testid='admin-return-notice']").length, 1);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a throwing storage getter refuses to send and never crashes the render", async () => {
  window.sessionStorage.clear();
  const descriptor = Object.getOwnPropertyDescriptor(window, "sessionStorage");
  Object.defineProperty(window, "sessionStorage", {
    configurable: true,
    get() {
      throw new Error("storage denied");
    },
  });
  const stub = stubFetch({ answer: () => ({ status: 200, payload: snapshot(CASE_A) }) });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-storage-unavailable']").length === 1);
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
    // No durable intent can be proven, so the surface must not open a review it could never persist.
    const action = q(tree.container, "[data-testid='admin-return-action-record-receipt']") as HTMLButtonElement;
    assert.equal(action.disabled, true);
    await click(action);
    await flush();
    assert.equal(qq(tree.container, "[data-testid='admin-return-receipt-form']").length, 0);
    assert.equal(stub.posts().length, 0, "no durable intent can be proven, so nothing may be sent");
  } finally {
    await tree.unmount();
    stub.restore();
    if (descriptor) Object.defineProperty(window, "sessionStorage", descriptor);
    window.sessionStorage.clear();
  }
});

test("a storage that throws on write refuses to send", async () => {
  window.sessionStorage.clear();
  const descriptor = Object.getOwnPropertyDescriptor(window, "sessionStorage");
  const throwing = {
    length: 0,
    getItem: () => null,
    setItem: () => {
      throw new Error("quota exceeded");
    },
    removeItem: () => undefined,
    clear: () => undefined,
    key: () => null,
  };
  Object.defineProperty(window, "sessionStorage", { configurable: true, value: throwing });
  const stub = stubFetch({ answer: () => ({ status: 200, payload: snapshot(CASE_A) }) });
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-action-record-receipt']").length === 1);
    await click(q(tree.container, "[data-testid='admin-return-action-record-receipt']"));
    await fillReceipt(tree.container);
    await click(q(tree.container, "[data-testid='admin-return-receipt-confirm']"));
    await flush();
    assert.equal(stub.posts().length, 0, "a write that cannot be verified must block the send");
    assert.equal(qq(tree.container, "[data-testid='admin-return-notice']").length, 1);
  } finally {
    await tree.unmount();
    stub.restore();
    if (descriptor) Object.defineProperty(window, "sessionStorage", descriptor);
    window.sessionStorage.clear();
  }
});

test("the unresolved card never disappears on a failed or missing fresh read", async () => {
  window.sessionStorage.clear();
  const intent = buildReturnReviewIntent({
    action: "receipt",
    target: "returns",
    returnRequestId: CASE_A,
    occurrenceKey: "occurrence-276-elsewhere",
    body: { occurrenceKey: "occurrence-276-elsewhere", items: [{ returnItemId: "ri-1", quantity: 1, condition: "Damaged" }] },
  });
  assert.ok(intent);
  seed(CASE_A, intent);

  const original = globalThis.fetch;
  globalThis.fetch = (async () =>
    ({ ok: false, status: 500, json: async () => ({ errorCode: "return.missing" }) }) as unknown as Response) as typeof fetch;
  const tree = await mount(createElement(AdminReturnWorkspace, { returnRequestId: CASE_A }));
  try {
    await waitFor(() => qq(tree.container, "[data-testid='admin-return-unresolved-intent']").length === 1);
    assert.ok(stored(CASE_A), "a failed detail read must never hide real retained evidence");
  } finally {
    await tree.unmount();
    globalThis.fetch = original;
    window.sessionStorage.clear();
  }
});
