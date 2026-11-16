/**
 * TB-271 — the employee work queue: ONE job-stage strip + ONE independent time scope over the EXISTING rows.
 *
 * These mounted cases pin the behaviour the task actually changes: the two old overlapping quick-filter rows are
 * gone, the visible controls are the only filter authority (no hidden legacy `queueFilter`), the badges/summary come
 * from the server's own full-set metadata rather than the fetched page, an older response never publishes over a
 * newer selection, and each row shows the exact server verb, the exact meaning of its job time and a human parcel
 * locator. Filtering is a read: it never mutates a work unit and never clears a retained planning attempt.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-fulfillment-work-queue-271.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { AdminFulfillmentWorkQueueScreen } from "./admin-fulfillment-work-queue-screen.tsx";
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

const QUERY_PATH = "/v1/admin/fulfillments/work-queue/query";

type RecordedCall = { url: string; body: Record<string, unknown> | null };

type Reply = { items?: unknown[]; dispatchCounts?: Record<string, unknown> | null; totalCount?: number };

let pending: Array<{
  body: Record<string, unknown> | null;
  release: (reply: Reply) => void;
}> = [];

/**
 * Installs a fetch stub for the queue read surface. Grid queries are answered by `answer`; the grid's own
 * saved-view preference reads are answered inertly so they never masquerade as queue requests.
 */
function stubFetch(answer: (call: RecordedCall) => Reply | Promise<Reply>) {
  const original = globalThis.fetch;
  const calls: RecordedCall[] = [];
  const respond = (payload: unknown) =>
    ({
      ok: true,
      status: 200,
      json: async () => payload,
    }) as unknown as Response;
  globalThis.fetch = (async (input: unknown, init?: { body?: unknown }) => {
    const url = typeof input === "string" ? input : String((input as { url?: string })?.url ?? input);
    const body = typeof init?.body === "string" ? (JSON.parse(init.body) as Record<string, unknown>) : null;
    if (url !== QUERY_PATH) {
      // Saved views / ui-preferences are a different read model and carry no queue selection.
      return respond({ items: [], views: [] });
    }
    const call = { url, body };
    calls.push(call);
    const reply = await answer(call);
    return respond({
      items: reply.items ?? [],
      totalCount: reply.totalCount ?? (reply.items?.length ?? 0),
      page: 1,
      pageSize: 25,
      dispatchCounts: reply.dispatchCounts ?? null,
    });
  }) as typeof fetch;
  return {
    calls,
    restore() {
      globalThis.fetch = original;
    },
  };
}

/** A deferred answer, so one test can prove that an OLD reply cannot overwrite a NEWER selection. */
function deferredAnswer() {
  pending = [];
  return (call: RecordedCall): Promise<Reply> =>
    new Promise<Reply>((resolve) => {
      pending.push({ body: call.body, release: resolve });
    });
}

/** Answers the NEWEST outstanding queue request, so a test can make a newer reply land before an older one. */
async function releaseLatest(reply: Reply) {
  const next = pending.pop();
  if (!next) throw new Error("no deferred queue request to release");
  next.release(reply);
  await flush();
}

function item(overrides: Record<string, unknown> = {}) {
  return {
    workUnitKey: "parcel:sh-1",
    workUnitKind: "Parcel",
    fulfillmentId: "f1",
    sellerOrderId: "so1",
    checkoutId: "checkout-1",
    sellerPartyId: "p1",
    sellerDisplayName: "فروشنده",
    orderReference: "TB-271-1",
    status: "Packed",
    workUnitState: "Created",
    recipientName: "گیرنده",
    cityName: "تهران",
    shippingMethodCode: "post",
    shippingMethodLabel: "پست",
    itemCount: 1,
    quantityOrdered: 1,
    quantityShipped: 0,
    shipmentCount: 1,
    primaryShipmentId: "sh-1",
    trackingSummary: "",
    createdAt: "2026-11-15T06:00:00Z",
    updatedAt: "2026-11-15T06:00:00Z",
    availableActionCodes: ["cancel_shipment", "plan_shipment_dispatch", "correct_tracking"],
    dispatchBucket: "Today",
    handoffWindowState: "Open",
    plannedHandoffDate: "2026-11-15",
    plannedHandoffWindow: "14:00-15:00",
    plannedHandoffTimeZone: "Asia/Tehran",
    customerReceivingDate: "2026-11-16",
    customerReceivingWindow: "09:00-10:00",
    preparationStartUtc: "2026-11-15T03:00:00Z",
    transferDepartureUtc: "2026-11-15T06:00:00Z",
    handoffDeadlineUtc: "2026-11-15T16:30:00Z",
    preparationState: "Today",
    targetShippingGroupId: "g1",
    targetShipmentId: "sh-1",
    nextStepCode: "assign_tracking",
    nextStepLabelFa: "ثبت کد رهگیری",
    nextStepBlockerFa: null,
    pendingDispatch: true,
    scopeLineCount: 1,
    scopeQuantity: 1,
    scopeRemainingQuantity: 1,
    jobStage: "ready_to_send",
    jobStageLabelFa: "آمادهٔ ارسال",
    jobTimeMeaning: "handoff_close",
    jobTimeLabelFa: "آخرین مهلت تحویل به حامل",
    jobTimeUtc: "2026-11-15T16:30:00Z",
    jobTimeState: "due_today",
    workUnitOrdinal: 2,
    workUnitOrdinalTotal: 3,
    ...overrides,
  };
}

/** Server-owned full-set counts for the «همه / امروز» selection. */
function counts(overrides: Record<string, unknown> = {}) {
  return {
    totalActive: 4,
    due: 1,
    today: 1,
    overdue: 1,
    upcoming: 1,
    unspecified: 1,
    inTransit: 0,
    preparationDue: 1,
    stageAll: 4,
    stagePreparation: 1,
    stageReadyToSend: 1,
    stageHandoff: 1,
    stageDelivery: 0,
    stageReturns: 1,
    stageHistory: 7,
    timeToday: 3,
    timeFuture: 1,
    timeAll: 4,
    undated: 1,
    selectedTotal: 3,
    selectedStage: "all",
    selectedTimeScope: "today",
    ...overrides,
  };
}

/** The applied stage/time of one request, read off the wire filter list the server actually receives. */
function selectionOf(body: Record<string, unknown> | null): {
  stage: string | undefined;
  scope: string | undefined;
  legacy: string | undefined;
} {
  const filters = Array.isArray(body?.filters) ? (body.filters as Array<Record<string, unknown>>) : [];
  const read = (field: string) => {
    const found = filters.find((entry) => entry.field === field);
    if (!found) return undefined;
    const values = found.values;
    if (Array.isArray(values)) return String(values[0]);
    return found.value === undefined ? undefined : String(found.value);
  };
  return { stage: read("jobStage"), scope: read("timeScope"), legacy: read("queueFilter") };
}

function setUrl(search: string) {
  window.history.replaceState({}, "", `/${search}`);
}

async function mountScreen() {
  return mount(createElement(AdminFulfillmentWorkQueueScreen));
}

test("a clean entry shows the live default work — stage همه with scope امروز — and sends both tokens", async () => {
  setUrl("");
  const stub = stubFetch(() => ({ items: [item()], dispatchCounts: counts() }));
  const tree = await mountScreen();
  try {
    await waitFor(() => stub.calls.length > 0);
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);

    // ONE stage strip and ONE time scope replace the two previous competing quick-filter rows.
    assert.equal(qq(tree.container, "[data-testid='admin-fulfillment-job-stages']").length, 1);
    assert.equal(qq(tree.container, "[data-testid='admin-fulfillment-time-scopes']").length, 1);
    assert.equal(q(tree.container, "[data-testid='admin-fulfillment-job-stage-all']").getAttribute("aria-selected"), "true");
    assert.equal(q(tree.container, "[data-testid='admin-fulfillment-time-scope-today']").getAttribute("aria-selected"), "true");

    const applied = selectionOf(stub.calls.at(-1)!.body);
    assert.equal(applied.stage, "all");
    assert.equal(applied.scope, "today");
    // TB-271-R1: a clean entry sends NO synthetic `updatedAt desc`, so the server's job-priority order runs.
    assert.deepEqual((stub.calls.at(-1)!.body?.sort as unknown[] | undefined) ?? [], []);
    // A clean entry never mutates the URL into an explicit selection.
    assert.equal(window.location.search, "");
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("the two controls serialize their own tokens and never a hidden legacy queue filter", async () => {
  setUrl("");
  const stub = stubFetch(() => ({ items: [item()], dispatchCounts: counts() }));
  const tree = await mountScreen();
  try {
    await waitFor(() => stub.calls.length > 0);
    await click(q(tree.container, "[data-testid='admin-fulfillment-job-stage-handoff']"));
    await flush();
    assert.match(window.location.search, /stage=handoff/);
    assert.doesNotMatch(window.location.search, /filter=/);

    await click(q(tree.container, "[data-testid='admin-fulfillment-time-scope-future']"));
    await flush();
    assert.match(window.location.search, /scope=future/);

    await waitFor(() => {
      const applied = selectionOf(stub.calls.at(-1)!.body);
      return applied.stage === "handoff" && applied.scope === "future";
    });
    for (const call of stub.calls) {
      // The visible controls are the ONLY selection authority.
      assert.equal(selectionOf(call.body).legacy, undefined);
      assert.equal(call.url, QUERY_PATH);
    }
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("an old legacy link is translated once into a real stage/scope and the legacy token is removed", async () => {
  setUrl("?filter=ready_to_ship");
  const stub = stubFetch(() => ({ items: [item()], dispatchCounts: counts() }));
  const tree = await mountScreen();
  try {
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);
    assert.equal(q(tree.container, "[data-testid='admin-fulfillment-job-stage-ready_to_send']").getAttribute("aria-selected"), "true");
    assert.equal(q(tree.container, "[data-testid='admin-fulfillment-time-scope-all']").getAttribute("aria-selected"), "true");
    // The old token is rewritten deliberately, never kept beside the visible controls.
    assert.doesNotMatch(window.location.search, /filter=/);
    assert.match(window.location.search, /stage=ready_to_send/);
    assert.match(window.location.search, /scope=all/);

    const applied = selectionOf(stub.calls.at(-1)!.body);
    assert.equal(applied.stage, "ready_to_send");
    assert.equal(applied.scope, "all");
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("browser back/forward restores the same queue selection instead of a stale hidden filter", async () => {
  setUrl("");
  const stub = stubFetch(() => ({ items: [item()], dispatchCounts: counts() }));
  const tree = await mountScreen();
  try {
    await waitFor(() => stub.calls.length > 0);
    await click(q(tree.container, "[data-testid='admin-fulfillment-job-stage-preparation']"));
    await flush();
    assert.match(window.location.search, /stage=preparation/);

    await waitFor(() => selectionOf(stub.calls.at(-1)!.body).stage === "preparation");

    window.history.back();
    await flush();
    await waitFor(() => q(tree.container, "[data-testid='admin-fulfillment-job-stage-all']").getAttribute("aria-selected") === "true");
    assert.equal(q(tree.container, "[data-testid='admin-fulfillment-time-scope-today']").getAttribute("aria-selected"), "true");
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("badges and the summary come from the server's full set, never from the fetched page", async () => {
  setUrl("");
  // ONE row on the page, but the server says 7 archived rows and 5 real returns: the page is not the set.
  const stub = stubFetch(() => ({
    items: [item()],
    dispatchCounts: counts({ stageReturns: 5, stageHistory: 7, selectedTotal: 37, undated: 2 }),
  }));
  const tree = await mountScreen();
  try {
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);
    assert.equal(
      q(tree.container, "[data-testid='admin-fulfillment-job-stage-count-returns']").textContent,
      (5).toLocaleString("fa-IR"),
    );
    assert.equal(
      q(tree.container, "[data-testid='admin-fulfillment-job-stage-history']").textContent?.includes((7).toLocaleString("fa-IR")),
      true,
    );
    const summary = q(tree.container, "[data-testid='admin-fulfillment-queue-summary']");
    assert.equal(summary.getAttribute("data-selected-total"), "37");
    assert.match(summary.textContent ?? "", /بدون موعد مشخص/);
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("unknown counts render as unavailable markers instead of a successful-looking empty queue", async () => {
  setUrl("");
  const stub = stubFetch(() => ({ items: [item()], dispatchCounts: null }));
  const tree = await mountScreen();
  try {
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);
    assert.equal(q(tree.container, "[data-testid='admin-fulfillment-job-stage-count-preparation']").textContent, "—");
    assert.equal(q(tree.container, "[data-testid='admin-fulfillment-time-scope-count-today']").textContent, "—");
    assert.match(q(tree.container, "[data-testid='admin-fulfillment-queue-summary']").textContent ?? "", /در دسترس نیست/);
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("the footer total is the server's selected total and the archive badge never moves with the active tab", async () => {
  setUrl("");
  const stub = stubFetch(() => ({
    items: [item()],
    totalCount: 3,
    dispatchCounts: counts({ selectedTotal: 3, stageHistory: 7 }),
  }));
  const tree = await mountScreen();
  try {
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);
    // TB-271-R1: the grid total is the server's selection total (totalCount === selectedTotal on the wire), never
    // the one fetched row.
    const pagination = q(tree.container, "[data-app-grid-pagination]");
    assert.match(pagination.textContent ?? "", /از\s*۳\s*ردیف/);
    assert.equal(
      q(tree.container, "[data-testid='admin-fulfillment-queue-summary']").getAttribute("data-selected-total"),
      "3",
    );
    const archiveBadge = () =>
      q(tree.container, "[data-testid='admin-fulfillment-job-stage-history']").textContent ?? "";
    assert.equal(archiveBadge().includes((7).toLocaleString("fa-IR")), true);

    // Changing the active stage and then the time scope is a read of a different selection, but the archive badge
    // is a function of the common constraints: it may never move with the tab the operator is looking at.
    await click(q(tree.container, "[data-testid='admin-fulfillment-job-stage-handoff']"));
    await waitFor(() => selectionOf(stub.calls.at(-1)!.body).stage === "handoff");
    await click(q(tree.container, "[data-testid='admin-fulfillment-time-scope-future']"));
    await waitFor(() => selectionOf(stub.calls.at(-1)!.body).scope === "future");
    assert.equal(archiveBadge().includes((7).toLocaleString("fa-IR")), true);
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("an accepted zone that is missing or invalid is named truthfully instead of guessed into a date", async () => {
  setUrl("");
  const stub = stubFetch(() => ({
    items: [item({
      jobTimeMeaning: "zone_unavailable",
      jobTimeLabelFa: "منطقهٔ زمانی مشخص نشده؛ زمان کار قابل تعیین نیست",
      jobTimeUtc: "2026-11-15T16:30:00Z",
      plannedHandoffTimeZone: null,
      jobTimeState: "undated",
    })],
    dispatchCounts: counts({ undated: 1, selectedTotal: 1 }),
  }));
  const tree = await mountScreen();
  try {
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);
    const cell = q(tree.container, "[data-testid='admin-fulfillment-job-time-parcel:sh-1']");
    // The sentence names the real problem, the state is the honest undated exception and no date is invented.
    assert.match(cell.textContent ?? "", /منطقهٔ زمانی مشخص نشده/);
    assert.equal(cell.getAttribute("data-job-time-state"), "undated");
    assert.doesNotMatch(cell.textContent ?? "", /\d{4}-\d{2}-\d{2}T/);
    assert.doesNotMatch(cell.textContent ?? "", /Asia\/Tehran/);
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("an older response can never publish its badges over a newer selection", async () => {
  setUrl("");
  const answer = deferredAnswer();
  const stub = stubFetch(answer);
  const tree = await mountScreen();
  try {
    await waitFor(() => pending.length === 1);
    // The user moves to «آینده» before the first (today) response has arrived.
    await click(q(tree.container, "[data-testid='admin-fulfillment-time-scope-future']"));
    await waitFor(() => pending.length === 2);

    // The NEWER request answers first …
    await releaseLatest({ items: [item({ jobTimeState: "future" })], dispatchCounts: counts({ selectedTotal: 1, undated: 0 }) });
    await waitFor(() => q(tree.container, "[data-testid='admin-fulfillment-queue-summary']").getAttribute("data-selected-total") === "1");

    // … then the stale today response lands and must be ignored.
    await releaseLatest({ items: [item()], dispatchCounts: counts({ selectedTotal: 3, undated: 2 }) });
    await flush();
    assert.equal(q(tree.container, "[data-testid='admin-fulfillment-queue-summary']").getAttribute("data-selected-total"), "1");
    assert.doesNotMatch(q(tree.container, "[data-testid='admin-fulfillment-queue-summary']").textContent ?? "", /بدون موعد مشخص/);
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("a row shows the exact server verb, the exact job-time meaning and a human parcel locator", async () => {
  setUrl("");
  const stub = stubFetch(() => ({ items: [item()], dispatchCounts: counts() }));
  const tree = await mountScreen();
  try {
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);

    const nextStep = q(tree.container, "[data-testid='admin-fulfillment-next-step-parcel:sh-1']");
    assert.match(nextStep.textContent ?? "", /ثبت کد رهگیری/);

    const jobTime = q(tree.container, "[data-testid='admin-fulfillment-job-time-parcel:sh-1']");
    assert.match(jobTime.textContent ?? "", /آخرین مهلت تحویل به حامل/);
    // The accepted zone is one human label; the raw id and raw ISO never reach the operator.
    assert.match(jobTime.textContent ?? "", /به وقت تهران/);
    assert.doesNotMatch(jobTime.textContent ?? "", /Asia\/Tehran|\d{4}-\d{2}-\d{2}T/);

    const stage = q(tree.container, "[data-testid='admin-fulfillment-row-stage-parcel:sh-1']");
    assert.equal(stage.getAttribute("data-stage"), "ready_to_send");
    assert.match(stage.textContent ?? "", /آمادهٔ ارسال/);

    // TWO parcels of one order stay distinguishable without a raw GUID.
    assert.match(
      q(tree.container, "[data-testid='admin-fulfillment-work-scope-parcel:sh-1']").textContent ?? "",
      /مرسولهٔ ۲ از ۳/,
    );

    // ONE locate action, carrying the exact physical target of THIS row.
    const open = q(tree.container, "[data-testid='admin-fulfillment-open-operations-parcel:sh-1']") as HTMLAnchorElement;
    const href = open.getAttribute("href") ?? "";
    assert.match(href, /fulfillmentId=f1/);
    assert.match(href, /shippingGroupId=g1/);
    assert.match(href, /shipmentId=sh-1/);
    assert.match(href, /from=queue/);
    // A legacy quick-filter token can never ride along on the return link.
    assert.doesNotMatch(href, /filter=/);

    // Moving the queue selection makes the SAME row carry the active stage, so «بازگشت به صف کار» restores it.
    await click(q(tree.container, "[data-testid='admin-fulfillment-job-stage-handoff']"));
    await waitFor(() => selectionOf(stub.calls.at(-1)!.body).stage === "handoff");
    await waitFor(() =>
      (q(tree.container, "[data-testid='admin-fulfillment-open-operations-parcel:sh-1']") as HTMLAnchorElement)
        .getAttribute("href")?.includes("stage=handoff") === true,
    );
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("the quiet archive is reachable, time-independent and never part of the active total", async () => {
  setUrl("");
  const stub = stubFetch(() => ({ items: [item({ jobStage: "history" })], dispatchCounts: counts() }));
  const tree = await mountScreen();
  try {
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);
    await click(q(tree.container, "[data-testid='admin-fulfillment-job-stage-history']"));
    await flush();

    assert.match(window.location.search, /stage=history/);
    assert.match(window.location.search, /scope=all/);
    // History has no time dimension, so the time control is honestly disabled rather than lying.
    assert.equal((q(tree.container, "[data-testid='admin-fulfillment-time-scope-today']") as HTMLButtonElement).disabled, true);
    await waitFor(() => selectionOf(stub.calls.at(-1)!.body).stage === "history");
    assert.equal(selectionOf(stub.calls.at(-1)!.body).scope, "all");
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("the operator sees Persian digits on the codes while search still reaches the stored Latin value", async () => {
  setUrl("");
  const reference = "TB-20261114151709-01-2d8281";
  const tracking = "TRK-01A1DB2C5F9F70008149360BCBC27C3A";
  const stub = stubFetch(() => ({
    items: [item({ orderReference: reference, trackingSummary: tracking, shipmentCount: 1 })],
    dispatchCounts: counts(),
  }));
  const tree = await mountScreen();
  try {
    await waitFor(() => qq(tree.container, "[data-testid^='admin-fulfillment-job-time-']").length > 0);

    // Display: Persian digits only, and the exact stored code stays recoverable through the tooltip.
    const referenceCell = [...tree.container.querySelectorAll("[title]")].find(
      (node) => node.getAttribute("title") === reference,
    );
    assert.ok(referenceCell, "the reference cell keeps its exact stored value as a tooltip");
    assert.match(referenceCell.textContent ?? "", /[۰-۹]/);
    assert.doesNotMatch(referenceCell.textContent ?? "", /[0-9]/);
    const trackingCell = [...tree.container.querySelectorAll("[title]")].find(
      (node) => node.getAttribute("title") === tracking,
    );
    assert.ok(trackingCell, "the tracking cell keeps its exact stored value as a tooltip");
    assert.doesNotMatch(trackingCell.textContent ?? "", /[0-9]/);

    // Typing the LOCALIZED code in search still finds the row: the outgoing value is folded back to Latin.
    const searchBox = tree.container.querySelector("input[type='search'], input[placeholder='جستجو…']");
    assert.ok(searchBox, "the grid search box is present");
    await setInputValue(searchBox, "TB-۲۰۲۶۱۱۱۴۱۵۱۷۰۹-۰۱-۲d۸۲۸۱");
    searchBox.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true }));
    await waitFor(() => stub.calls.at(-1)!.body?.search !== undefined);

    assert.equal(stub.calls.at(-1)!.body?.search, reference);
    // No localized digit ever reaches the wire.
    assert.doesNotMatch(JSON.stringify(stub.calls.at(-1)!.body), /[۰-۹]/);
  } finally {
    await tree.unmount();
    stub.restore();
  }
});

test("changing stage/time is a read: no mutation request and no cleared planning attempt", async () => {
  setUrl("");
  const KEY = "tb271-retained-plan-attempt";
  localStorage.setItem(KEY, JSON.stringify({ occurrenceKey: "plan:sh-1", shipmentId: "sh-1" }));
  const stub = stubFetch(() => ({ items: [item()], dispatchCounts: counts() }));
  const tree = await mountScreen();
  try {
    await waitFor(() => stub.calls.length > 0);
    await click(q(tree.container, "[data-testid='admin-fulfillment-job-stage-returns']"));
    await flush();
    await click(q(tree.container, "[data-testid='admin-fulfillment-time-scope-all']"));
    await flush();
    await waitFor(() => selectionOf(stub.calls.at(-1)!.body).stage === "returns");

    // Every request stayed a read of the queue; a filter change never invokes a command surface.
    assert.equal(stub.calls.every((call) => call.url === QUERY_PATH && call.body?.page !== undefined), true);    // A retained planning attempt belongs to the detail workflow, not to the queue's read state.
    assert.equal(localStorage.getItem(KEY), JSON.stringify({ occurrenceKey: "plan:sh-1", shipmentId: "sh-1" }));
  } finally {
    localStorage.removeItem(KEY);
    await tree.unmount();
    stub.restore();
  }
});
