/**
 * TB-282-R18 — native 3000ms Story toasts (semantic severity), collapsed user-invoked recovery,
 * definite single-refusal reason, and alignment between offered admin actions and the real
 * approval prerequisites. These mount the real workspace and the real `ToastContainer`.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { ToastContainer, toast } from "react-toastify";
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
import { formatAdminBatchToast, type StoryAdminBatchResult } from "./story-admin-ops.ts";
import { newStoryOccurrenceKey, storeStoryIntent, type StoryIntentEnvelope } from "./story-intent.ts";

ensureDom();

const ID_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa18a1";
const ID_C = "cccccccc-cccc-cccc-cccc-cccccccc18c3";
const SHOP = "22222222-2222-2222-2222-222222222222";
const ACTOR = "actor-admin-r18";

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

function pendingApproveEnvelope(): StoryIntentEnvelope {
  const occurrenceKey = newStoryOccurrenceKey();
  return {
    version: 2,
    action: "approve",
    occurrenceKey,
    method: "POST",
    path: `/v1/admin/stories/${ID_A}/approve`,
    routeTargetId: ID_A,
    expectedVersion: "4",
    bodyText: JSON.stringify({ expectedVersion: "4", occurrenceKey }),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "admin",
    shopId: null,
    actorUserId: ACTOR,
    recordedAt: new Date().toISOString(),
  };
}

const storyToastCalls: Array<{ level: string; options: Record<string, unknown> }> = [];

/** Records the OPTIONS handed to the real toast dispatcher, then lets the real container render. */
function installToastCallRecorder() {
  const levels = ["success", "error", "warning", "info"] as const;
  const originals: Partial<Record<(typeof levels)[number], unknown>> = {};
  for (const level of levels) {
    const fn = (toast as unknown as Record<string, (...args: unknown[]) => unknown>)[level]!;
    originals[level] = fn.bind(toast);
    (toast as unknown as Record<string, unknown>)[level] = (content: unknown, options?: unknown) => {
      storyToastCalls.push({ level, options: (options ?? {}) as Record<string, unknown> });
      return (originals[level] as (...a: unknown[]) => unknown)(content, options);
    };
  }
  return {
    restore() {
      for (const level of levels) {
        if (originals[level]) (toast as unknown as Record<string, unknown>)[level] = originals[level];
      }
    },
  };
}

function gridResponse(rows: AdminStorySnapshot[]) {  return new Response(JSON.stringify({ items: rows, page: 1, pageSize: 50, totalCount: rows.length }), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

function installFetch(rows: AdminStorySnapshot[], onPost?: (url: string) => Response | null) {
  const original = globalThis.fetch;
  const posts: string[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST") {
      posts.push(url);
      const scripted = onPost?.(url);
      if (scripted) return scripted;
    }
    if (method === "POST" && url.includes("/stories/query")) return gridResponse(rows);
    if (url.includes("locales") || url.includes("locale-options")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
  return {
    posts,
    restore() {
      globalThis.fetch = original;
    },
  };
}

async function selectFirstRow(container: HTMLElement) {
  await waitFor(() => qq(container, ".ag-row").length >= 1, { timeoutMs: 5000 });
  await waitFor(
    () => qq(container, '.ag-selection-checkbox input[type="checkbox"], .ag-row input[type="checkbox"]').length >= 1,
    { timeoutMs: 5000 },
  );
  const box = qq(
    container,
    '.ag-selection-checkbox input[type="checkbox"], .ag-row input[type="checkbox"]',
  ).filter((el) => !(el as HTMLInputElement).closest(".ag-header-row"))[0]!;
  await click(box);
  await flush();
  await waitFor(() => {
    const text = container.querySelector('[data-testid="app-grid-selected-count"]')?.textContent ?? "";
    return text.includes("1") || text.includes("۱");
  }, { timeoutMs: 5000 });
}

test("R18: Story toast uses the app container but expires at 3000ms and keeps error severity", async () => {
  const platform = story({
    storyId: ID_C,
    title: "موبایل",
    origin: "Admin",
    reviewStatus: "None",
    sellerPartyId: null,
    actionCodes: ["update", "add_item", "delete", "publish_now"],
  });
  const fetchSpy = installFetch([platform]);
  const toastRecorder = installToastCallRecorder();
  storyToastCalls.length = 0;
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);
  // A stored command must not become a permanent banner: the control starts collapsed.
  assert.equal(storeStoryIntent(pendingApproveEnvelope()), true);

  const tree = await mount(
    createElement(
      "div",
      null,
      createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }),
      // Real app container shape: LONGER default close + hover/focus pauses enabled.
      createElement(ToastContainer, {
        autoClose: 5000,
        pauseOnHover: true,
        pauseOnFocusLoss: true,
      }),
    ),
  );
  await flush();
  await waitFor(() => qq(tree.container, ".ag-row").length >= 1, { timeoutMs: 5000 });

  const recovery = q(tree.container, '[data-testid="story-pending-recovery"]');
  assert.equal(recovery.tagName, "DETAILS", "recovery must be a compact control, not a banner card");
  assert.equal((recovery as HTMLDetailsElement).open, false, "recovery starts collapsed");
  assert.match(
    q(tree.container, '[data-testid="story-pending-recovery-toggle"]').textContent ?? "",
    /۱|1/,
    "collapsed control exposes the pending count",
  );

  await selectFirstRow(tree.container);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]'));
  await flush();
  assert.equal(qq(document.body, '[data-testid="story-admin-op-dialog"]').length, 0);
  assert.equal(fetchSpy.posts.filter((u) => /\/approve\/?$/.test(u)).length, 0);

  await waitFor(() => qq(document.body, ".Toastify__toast").length === 1, { timeoutMs: 3000 });
  assert.equal(qq(document.body, ".Toastify__toast--error").length, 1, "cannot-run is error style, not info");
  assert.equal(qq(document.body, ".Toastify__toast--info").length, 0);
  assert.match(qq(document.body, ".Toastify__toast")[0]!.textContent ?? "", /ویترین اصلی/);

  // The app's 3000ms duration is carried on the STORY toast itself, including the
  // no-pause overrides that make hover/focus unable to extend this message.
  const storyOptions = storyToastCalls.at(-1);
  assert.ok(storyOptions, "the zero-eligible refusal must go through the Story toast helper");
  assert.equal(storyOptions!.options.autoClose, 3000);
  assert.equal(storyOptions!.options.pauseOnHover, false);
  assert.equal(storyOptions!.options.pauseOnFocusLoss, false);

  // Stable single message: a later grid/render pass must not recreate or stack toasts.
  await flush();
  assert.equal(qq(document.body, ".Toastify__toast").length, 1);

  // The stored command is preserved for exact retry (opening the control does not auto-send).
  assert.equal(qq(tree.container, '[data-testid="story-recovery-retry"]').length, 1);

  fetchSpy.restore();
  toastRecorder.restore();
  toast.dismiss();
  window.sessionStorage.clear();
  await tree.unmount();
});

test("R18: one definite refusal names the Persian reason, stays error, and clears its stored command", async () => {
  window.sessionStorage.clear();
  toast.dismiss();
  const row = story({ storyId: ID_A, title: "ناقص", versionToken: 31 });
  const fetchSpy = installFetch([row], (url) => {
    if (!/\/approve\/?$/.test(url)) return null;
    return new Response(
      JSON.stringify({ errorCode: "story.content.notReady", message: "story.content.notReady" }),
      { status: 422, headers: { "Content-Type": "application/json" } },
    );
  });
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);

  const tree = await mount(
    createElement(
      "div",
      null,
      createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }),
      createElement(ToastContainer, { autoClose: 5000, pauseOnHover: true, pauseOnFocusLoss: true }),
    ),
  );
  await flush();
  await selectFirstRow(tree.container);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-confirm"]').length === 1, {
    timeoutMs: 5000,
  });
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();

  await waitFor(() => qq(document.body, ".Toastify__toast--error").length >= 1, { timeoutMs: 5000 });
  const text = qq(document.body, ".Toastify__toast--error").at(-1)!.textContent ?? "";
  assert.match(text, /تأیید نشد:/, "single refusal must carry its reason");
  assert.match(text, /اسلاید قابل‌پخش ندارد/);
  assert.ok(!/انجام نشد$/.test(text.trim()), "a bare «انجام نشد» is not a reason");
  assert.equal(qq(document.body, '[data-testid="story-admin-op-result"]').length, 0);

  // A terminal refusal is a definite non-effect: nothing is left to replay as an uncertain command.
  await waitFor(() => qq(tree.container, '[data-testid="story-pending-recovery"]').length === 0, {
    timeoutMs: 3000,
  });

  fetchSpy.restore();
  await tree.unmount();
});

test("R18 helper severity: success / warning / error never collapses into blue info", () => {
  const base: StoryAdminBatchResult = {
    items: [],
    succeeded: 0,
    failed: 0,
    unknown: 0,
    skipped: 0,
    notStarted: 0,
    stoppedReason: null,
  };
  const okOne: StoryAdminBatchResult = {
    ...base,
    items: [{ storyId: ID_A, title: "a", kind: "approve", outcome: "succeeded", message: null, envelope: null }],
    succeeded: 1,
  };
  assert.equal(formatAdminBatchToast(okOne, "approve").severity, "success");

  const mixedSkipped: StoryAdminBatchResult = { ...base, succeeded: 2, skipped: 1 };
  assert.equal(formatAdminBatchToast(mixedSkipped, "reject").severity, "warning");

  const unknown: StoryAdminBatchResult = { ...base, unknown: 1 };
  assert.equal(formatAdminBatchToast(unknown, "delete").severity, "warning");

  const refused: StoryAdminBatchResult = { ...base, failed: 1, skipped: 1 };
  assert.equal(formatAdminBatchToast(refused, "approve").severity, "error");
});
