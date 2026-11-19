/**
 * TB-282-R16 — Reject dialog typing must not refetch grid or clear selection.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { act as reactDomTestAct } from "react-dom/test-utils";
import {
  click,
  ensureDom,
  flush,
  mount,
  q,
  qq,
  waitFor,
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";
import type { AdminStorySnapshot } from "../story-api.ts";

const act = reactDomTestAct as (fn: () => void | Promise<void>) => Promise<void>;

ensureDom();

const ID_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa16a1";
const ID_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb16b2";
const ID_C = "cccccccc-cccc-cccc-cccc-cccccccc16c3";
const SHOP = "22222222-2222-2222-2222-222222222222";

function story(over: Partial<AdminStorySnapshot> & { storyId: string; title: string }): AdminStorySnapshot {
  const now = new Date().toISOString();
  return {
    id: over.storyId,
    tenantId: "store-alpha",
    origin: "Seller",
    reviewStatus: "Submitted",
    sellerPartyId: SHOP,
    status: "Draft",
    versionToken: 4,
    locale: "fa-IR",
    market: null,
    coverMediaAssetId: null,
    coverMediaUrl: null,
    displayOrder: 0,
    ctaType: "none",
    ctaTarget: null,
    startAt: null,
    endAt: null,
    createdAt: now,
    updatedAt: now,
    createdByActorUserId: null,
    reviewedByActorUserId: null,
    items: [],
    shopDisplayName: "فروشگاه",
    actionCodes: ["approve", "reject", "delete", "update", "add_item"],
    effectivelyPublic: false,
    contentReady: true,
    rejectionReason: null,
    submittedAt: now,
    reviewedAt: null,
    submittedByActorUserId: null,
    ...over,
  };
}

function selectedCountText(container: HTMLElement): string {
  const el = container.querySelector('[data-testid="app-grid-selected-count"]');
  return el?.textContent?.trim() ?? "";
}

async function selectAgRows(container: HTMLElement, count: number) {
  await waitFor(() => qq(container, ".ag-row").length >= count, { timeoutMs: 5000 });
  await waitFor(() => {
    const boxes = qq(container, '.ag-selection-checkbox input[type="checkbox"], .ag-row input[type="checkbox"]');
    return boxes.length >= count;
  }, { timeoutMs: 5000 });
  const boxes = qq(
    container,
    '.ag-selection-checkbox input[type="checkbox"], .ag-row input[type="checkbox"]',
  ).filter((el) => !(el as HTMLInputElement).closest(".ag-header-row"));
  assert.ok(boxes.length >= count);
  for (let i = 0; i < count; i += 1) {
    await click(boxes[i]!);
    await flush();
  }
  await waitFor(() => {
    const text = selectedCountText(container);
    return text.includes(String(count)) || text.includes(["۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹"][count]!);
  }, { timeoutMs: 5000 });
}

/** Character-by-character through the native value setter so React controlled input updates. */
async function typeReasonCharByChar(input: HTMLInputElement, text: string) {
  const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value")?.set;
  assert.ok(setter, "HTMLInputElement value setter missing");
  input.focus();
  let value = input.value;
  for (const ch of text) {
    value += ch;
    await act(async () => {
      setter.call(input, value);
      input.dispatchEvent(
        new InputEvent("input", { bubbles: true, cancelable: true, data: ch, inputType: "insertText" }),
      );
      input.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await flush();
  }
}

async function backspaceOnce(input: HTMLInputElement) {
  const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value")?.set;
  assert.ok(setter);
  const next = input.value.slice(0, -1);
  await act(async () => {
    setter.call(input, next);
    input.dispatchEvent(
      new InputEvent("input", { bubbles: true, cancelable: true, inputType: "deleteContentBackward" }),
    );
    input.dispatchEvent(new Event("change", { bubbles: true }));
  });
  await flush();
}

test("R16: reject typing does not refetch or clear selection; cancel then confirm eligible only", async () => {
  const a = story({ storyId: ID_A, title: "eligible-a", versionToken: 21 });
  const b = story({ storyId: ID_B, title: "eligible-b", versionToken: 22 });
  const c = story({
    storyId: ID_C,
    title: "موبایل",
    origin: "Admin",
    reviewStatus: "None",
    sellerPartyId: null,
    versionToken: 23,
    actionCodes: ["update", "add_item", "delete", "publish_now"],
  });
  const rows = [a, b, c];
  let queryReads = 0;
  const rejectBodies: string[] = [];
  const rejectUrls: string[] = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST" && url.includes("/stories/query")) {
      queryReads += 1;
      return new Response(JSON.stringify({ items: rows, page: 1, pageSize: 50, totalCount: rows.length }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (method === "POST" && /\/reject\/?$/.test(url)) {
      rejectUrls.push(url);
      rejectBodies.push(String(init?.body ?? ""));
      const id = url.includes(ID_A) ? ID_A : url.includes(ID_B) ? ID_B : "unknown";
      const row = rows.find((r) => r.storyId === id) ?? a;
      return new Response(JSON.stringify({ ...row, reviewStatus: "Rejected", actionCodes: ["delete"] }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/stories") && method === "GET") {
      return new Response(JSON.stringify(rows[0]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("locales") || url.includes("locale-options")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/admin/sellers")) {
      return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-admin-r16");
  const tree = await mount(
    createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }),
  );
  await flush();
  await waitFor(() => qq(tree.container, ".ag-row").length >= 3, { timeoutMs: 5000 });
  await waitFor(() => queryReads >= 1, { timeoutMs: 5000 });
  const baselineReads = queryReads;

  await selectAgRows(tree.container, 3);
  const selectedBefore = selectedCountText(tree.container);
  assert.match(selectedBefore, /3|۳/);

  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-reject"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-reject-reason-input"]').length === 1, {
    timeoutMs: 5000,
  });
  assert.equal(queryReads, baselineReads, "opening reject must not refetch");

  const input = q(document.body, '[data-testid="story-reject-reason-input"]') as HTMLInputElement;
  await typeReasonCharByChar(input, "ناقص");
  await backspaceOnce(input);
  assert.equal(input.value, "ناق");
  await typeReasonCharByChar(input, "ص");

  assert.equal(queryReads, baselineReads, "typing/backspace must not refetch");
  assert.equal(selectedCountText(tree.container), selectedBefore, "selection must stay");
  assert.equal(document.activeElement, input, "focus stays on reason");
  assert.equal(input.value, "ناقص");
  assert.match(q(document.body, '[data-testid="story-admin-op-processable-count"]').textContent ?? "", /2|۲/);
  assert.match(q(document.body, '[data-testid="story-admin-op-skipped"]').textContent ?? "", /1|۱/);
  assert.equal(qq(document.body, '[data-testid="story-admin-op-skipped-group"]').length, 1);

  await click(q(document.body, '[data-testid="story-admin-op-cancel"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-reject-reason-input"]').length === 0, {
    timeoutMs: 5000,
  });
  assert.equal(rejectBodies.length, 0);
  assert.equal(selectedCountText(tree.container), selectedBefore);
  assert.equal(queryReads, baselineReads);

  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-reject"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-reject-reason-input"]').length === 1, {
    timeoutMs: 5000,
  });
  const input2 = q(document.body, '[data-testid="story-reject-reason-input"]') as HTMLInputElement;
  await typeReasonCharByChar(input2, "ناقص بود");
  assert.equal(queryReads, baselineReads);
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => rejectBodies.length === 2, { timeoutMs: 5000 });
  assert.ok(!rejectUrls.some((u) => u.includes(ID_C)));
  const ids = rejectBodies.map((raw) => JSON.parse(raw) as { expectedVersion: number; reason: string; occurrenceKey: string });
  assert.deepEqual(new Set(ids.map((x) => x.expectedVersion)), new Set([21, 22]));
  assert.ok(ids.every((x) => x.reason === "ناقص بود"));
  assert.notEqual(ids[0]!.occurrenceKey, ids[1]!.occurrenceKey);
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-result"]').length === 0, {
    timeoutMs: 5000,
  });
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-dialog"]').length === 0, {
    timeoutMs: 5000,
  });

  // Deliberate review-filter change still fetches
  const beforeRefresh = queryReads;
  const filter = q(tree.container, '[data-testid="admin-story-review-filter"]') as HTMLSelectElement;
  await act(async () => {
    filter.value = "Submitted";
    filter.dispatchEvent(new Event("change", { bubbles: true }));
  });
  await flush();
  await waitFor(() => queryReads > beforeRefresh, { timeoutMs: 5000 });

  globalThis.fetch = original;
  await tree.unmount();
});
