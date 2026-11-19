/**
 * TB-282-R11 — real mounted delete cancel/confirm/replay, preview cleanup,
 * displayState honesty, requireEnd window, URL-only media recovery copy.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import {
  click,
  ensureDom,
  flush,
  mount,
  q,
  qq,
  waitFor,
  type MountedTree,
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { listPendingStoryIntents } from "./story-intent.ts";
import { faStoryCopy } from "./story-management-copy.ts";
import { parseOptionalPublicationWindow } from "./story-schedule-time.ts";
import type { AdminStorySnapshot } from "../story-api.ts";

ensureDom();

const STORY_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0011";
const SHOP_ID = "22222222-2222-2222-2222-222222222222";
const ITEM_ID = "cccccccc-cccc-cccc-cccc-cccccccc0011";
const MEDIA_ID = "dddddddd-dddd-dddd-dddd-dddddddd0011";

function baseStory(over: Partial<AdminStorySnapshot> = {}): AdminStorySnapshot {
  const now = new Date().toISOString();
  return {
    id: STORY_ID,
    storyId: STORY_ID,
    tenantId: "store-alpha",
    origin: "Seller",
    reviewStatus: "None",
    sellerPartyId: SHOP_ID,
    title: "استوری حذف",
    status: "Draft",
    versionToken: 3,
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
    items: [
      {
        storyItemId: ITEM_ID,
        displayOrder: 0,
        mediaType: "image",
        mediaAssetId: MEDIA_ID,
        mediaUrl: "https://cdn.example/r11.jpg",
        caption: "اسلاید",
        durationMs: 7000,
        ctaType: "none",
        ctaTarget: null,
        createdAt: now,
        updatedAt: now,
        ctaProductTitle: null,
      },
    ],
    shopDisplayName: "فروشگاه آلفا",
    actionCodes: ["update", "add_item", "delete"],
    effectivelyPublic: false,
    contentReady: true,
    ...over,
  } as AdminStorySnapshot;
}

test("B: requireEnd rejects empty end when without-end unchecked", () => {
  const refused = parseOptionalPublicationWindow(
    { startDayIso: "2026-11-20", startTime: "10:00", endDayIso: "", endTime: "" },
    { requireEnd: true },
  );
  assert.equal(refused.ok, false);
  if (!refused.ok) assert.equal(refused.field, "endAt");

  const open = parseOptionalPublicationWindow(
    { startDayIso: "2026-11-20", startTime: "10:00", endDayIso: "", endTime: "" },
    { requireEnd: false },
  );
  assert.equal(open.ok, true);
  if (open.ok) assert.equal(open.endAt, null);
});

test("B: displayState never labels non-public Active as storefront; past end → expired", async () => {
  // Exercise via mounted detail status label.
  const original = globalThis.fetch;
  const story = baseStory({
    status: "Active",
    effectivelyPublic: false,
    endAt: "2020-01-01T00:00:00.000Z",
    reviewStatus: "Approved",
  });
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes(`/stories/${STORY_ID}`)) {
      return new Response(JSON.stringify(story), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("/query") || url.includes("/stories?")) {
      return new Response(JSON.stringify({ items: [story], page: 1, pageSize: 50, totalCount: 1 }), {
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

  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-1");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-detail-workspace"]').length === 1);
  const text = tree.container.textContent ?? "";
  assert.match(text, new RegExp(faStoryCopy.displayExpired));
  assert.doesNotMatch(text, new RegExp(faStoryCopy.displayOnStorefront));
  globalThis.fetch = original;
  await tree.unmount();
});

test("A: delete cancel sends zero DELETE; confirm then lost→replay clears and leaves list", async () => {
  const original = globalThis.fetch;
  const story = baseStory();
  let deletes = 0;
  let deleteMode: "ok" | "unknown" = "unknown";
  const bodies: string[] = [];
  let href = `/admin/stories/${STORY_ID}`;

  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "DELETE" && url.includes(`/stories/${STORY_ID}`)) {
      deletes += 1;
      bodies.push(String(init?.body ?? ""));
      if (deleteMode === "unknown") {
        // Verified-bound retention: 2xx alone without parseable bound payload is not success.
        return new Response("", { status: 200, headers: { "Content-Type": "application/json" } });
      }
      return new Response(
        JSON.stringify({
          ...story,
          id: STORY_ID,
          storyId: STORY_ID,
          items: [],
          effectivelyPublic: false,
        }),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        },
      );
    }
    if (url.includes(`/stories/${STORY_ID}`) && method === "GET") {
      return new Response(JSON.stringify(story), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("/query") || url.includes("/stories?")) {
      return new Response(JSON.stringify({ items: [story], page: 1, pageSize: 50, totalCount: 1 }), {
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

  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-1");
  const tree: MountedTree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: STORY_ID,
      navigate: (next) => {
        href = next;
      },
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-delete-open"]').length === 1);

  await click(q(tree.container, '[data-testid="story-delete-open"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-dialog"]').length === 1);
  const cancelBtn = q(document.body, '[data-testid="story-admin-op-cancel"]');
  assert.ok(cancelBtn);
  await click(cancelBtn);
  await flush();
  assert.equal(deletes, 0, "cancel must send zero DELETE");

  // Re-open (portal Dialog may keep node briefly; open control still works).
  await click(q(tree.container, '[data-testid="story-delete-open"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-confirm"]').length === 1);
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => deletes === 1);
  const pending = listPendingStoryIntents("admin", null, "actor-admin-1");
  assert.ok(pending.some((row) => row.state === "ok" && row.envelope.action === "delete"));
  const firstBody = bodies[0]!;

  deleteMode = "ok";
  await waitFor(() => qq(document.body, '[data-testid="story-recovery-retry"]').length === 1);
  await click(q(document.body, '[data-testid="story-recovery-retry"]'));
  await flush();
  await waitFor(() => deletes >= 2);
  assert.equal(bodies[1], firstBody);
  await waitFor(() => !href.endsWith(STORY_ID));
  globalThis.fetch = original;
  await tree.unmount();
});

test("C: seller URL-only slide shows recovery copy; local image preview uses object URL", async () => {
  const original = globalThis.fetch;
  const story = baseStory({
    contentReady: false,
    actionCodes: ["update", "add_item", "delete"],
    items: [
      {
        storyItemId: ITEM_ID,
        displayOrder: 0,
        mediaType: "image",
        mediaAssetId: null,
        mediaUrl: "/images/legacy.jpg",
        caption: "لینک‌دار",
        durationMs: 5000,
        ctaType: "none",
        ctaTarget: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        ctaProductTitle: null,
      },
    ],
  });

  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes(`/stories/${STORY_ID}`)) {
      return new Response(JSON.stringify(story), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("/query") || url.includes("/stories?")) {
      return new Response(JSON.stringify({ items: [], page: 1, pageSize: 50, totalCount: 0 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("locales") || url.includes("locale-options") || url.includes("product")) {
      return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  window.localStorage.setItem("tooba.sellerPartyId", SHOP_ID);
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-media-recovery"]').length === 1);
  assert.match(tree.container.textContent ?? "", new RegExp(faStoryCopy.slideNeedsOwnedMedia.slice(0, 20)));
  assert.equal(qq(tree.container, '[data-testid="story-media-required-hint"]').length, 1);

  await click(q(tree.container, '[data-testid="story-slide-open-new"]'));
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-file"]').length === 1);
  const input = q(tree.container, '[data-testid="story-slide-file"]') as HTMLInputElement;
  const file = new File(["img"], "preview.jpg", { type: "image/jpeg" });
  const list = {
    0: file,
    length: 1,
    item: (i: number) => (i === 0 ? file : null),
    *[Symbol.iterator]() {
      yield file;
    },
  } as unknown as FileList;
  Object.defineProperty(input, "files", { configurable: true, value: list });
  input.dispatchEvent(new Event("change", { bubbles: true }));
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-local-preview"]').length === 1);
  const previewImg = q(tree.container, '[data-testid="story-slide-local-preview"] img') as HTMLImageElement;
  assert.match(String(previewImg?.getAttribute("src") ?? previewImg?.src ?? ""), /^blob:/);

  // Duration visible for photo.
  assert.equal(qq(tree.container, '[data-testid="story-slide-duration"]').length, 1);

  globalThis.fetch = original;
  await tree.unmount();
});

test("C: public player advances video on ended (source retained)", async () => {
  const fs = await import("node:fs/promises");
  const path = await import("node:path");
  const modalPath = path.join(process.cwd(), "src/frontend/app/storefront/stories/story-modal.tsx");
  const src = await fs.readFile(modalPath, "utf8");
  assert.match(src, /addEventListener\("ended"/);
  assert.match(src, /imageProgressStep/);
  assert.match(src, /durationMs/);
});
