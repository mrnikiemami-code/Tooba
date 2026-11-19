/**
 * TB-282-R1 F5 — mount REAL StoryManagementScreen + executor behaviors (not fs regex).
 * Run:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/stories/management/story-management-282r1.mount.test.ts
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
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import {
  clearStoryIntentIfCurrent,
  listPendingStoryIntents,
  storeStoryIntent,
  type StoryIntentEnvelope,
} from "./story-intent.ts";
import { runDurableStoryMutation } from "./story-executor.ts";

ensureDom();

function baseStory(overrides: Record<string, unknown> = {}) {
  return {
    storyId: "11111111-1111-1111-1111-111111111111",
    tenantId: "store-alpha",
    origin: "Seller",
    reviewStatus: "None",
    sellerPartyId: "22222222-2222-2222-2222-222222222222",
    rejectionReason: null,
    submittedAt: null,
    reviewedAt: null,
    submittedByActorUserId: null,
    reviewedByActorUserId: null,
    locale: "fa-IR",
    market: null,
    title: "استوری تست",
    coverMediaAssetId: null,
    coverMediaUrl: null,
    displayOrder: 1,
    startAt: null,
    endAt: null,
    status: "Draft",
    ctaType: "none",
    ctaTarget: null,
    versionToken: 1,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    items: [
      {
        storyItemId: "33333333-3333-3333-3333-333333333333",
        displayOrder: 0,
        mediaType: "image",
        mediaAssetId: null,
        mediaUrl: "https://cdn.example/a.jpg",
        caption: "زیرنویس",
        durationMs: 5000,
        ctaType: "none",
        ctaTarget: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ],
    id: "11111111-1111-1111-1111-111111111111",
    createdByActorUserId: null,
    shopDisplayName: "فروشگاه آلفا",
    contentReady: true,
    effectivelyPublic: false,
    actionCodes: ["update", "add_item", "submit"],
    ...overrides,
  };
}

function stubFetch() {
  return (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes("/query")) {
      return new Response(JSON.stringify({ items: [baseStory()], page: 1, pageSize: 50, totalCount: 1 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/admin/stories/") && !url.includes("query")) {
      return new Response(
        JSON.stringify(
          baseStory({
            origin: "Seller",
            reviewStatus: "Submitted",
            actionCodes: ["approve_and_publish", "reject", "approve_and_schedule"],
          }),
        ),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("/v1/seller/stories/") && !url.includes("query")) {
      return new Response(JSON.stringify(baseStory()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/admin/languages")) {
      return new Response(
        JSON.stringify([
          {
            code: "fa-IR",
            nativeName: "فارسی",
            active: true,
            default: true,
            urlPrefix: "fa",
            displayName: "fa",
            direction: "rtl",
            culture: "fa-IR",
            calendarDisplay: "jalali",
            sortOrder: 0,
          },
        ]),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("/v1/admin/sellers") || url.includes("/offers")) {
      return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
}

test("seller screen mounts without admin review controls", async () => {
  window.localStorage.setItem("tooba.sellerPartyId", "22222222-2222-2222-2222-222222222222");
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  const originalFetch = globalThis.fetch;
  globalThis.fetch = stubFetch();
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      navigate: () => undefined,
      disableLiveGrid: true,
      createMode: true,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="seller-stories"]').length === 1);
  assert.equal(qq(tree.container, '[data-testid="admin-story-review-filter"]').length, 0);
  assert.ok(!(tree.container.textContent ?? "").includes("تأیید و انتشار"));
  globalThis.fetch = originalFetch;
  await tree.unmount();
});

test("durable executor refuses overwrite and remount shows pending recovery", async () => {
  window.sessionStorage.clear();
  window.localStorage.setItem("tooba.sellerPartyId", "22222222-2222-2222-2222-222222222222");
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  const pending: StoryIntentEnvelope = {
    version: 2,
    action: "submit",
    occurrenceKey: "occ-pending-1",
    method: "POST",
    path: "/v1/seller/stories/11111111-1111-1111-1111-111111111111/submit",
    routeTargetId: "11111111-1111-1111-1111-111111111111",
    expectedVersion: 1,
    bodyText: "{\"expectedVersion\":1,\"occurrenceKey\":\"occ-pending-1\"}",
    uploadMeta: null,
    children: [],
    role: "seller",
    shopId: "22222222-2222-2222-2222-222222222222",
    actorUserId: "actor-seller-1",
    recordedAt: new Date().toISOString(),
  };
  assert.equal(storeStoryIntent(pending), true);
  const gate = { inFlightRef: { current: false }, epochRef: { current: 1 } };
  const blocked = await runDurableStoryMutation({
    gate,
    envelope: { ...pending, occurrenceKey: "occ-other" },
    send: async () => ({ ok: true, data: true }),
  });
  assert.equal(blocked.state, "blocked");
  assert.equal(blocked.state === "blocked" && blocked.reason, "storage");

  const originalFetch = globalThis.fetch;
  globalThis.fetch = stubFetch();
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      navigate: () => undefined,
      disableLiveGrid: true,
      createMode: true,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-pending-recovery"]').length === 1);
  assert.ok((tree.container.textContent ?? "").includes("اقدام ناتمام"));
  assert.equal(listPendingStoryIntents("seller", "22222222-2222-2222-2222-222222222222").length, 1);
  clearStoryIntentIfCurrent(pending);
  globalThis.fetch = originalFetch;
  await tree.unmount();
});

test("same-tick double click executor sends once", async () => {
  window.sessionStorage.clear();
  const gate = { inFlightRef: { current: false }, epochRef: { current: 1 } };
  let sends = 0;
  const envelope: StoryIntentEnvelope = {
    version: 2,
    action: "create",
    occurrenceKey: "occ-once",
    method: "POST",
    path: "/v1/seller/stories",
    routeTargetId: null,
    expectedVersion: null,
    bodyText: "{}",
    uploadMeta: null,
    children: [],
    role: "seller",
    shopId: "shop",
    actorUserId: "actor",
    recordedAt: new Date().toISOString(),
  };
  const p1 = runDurableStoryMutation({
    gate,
    envelope,
    send: async () => {
      sends += 1;
      await new Promise((r) => setTimeout(r, 30));
      return { ok: true, data: true };
    },
  });
  const p2 = runDurableStoryMutation({
    gate,
    envelope: { ...envelope, occurrenceKey: "occ-once" },
    send: async () => {
      sends += 1;
      return { ok: true, data: true };
    },
  });
  const [a, b] = await Promise.all([p1, p2]);
  assert.equal(sends, 1);
  assert.equal(a.state, "ok");
  assert.equal(b.state, "blocked");
});

test("admin publish preview dialog requires deliberate confirm", async () => {
  window.localStorage.setItem("tooba.adminActorUserId", "admin-1");
  const originalFetch = globalThis.fetch;
  globalThis.fetch = stubFetch();
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: "11111111-1111-1111-1111-111111111111",
      navigate: () => undefined,
      disableLiveGrid: true,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-detail-workspace"]').length === 1);
  const publishBtn = Array.from(tree.container.querySelectorAll("button")).find((b) =>
    (b.textContent ?? "").includes("تأیید و انتشار"),
  );
  assert.ok(publishBtn);
  await click(publishBtn!);
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-publish-preview"]').length === 1);
  globalThis.fetch = originalFetch;
  await tree.unmount();
});

test("negative control: pending overwrite refusal holds", async () => {
  window.sessionStorage.clear();
  const first: StoryIntentEnvelope = {
    version: 2,
    action: "submit",
    occurrenceKey: "a",
    method: "POST",
    path: "/x",
    routeTargetId: "t",
    expectedVersion: 1,
    bodyText: "{}",
    uploadMeta: null,
    children: [],
    role: "seller",
    shopId: "s",
    actorUserId: "u",
    recordedAt: new Date().toISOString(),
  };
  assert.equal(storeStoryIntent(first), true);
  assert.equal(storeStoryIntent({ ...first, occurrenceKey: "b" }), false);
});

void q;
