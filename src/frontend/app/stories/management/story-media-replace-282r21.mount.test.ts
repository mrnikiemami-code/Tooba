import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { click, ensureDom, flush, mount, q, qq, setInputValue, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";
import type { AdminStorySnapshot } from "../story-api.ts";


ensureDom();

const STORY = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa21b1";
const ITEM = "dddddddd-dddd-dddd-dddd-dddddddd21b1";
const ASSET = "eeeeeeee-eeee-eeee-eeee-eeeeeeee21b1";
const ACTOR = "actor-media-r21";

function story(over: Partial<AdminStorySnapshot> = {}): AdminStorySnapshot {
  const now = new Date().toISOString();
  return {
    id: STORY, storyId: STORY, tenantId: "store-alpha", origin: "Admin", reviewStatus: "Approved",
    sellerPartyId: null, rejectionReason: null, submittedAt: now, reviewedAt: now, submittedByActorUserId: null,
    reviewedByActorUserId: null, locale: "fa", market: null, title: "استوری رسانه", coverMediaAssetId: null,
    coverMediaUrl: "https://cdn.example.test/cover.jpg", displayOrder: 0, startAt: null, endAt: null,
    status: "Draft", ctaType: "none", ctaTarget: null, versionToken: 4, createdAt: now, updatedAt: now,
    createdByActorUserId: ACTOR, shopDisplayName: null, contentReady: true, effectivelyPublic: false,
    actionCodes: ["update", "add_item"], items: [{
      storyItemId: ITEM, displayOrder: 0, mediaType: "image", mediaAssetId: "44444444-4444-4444-4444-444444444444",
      mediaUrl: "https://cdn.example.test/old.jpg", caption: "قدیمی", durationMs: 5000, ctaType: "none",
      ctaTarget: null, createdAt: now, updatedAt: now,
    }], ...over,
  };
}

async function assignFile(input: HTMLInputElement, file: File): Promise<void> {
  const list = { 0: file, length: 1, item: (index: number) => (index === 0 ? file : null), *[Symbol.iterator]() { yield file; } } as unknown as FileList;
  Object.defineProperty(input, "files", { configurable: true, value: list });
  input.dispatchEvent(new Event("change", { bubbles: true }));
  await flush();
}

test("form replacement uploads once, keeps the advanced journal after a lost UPDATE, and resumes only the frozen update", async () => {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);
  window.sessionStorage.clear();
  let uploads = 0;
  let updates = 0;
  let current = story();
  const bodies: string[] = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = typeof init?.body === "string" ? init.body : null;
    if (url.includes("locale-options")) return new Response(JSON.stringify([{ code: "fa", name: "فارسی", isDefault: true }]), { status: 200, headers: { "Content-Type": "application/json" } });
    if (url.includes("/stories/query")) return new Response(JSON.stringify({ items: [current], page: 1, pageSize: 50, totalCount: 1 }), { status: 200, headers: { "Content-Type": "application/json" } });
    if (method === "POST" && url.includes("/media/")) {
      uploads += 1;
      return new Response(JSON.stringify({ mediaAssetId: ASSET, contentType: "image/jpeg", displayUrl: "https://cdn.example.test/new.jpg" }), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (method === "PUT" && url.includes(`/items/${ITEM}`)) {
      updates += 1;
      if (body) bodies.push(body);
      if (updates === 1) return new Response("<html><body>lost</body></html>", { status: 404, headers: { "Content-Type": "text/html" } });
      const parsed = JSON.parse(body ?? "{}") as Record<string, unknown>;
      current = story({
        versionToken: 5,
        items: [{ ...story().items[0]!, mediaAssetId: String(parsed.mediaAssetId), mediaUrl: String(parsed.mediaUrl), caption: (parsed.caption as string | null) ?? null, durationMs: Number(parsed.durationMs) }],
      });
      return new Response(JSON.stringify(current), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes(`/stories/${STORY}`)) return new Response(JSON.stringify(current), { status: 200, headers: { "Content-Type": "application/json" } });
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  try {
    let tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES, initialStoryId: STORY, navigate: () => undefined }));
    await flush();
    await waitFor(() => q(tree.container, '[data-testid="story-detail-workspace"]') != null);
    await click(q(tree.container, '[data-testid="story-slide-edit"]'));
    await waitFor(() => qq(tree.container, "dialog[open]").length === 1);
    const file = new File(["replacement-bytes"], "new.jpg", { type: "image/jpeg" });
    await assignFile(q(tree.container, '[data-testid="story-replace-file"]') as HTMLInputElement, file);
    await click(q(tree.container, '[data-testid="story-slide-save"]'));
    await waitFor(() => updates === 1);
    assert.equal(uploads, 1);
    await waitFor(() => q(tree.container, '[data-testid="story-pending-recovery-open"]') != null);
    await tree.unmount();

    tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES, initialStoryId: STORY, navigate: () => undefined }));
    await flush();
    await waitFor(() => qq(tree.container, '[data-testid="story-pending-recovery-open"]').length === 1);
    await click(q(tree.container, '[data-testid="story-pending-recovery-open"]'));
    await waitFor(() => qq(tree.container, 'dialog[open] [data-testid="story-recovery-retry"]').length === 1);
    await click(q(tree.container, '[data-testid="story-recovery-retry"]'));
    await waitFor(() => updates === 2);
    assert.equal(uploads, 1, "reopening and continuing must never upload the file again");
    assert.equal(bodies.length, 2);
    assert.equal(bodies[1], bodies[0], "the resumed UPDATE must be byte-identical to the frozen one");
    assert.equal(qq(tree.container, '[data-testid="story-pending-recovery-open"]').length, 0);
    await tree.unmount();
  } finally {
    globalThis.fetch = original;
  }
});

test("R22 F03: an edited slide asks before closing and Stay keeps the edit", async () => {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);
  window.sessionStorage.clear();
  const current = story();
  let calls = 0;
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    calls += 1;
    const url = String(input);
    if (url.includes("locale-options")) return new Response(JSON.stringify([{ code: "fa", name: "فارسی", isDefault: true }]), { status: 200, headers: { "Content-Type": "application/json" } });
    if (url.includes("/stories/query")) return new Response(JSON.stringify({ items: [current], page: 1, pageSize: 50, totalCount: 1 }), { status: 200, headers: { "Content-Type": "application/json" } });
    return new Response(JSON.stringify(current), { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
  try {
    const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES, initialStoryId: STORY, navigate: () => undefined }));
    await flush();
    await waitFor(() => q(tree.container, '[data-testid="story-detail-workspace"]') != null);
    await click(q(tree.container, '[data-testid="story-slide-edit"]'));
    await waitFor(() => qq(tree.container, "dialog[open]").length === 1);
    await setInputValue(q(tree.container, '[data-testid="story-slide-caption"]'), "ویرایش ذخیره‌نشده");
    const open = q(tree.container, "dialog[open]") as HTMLDialogElement;
    const close = Array.from(open.querySelectorAll("button")).find((button) => button.textContent?.trim() === "بستن");
    assert.ok(close);
    const before = calls;
    await click(close);
    const discardOpen = () =>
      Boolean((q(tree.container, '[data-testid="story-slide-unsaved-dialog"]') as Element | null)?.closest("dialog")?.open);
    await waitFor(discardOpen);
    await click(q(tree.container, '[data-testid="story-slide-unsaved-stay"]'));
    await waitFor(() => !discardOpen());
    assert.equal((q(tree.container, '[data-testid="story-slide-caption"]') as HTMLInputElement).value, "ویرایش ذخیره‌نشده");
    assert.equal(calls, before, "closing a dirty slide and staying must not send a request");
    await tree.unmount();
  } finally {
    globalThis.fetch = original;
  }
});

test("R22 F03: browser Back with unsaved form restores the editor URL on Stay", async () => {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);
  window.sessionStorage.clear();
  window.history.replaceState({}, "", "/fa/admin/stories");
  window.history.pushState({}, "", `/fa/admin/stories/${STORY}`);
  const current = story();
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes("locale-options")) return new Response(JSON.stringify([{ code: "fa", name: "فارسی", isDefault: true }]), { status: 200, headers: { "Content-Type": "application/json" } });
    if (url.includes("/stories/query")) return new Response(JSON.stringify({ items: [current], page: 1, pageSize: 50, totalCount: 1 }), { status: 200, headers: { "Content-Type": "application/json" } });
    return new Response(JSON.stringify(current), { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
  try {
    const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES, initialStoryId: STORY, navigate: () => undefined }));
    await flush();
    await waitFor(() => q(tree.container, '[data-testid="story-detail-workspace"]') != null);
    await setInputValue(q(tree.container, '[data-testid="story-edit-title"]'), "عنوان ذخیره‌نشده");
    await waitFor(() => window.history.state?.storyDirtyGuard === true);
    window.history.back();
    await waitFor(() => {
      const dialog = q(tree.container, '[data-testid="story-unsaved-leave-dialog"]') as Element | null;
      return Boolean(dialog?.closest("dialog")?.open);
    });
    await click(q(tree.container, '[data-testid="story-unsaved-stay"]'));
    await waitFor(() => window.location.pathname === `/fa/admin/stories/${STORY}`);
    await tree.unmount();
  } finally {
    globalThis.fetch = original;
  }
});
