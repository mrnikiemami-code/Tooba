/**
 * TB-282-R17 — native toast + compact bulk confirm; no zero-eligible / result dialogs.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { toast } from "react-toastify";
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
import {
  classifyAdminOp,
  explainZeroEligibleToast,
  formatAdminBatchToast,
  groupSkippedReasons,
  type StoryAdminBatchResult,
} from "./story-admin-ops.ts";

ensureDom();

const act = reactDomTestAct as (fn: () => void | Promise<void>) => Promise<void>;

const ID_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa17a1";
const ID_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb17b2";
const ID_C = "cccccccc-cccc-cccc-cccc-cccccccc17c3";
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
  return container.querySelector('[data-testid="app-grid-selected-count"]')?.textContent?.trim() ?? "";
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
  for (let i = 0; i < count; i += 1) {
    await click(boxes[i]!);
    await flush();
  }
  await waitFor(() => {
    const text = selectedCountText(container);
    return text.includes(String(count)) || text.includes(["۰", "۱", "۲", "۳"][count]!);
  }, { timeoutMs: 5000 });
}

function installToastSpy() {
  const messages: Array<{ level: string; text: string }> = [];
  const levels = ["info", "success", "error", "warning", "warn"] as const;
  const originals: Partial<Record<(typeof levels)[number], unknown>> = {};
  for (const level of levels) {
    const fn = (toast as unknown as Record<string, (...args: unknown[]) => unknown>)[level];
    if (typeof fn !== "function") continue;
    originals[level] = fn.bind(toast);
    (toast as unknown as Record<string, unknown>)[level] = (content: unknown, ...rest: unknown[]) => {
      messages.push({ level, text: String(content) });
      return (originals[level] as (...a: unknown[]) => unknown)(content, ...rest);
    };
  }
  return {
    messages,
    restore() {
      for (const level of levels) {
        if (originals[level]) (toast as unknown as Record<string, unknown>)[level] = originals[level];
      }
    },
  };
}

test("R17: all-ineligible approve → one toast, no dialog, no POST, selection kept", async () => {
  const platform = story({
    storyId: ID_C,
    title: "موبایل",
    origin: "Admin",
    reviewStatus: "None",
    sellerPartyId: null,
    actionCodes: ["update", "add_item", "delete", "publish_now"],
  });
  const rows = [platform];
  let approvePosts = 0;
  const spy = installToastSpy();
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST" && /\/approve\/?$/.test(url)) approvePosts += 1;
    if (method === "POST" && url.includes("/stories/query")) {
      return new Response(JSON.stringify({ items: rows, page: 1, pageSize: 50, totalCount: 1 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("locales") || url.includes("locale-options")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-admin-r17");
  const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }));
  await flush();
  await waitFor(() => qq(tree.container, ".ag-row").length >= 1, { timeoutMs: 5000 });
  await selectAgRows(tree.container, 1);
  const selectedBefore = selectedCountText(tree.container);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]'));
  await flush();
  assert.equal(qq(document.body, '[data-testid="story-admin-op-dialog"]').length, 0);
  assert.equal(approvePosts, 0);
  assert.equal(selectedCountText(tree.container), selectedBefore);
  await waitFor(() => spy.messages.some((m) => m.text.includes("ویترین اصلی")), { timeoutMs: 3000 });
  assert.equal(spy.messages.length, 1);
  assert.ok(!spy.messages[0]!.text.includes("مورد مجاز"));
  assert.ok(!spy.messages[0]!.text.includes("ردشده از این اقدام"));

  spy.restore();
  globalThis.fetch = original;
  await tree.unmount();
});

test("R17: mixed reject → compact counts, typing stable, one toast, no result dialog", async () => {
  const a = story({ storyId: ID_A, title: "eligible-a", versionToken: 31 });
  const b = story({ storyId: ID_B, title: "eligible-b", versionToken: 32 });
  const c = story({
    storyId: ID_C,
    title: "موبایل",
    origin: "Admin",
    reviewStatus: "None",
    sellerPartyId: null,
    versionToken: 33,
    actionCodes: ["update", "add_item", "delete", "publish_now"],
  });
  const rows = [a, b, c];
  let queryReads = 0;
  const rejectBodies: string[] = [];
  const spy = installToastSpy();
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST" && url.includes("/stories/query")) {
      queryReads += 1;
      return new Response(JSON.stringify({ items: rows, page: 1, pageSize: 50, totalCount: 3 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (method === "POST" && /\/reject\/?$/.test(url)) {
      rejectBodies.push(String(init?.body ?? ""));
      return new Response(JSON.stringify({ ...a, reviewStatus: "Rejected" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("locales") || url.includes("locale-options")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-admin-r17b");
  const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }));
  await flush();
  await waitFor(() => queryReads >= 1, { timeoutMs: 5000 });
  const baseline = queryReads;
  await selectAgRows(tree.container, 3);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-reject"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-reject-reason-input"]').length === 1, {
    timeoutMs: 5000,
  });
  assert.match(q(document.body, '[data-testid="story-admin-op-processable-count"]').textContent ?? "", /2|۲/);
  const dialogText = q(document.body, '[data-testid="story-admin-op-dialog"]').textContent ?? "";
  assert.ok(!dialogText.includes("eligible-a"));
  assert.ok(!dialogText.includes("موبایل"));
  assert.equal(qq(document.body, '[data-testid="story-admin-op-skipped-group"]').length, 1);

  const input = q(document.body, '[data-testid="story-reject-reason-input"]') as HTMLInputElement;
  const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value")?.set;
  assert.ok(setter, "native value setter must exist");
  let value = "";
  for (const ch of "ناقص") {
    value += ch;
    await act(async () => {
      setter.call(input, value);
      input.dispatchEvent(new InputEvent("input", { bubbles: true, data: ch, inputType: "insertText" }));
      input.dispatchEvent(new Event("change", { bubbles: true }));
    });
    await flush();
  }
  assert.equal(queryReads, baseline);

  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => rejectBodies.length === 2, { timeoutMs: 5000 });
  assert.equal(qq(document.body, '[data-testid="story-admin-op-result"]').length, 0);
  await waitFor(() => spy.messages.some((m) => m.text.includes("رد شد")), { timeoutMs: 3000 });
  assert.equal(spy.messages.filter((m) => m.text.includes("رد شد") || m.text.includes("انجام نشد")).length, 1);
  assert.ok(!spy.messages.some((m) => m.text.includes("ردشده")));

  spy.restore();
  globalThis.fetch = original;
  await tree.unmount();
});

test("R17 helpers: 1000-row plan stays bounded; toast never lists titles", () => {
  const rows = Array.from({ length: 1000 }, (_, i) =>
    story({
      storyId: `00000000-0000-4000-8000-${String(i).padStart(12, "0")}`,
      title: `title-${i}-very-long-name`,
      origin: i % 3 === 0 ? "Admin" : "Seller",
      reviewStatus: i % 3 === 0 ? "None" : "Submitted",
      sellerPartyId: i % 3 === 0 ? null : SHOP,
      actionCodes:
        i % 3 === 0
          ? ["update", "delete", "publish_now"]
          : ["approve", "reject", "delete", "update", "add_item"],
      versionToken: i + 1,
    }),
  );
  const plan = classifyAdminOp("approve", rows);
  assert.ok(plan.eligible.length > 0);
  assert.ok(plan.skipped.length > 0);
  const groups = groupSkippedReasons(plan.skipped);
  assert.ok(groups.length >= 1);
  assert.ok(groups.length < 50, "skipped reasons must stay grouped");
  assert.ok(!groups.some((g) => g.reason.includes("title-")));
  // Confirmation summary for 1000 must not grow with titles
  const summaryChars = groups.reduce((n, g) => n + g.reason.length + String(g.count).length, 0);
  assert.ok(summaryChars < 2000, "grouped skip summary must stay compact at 1000 rows");
  const zeroMsg = explainZeroEligibleToast(classifyAdminOp("approve", rows.filter((r) => r.origin === "Admin")));
  assert.match(zeroMsg, /ویترین اصلی/);
  assert.ok(zeroMsg.length < 200);

  const fakeResult: StoryAdminBatchResult = {
    items: rows.slice(0, 20).map((r) => ({
      storyId: r.storyId,
      title: r.title,
      kind: "delete" as const,
      outcome: "succeeded" as const,
      message: null,
      envelope: null,
    })),
    succeeded: 8,
    failed: 0,
    unknown: 1,
    skipped: 2,
    notStarted: 0,
    stoppedReason: null,
  };
  const toastMsg = formatAdminBatchToast(fakeResult, "delete");
  assert.match(toastMsg.message, /8 استوری حذف شد|۸ استوری حذف شد/);
  assert.match(toastMsg.message, /نامشخص/);
  assert.ok(!toastMsg.message.includes("title-"));
  assert.ok(!toastMsg.message.includes("ردشده"));
  assert.equal(toastMsg.severity, "warning");
  assert.ok(toastMsg.message.length < 300);
});
