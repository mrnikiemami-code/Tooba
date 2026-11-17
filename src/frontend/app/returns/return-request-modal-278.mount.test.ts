/**
 * TB-278 — the customer's ONE deliberate multi-seller return request, mounted on the real modal.
 *
 * These cases pin what the served surface must do:
 *
 *  1. the modal lists the OWNING eligibility of every seller order of the checkout with the line's real label, unit,
 *     exact remaining ceiling and the line's own precision — nothing is preselected or auto-filled;
 *  2. a quantity that violates the line's unit (a fraction of a piece) or its remaining ceiling is refused LOCALLY,
 *     before any request, while a lawful 2-of-3 from seller A and a fractional item from seller B are accepted;
 *  3. one deliberate act creates ONE child per selected line across sellers, each with its OWN frozen bytes and
 *     idempotency key, and the unselected lines are never sent;
 *  4. a child whose answer never arrived keeps its exact bytes, the surface states that the outcome is uncertain and
 *     the resume action re-sends byte-identical bytes; the recorded child is never re-sent;
 *  5. the outcome is announced through the shared `react-toastify` surface, not a private floating box.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/returns/return-request-modal-278.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { act, createElement } from "react";
import test from "node:test";
import { ToastContainer } from "react-toastify";
import {
  ReturnRequestModal,
  type ReturnRequestSeller,
} from "./return-request-modal.tsx";
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

ensureDom();

const CHECKOUT_ID = "checkout-278-a";

type Call = { url: string; method: string; raw: string | null; body: Record<string, unknown> | null };

function respond(status: number, payload: unknown): Response {
  return { ok: status >= 200 && status < 300, status, json: async () => payload } as unknown as Response;
}

function snapshot(id: string, sellerOrderId: string, checkoutId = CHECKOUT_ID) {
  return {
    returnRequestId: id,
    sellerOrderId,
    checkoutId,
    sellerPartyId: "party-278",
    requestedByUserId: "user-278",
    status: "Requested",
    reason: "خرابی کالا",
    currency: "IRR",
    refundAmount: 0,
    paymentId: null,
    refundDestination: "OriginalPayment",
    createdAt: "2026-11-17T00:00:00Z",
    updatedAt: "2026-11-17T00:00:00Z",
    origin: "CustomerDelivered",
    items: [],
    refundAttempts: [],
  };
}

type Answer =
  | { kind: "ok"; id: string }
  | { kind: "error"; status: number; code: string }
  | { kind: "transport" };

function stubCustomerReturns(answers: Answer[]) {
  const original = globalThis.fetch;
  const calls: Call[] = [];
  globalThis.fetch = (async (input: unknown, init?: { method?: string; body?: unknown }) => {
    const url = typeof input === "string" ? input : String((input as { url?: string })?.url ?? input);
    const method = (init?.method ?? "GET").toUpperCase();
    const raw = typeof init?.body === "string" ? init.body : null;
    const body = raw ? (JSON.parse(raw) as Record<string, unknown>) : null;
    calls.push({ url, method, raw, body });
    const answer = answers[Math.min(calls.length - 1, answers.length - 1)];
    if (answer.kind === "transport") throw new TypeError("Failed to fetch");
    if (answer.kind === "error") {
      return { ok: false, status: answer.status, json: async () => ({ errorCode: answer.code }) } as unknown as Response;
    }
    const sellerOrderId = String(body?.sellerOrderId ?? "");
    return respond(200, snapshot(answer.id, sellerOrderId));
  }) as typeof fetch;
  return {
    calls,
    posts: () => calls.filter((call) => call.method === "POST"),
    restore() {
      globalThis.fetch = original;
    },
  };
}

function sellers(): ReturnRequestSeller[] {
  return [
    {
      sellerOrderId: "so-a",
      sellerDisplayName: "فروشگاه الف",
      orderNumber: "SO-A-1",
      eligibility: {
        sellerOrderId: "so-a",
        checkoutId: CHECKOUT_ID,
        currency: "IRR",
        eligible: true,
        reasonCode: "",
        eligibleUntil: "2026-11-25T00:00:00Z",
        lastDeliveredAt: "2026-11-15T00:00:00Z",
        lines: [
          {
            orderLineId: "ol-socks",
            productLabel: "جوراب نخی",
            unitLabel: "pcs",
            unitDecimalPlaces: 0,
            deliveredQuantity: 3,
            alreadyReturnedQuantity: 0,
            remainingReturnableQuantity: 3,
            isReturnable: true,
            returnWindowDays: 7,
            returnableUntil: "2026-11-25T00:00:00Z",
            blockerCode: null,
            selectable: true,
          },
          {
            orderLineId: "ol-shirt",
            productLabel: "پیراهن آستین‌بلند",
            unitLabel: "pcs",
            unitDecimalPlaces: 0,
            deliveredQuantity: 1,
            alreadyReturnedQuantity: 1,
            remainingReturnableQuantity: 0,
            isReturnable: false,
            returnWindowDays: 7,
            returnableUntil: "2026-11-25T00:00:00Z",
            blockerCode: "nothing_returnable",
            selectable: false,
          },
        ],
      },
    },
    {
      sellerOrderId: "so-b",
      sellerDisplayName: "فروشگاه ب",
      orderNumber: "SO-B-1",
      eligibility: {
        sellerOrderId: "so-b",
        checkoutId: CHECKOUT_ID,
        currency: "IRR",
        eligible: true,
        reasonCode: "",
        eligibleUntil: "2026-11-25T00:00:00Z",
        lastDeliveredAt: "2026-11-15T00:00:00Z",
        lines: [
          {
            orderLineId: "ol-lamp",
            productLabel: "چراغ مطالعه",
            unitLabel: "g",
            unitDecimalPlaces: 3,
            deliveredQuantity: 2,
            alreadyReturnedQuantity: 0,
            remainingReturnableQuantity: 2,
            isReturnable: true,
            returnWindowDays: 7,
            returnableUntil: "2026-11-25T00:00:00Z",
            blockerCode: null,
            selectable: true,
          },
        ],
      },
    },
  ];
}

async function setSelectValue(el: Element, value: string) {
  const select = el as HTMLSelectElement;
  const setter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, "value")?.set;
  if (!setter) throw new Error("HTMLSelectElement value setter missing");
  await act(async () => {
    setter.call(select, value);
    select.dispatchEvent(new Event("change", { bubbles: true }));
  });
  await flush();
}

async function setTextarea(el: Element, value: string) {
  const textarea = el as HTMLTextAreaElement;
  const setter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value")?.set;
  if (!setter) throw new Error("HTMLTextAreaElement value setter missing");
  await act(async () => {
    setter.call(textarea, value);
    textarea.dispatchEvent(new Event("input", { bubbles: true }));
  });
  await flush();
}

async function toggleLine(container: HTMLElement, id: string) {
  const box = q(container, `[data-testid='return-line-select-${id}']`) as HTMLInputElement;
  await click(box);
}

function toastTexts(container: HTMLElement): string[] {
  return qq(container, ".Toastify__toast").map((node) => node.textContent ?? "");
}

async function mountModal(sellersInput: ReturnRequestSeller[], onSubmitted?: (count: number) => void) {
  return mount(
    createElement(
      "div",
      null,
      createElement(ReturnRequestModal, {
        open: true,
        onClose: () => undefined,
        checkoutId: CHECKOUT_ID,
        orderReference: "REF-278",
        sellers: sellersInput,
        onSubmitted: (snaps) => onSubmitted?.(snaps.length),
      }),
      createElement(ToastContainer, { autoClose: false, newestOnTop: true }),
    ),
  );
}

test("the modal shows the authoritative per-seller eligibility and preselects nothing", async () => {
  const stub = stubCustomerReturns([{ kind: "ok", id: "rr-0" }]);
  const tree = await mountModal(sellers());
  try {
    await flush();
    assert.match(q(tree.container, "[data-testid='return-line-ceiling-ol-socks']").textContent ?? "", /حداکثر قابل مرجوعی: 3/);
    assert.match(q(tree.container, "[data-testid='return-line-ceiling-ol-lamp']").textContent ?? "", /حداکثر قابل مرجوعی: 2/);
    // The already-returned line is shown with its own truthful blocker, not hidden and not offered.
    assert.equal(
      (q(tree.container, "[data-testid='return-line-select-ol-shirt']") as HTMLInputElement).disabled,
      true,
    );
    assert.match(tree.container.textContent ?? "", /مقدار قابل مرجوعی این قلم تمام شده است/);
    // Nothing is checked, no quantity control exists and no request left the surface.
    assert.equal(
      (q(tree.container, "[data-testid='return-line-select-ol-socks']") as HTMLInputElement).checked,
      false,
    );
    assert.equal(qq(tree.container, "[data-testid='return-line-quantity-ol-socks']").length, 0);
    assert.equal(stub.posts().length, 0);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a whole-piece fraction and an over-ceiling amount are refused locally; lawful multi-seller selection is sent", async () => {
  window.sessionStorage.clear();
  const submitted: number[] = [];
  const stub = stubCustomerReturns([
    { kind: "ok", id: "rr-socks" },
    { kind: "ok", id: "rr-lamp" },
  ]);
  const tree = await mountModal(sellers(), (count) => submitted.push(count));
  try {
    await flush();
    await toggleLine(tree.container, "ol-socks");
    await setInputValue(q(tree.container, "[data-testid='return-line-quantity-ol-socks']"), "0.5");
    const submitButton = qq(tree.container, "button").find((button) => (button.textContent ?? "").includes("ثبت درخواست مرجوعی"));
    assert.ok(submitButton);
    assert.equal((submitButton as HTMLButtonElement).disabled, true, "a fractional piece amount must disable submit");
    await click(submitButton as HTMLElement);
    await flush();
    assert.equal(stub.posts().length, 0, "a fractional piece amount must never reach the owning route");
    assert.match(tree.container.textContent ?? "", /با واحد این قلم سازگار نیست/);

    // A lawful 2 of 3 pieces, plus a precise fractional gram item from the OTHER seller.
    await setInputValue(q(tree.container, "[data-testid='return-line-quantity-ol-socks']"), "2");
    await toggleLine(tree.container, "ol-lamp");
    await setInputValue(q(tree.container, "[data-testid='return-line-quantity-ol-lamp']"), "۱٫۲۵۰");
    await setSelectValue(q(tree.container, "#return-shared-reason"), "defective");
    await setTextarea(q(tree.container, "#return-description"), "جوراب در دو عدد و چراغ آسیب‌دیده است.");

    const ready = qq(tree.container, "button").find((button) => (button.textContent ?? "").includes("ثبت درخواست مرجوعی"));
    assert.ok(ready);
    assert.equal((ready as HTMLButtonElement).disabled, false);
    await click(ready as HTMLElement);
    await waitFor(() => stub.posts().length === 2);

    const [socksPost, lampPost] = stub.posts();
    assert.equal(socksPost.url, "/api/customer/returns");
    assert.equal(lampPost.url, "/api/customer/returns");
    const socksBody = socksPost.body as { sellerOrderId: string; idempotencyKey: string; items: Array<{ orderLineId: string; quantity: number }> };
    const lampBody = lampPost.body as { sellerOrderId: string; idempotencyKey: string; items: Array<{ orderLineId: string; quantity: number }> };
    assert.equal(socksBody.sellerOrderId, "so-a");
    assert.equal(lampBody.sellerOrderId, "so-b");
    assert.deepEqual(socksBody.items, [{ orderLineId: "ol-socks", quantity: 2 }]);
    // Persian digits and separator became the EXACT stored decimal, not a rounded one.
    assert.deepEqual(lampBody.items, [{ orderLineId: "ol-lamp", quantity: 1.25 }]);
    assert.notEqual(socksBody.idempotencyKey, lampBody.idempotencyKey);
    // The line the customer left untouched is never sent.
    assert.equal(
      stub.posts().some((call) => JSON.stringify(call.body).includes("ol-shirt")),
      false,
    );

    await waitFor(() => toastTexts(tree.container).some((text) => text.includes("درخواست مرجوعی ثبت شد")));
    assert.equal(submitted[0], 2);
    // The successful act is resolved, so nothing is retained for a later resume.
    assert.equal(window.sessionStorage.length, 0);
    assert.match(tree.container.textContent ?? "", /درخواست شما ثبت شد/);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a partially successful act keeps its success, reports the uncertain child and re-sends identical bytes", async () => {
  window.sessionStorage.clear();
  const stub = stubCustomerReturns([
    { kind: "ok", id: "rr-socks" },
    { kind: "transport" },
    { kind: "ok", id: "rr-lamp" },
  ]);
  const tree = await mountModal(sellers());
  try {
    await flush();
    await toggleLine(tree.container, "ol-socks");
    await setInputValue(q(tree.container, "[data-testid='return-line-quantity-ol-socks']"), "2");
    await toggleLine(tree.container, "ol-lamp");
    await setInputValue(q(tree.container, "[data-testid='return-line-quantity-ol-lamp']"), "1.25");
    await setSelectValue(q(tree.container, "#return-shared-reason"), "defective");
    await setTextarea(q(tree.container, "#return-description"), "هر دو قلم را مرجوع می‌کنم.");

    const submitButton = qq(tree.container, "button").find((button) => (button.textContent ?? "").includes("ثبت درخواست مرجوعی"));
    assert.ok(submitButton);
    await click(submitButton as HTMLElement);
    await waitFor(() => stub.posts().length === 2);

    // The first child is recorded; the second never answered, so the act is retained and says so.
    await waitFor(() => qq(tree.container, "[data-testid='retained-return-submission']").length === 1);
    const retainedText = q(tree.container, "[data-testid='retained-return-submission']").textContent ?? "";
    assert.match(retainedText, /۱ ثبت‌شده/);
    assert.match(retainedText, /نتیجه نامشخص/);
    assert.equal(qq(tree.container, "[data-testid='resume-return-submission']").length, 1);
    await waitFor(() => toastTexts(tree.container).some((text) => text.includes("نامشخص")));

    // Nothing is cleared by an uncertain outcome, and the recorded child is not re-sent.
    assert.notEqual(window.sessionStorage.length, 0);
    const firstBodies = stub.posts().map((call) => call.raw);
    await click(q(tree.container, "[data-testid='resume-return-submission']"));
    await waitFor(() => stub.posts().length === 3);

    const [, firstLampSend, replayedLampSend] = stub.posts();
    assert.equal(replayedLampSend.raw, firstLampSend.raw, "the uncertain child must re-send byte-identical bytes");
    assert.equal(
      stub.posts().filter((call) => JSON.stringify(call.body).includes("ol-socks")).length,
      1,
      "a recorded child is never re-sent",
    );
    assert.equal(firstBodies.every((raw) => raw !== null), true);

    // The authoritative answer of the SAME bytes resolves the act and clears the retained evidence.
    await waitFor(() => window.sessionStorage.length === 0);
    await waitFor(() => qq(tree.container, "[data-testid='retained-return-submission']").length === 0);
    await waitFor(() => toastTexts(tree.container).some((text) => text.includes("درخواست مرجوعی ثبت شد")));
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});

test("a checkout with no eligible line offers no selection at all", async () => {
  window.sessionStorage.clear();
  const blocked: ReturnRequestSeller[] = [
    {
      sellerOrderId: "so-c",
      sellerDisplayName: "فروشگاه ج",
      orderNumber: "SO-C-1",
      eligibility: {
        sellerOrderId: "so-c",
        checkoutId: CHECKOUT_ID,
        currency: "IRR",
        eligible: false,
        reasonCode: "window_expired",
        eligibleUntil: null,
        lastDeliveredAt: "2026-01-01T00:00:00Z",
        lines: [
          {
            orderLineId: "ol-old",
            productLabel: "کالای قدیمی",
            unitLabel: "pcs",
            unitDecimalPlaces: 0,
            deliveredQuantity: 1,
            alreadyReturnedQuantity: 0,
            remainingReturnableQuantity: 1,
            isReturnable: false,
            returnWindowDays: 7,
            returnableUntil: "2026-01-08T00:00:00Z",
            blockerCode: "window_expired",
            selectable: false,
          },
        ],
      },
    },
  ];
  const stub = stubCustomerReturns([{ kind: "ok", id: "rr-0" }]);
  const tree = await mountModal(blocked);
  try {
    await flush();
    assert.match(tree.container.textContent ?? "", /بازهٔ مجاز مرجوعی این قلم به پایان رسیده است/);
    assert.equal(qq(tree.container, "[data-testid^='return-line-select-']").length, 1);
    assert.equal((qq(tree.container, "[data-testid^='return-line-select-']")[0] as HTMLInputElement).disabled, true);
    const submitButton = qq(tree.container, "button").find((button) => (button.textContent ?? "").includes("ثبت درخواست مرجوعی"));
    assert.ok(submitButton);
    assert.equal((submitButton as HTMLButtonElement).disabled, true);
    assert.equal(stub.posts().length, 0);
  } finally {
    await tree.unmount();
    stub.restore();
    window.sessionStorage.clear();
  }
});
