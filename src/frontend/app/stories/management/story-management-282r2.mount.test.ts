/**
 * TB-282-R2 B3 — recovery click sends identical frozen bytes; schedule; lookups.
 * Run:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/stories/management/story-management-282r2.mount.test.ts
 */
import "./story-mount-harness.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { act } from "react";
import { flush, mount, q } from "./story-mount-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import {
  listPendingStoryIntents,
  storeStoryIntent,
  type StoryIntentEnvelope,
} from "./story-intent.ts";
import { runDurableStoryMutation, type StoryExecutorGate } from "./story-executor.ts";
import { combineTehranDayAndTime } from "./story-schedule-time.ts";

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
    items: [],
    id: "11111111-1111-1111-1111-111111111111",
    createdByActorUserId: null,
    shopDisplayName: "فروشگاه آلفا",
    contentReady: true,
    effectivelyPublic: false,
    actionCodes: ["update", "add_item", "submit"],
    ...overrides,
  };
}

test("recovery click resends exact frozen create bytes after unknown outcome", async () => {
  sessionStorage.clear();
  localStorage.setItem("tooba.sellerPartyId", "22222222-2222-2222-2222-222222222222");
  localStorage.setItem("tooba.sellerActorUserId", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  const seen: Array<{ method: string; url: string; body: string | null }> = [];
  let createCalls = 0;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = typeof init?.body === "string" ? init.body : null;
    seen.push({ method, url, body });
    if (url.includes("/locale-options") || url.includes("/product-options")) {
      return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("/v1/seller/stories/") && method === "GET") {
      return new Response(JSON.stringify(baseStory()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/query")) {
      return new Response(JSON.stringify({ items: [], page: 1, pageSize: 50, totalCount: 0 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url === "/v1/seller/stories" && method === "POST") {
      createCalls += 1;
      if (createCalls === 1) {
        return new Response("{}", { status: 500, headers: { "Content-Type": "application/json" } });
      }
      return new Response(JSON.stringify({ storyId: "11111111-1111-1111-1111-111111111111" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  const body = JSON.stringify({
    title: "بازیابی",
    occurrenceKey: "occ-create-1",
    sellerPartyId: null,
  });
  const envelope: StoryIntentEnvelope = {
    version: 2,
    action: "create",
    occurrenceKey: "occ-create-1",
    method: "POST",
    path: "/v1/seller/stories",
    routeTargetId: null,
    expectedVersion: null,
    bodyText: body,
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "seller",
    shopId: "22222222-2222-2222-2222-222222222222",
    actorUserId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    recordedAt: new Date().toISOString(),
  };
  assert.equal(storeStoryIntent(envelope), true);

  const gate: StoryExecutorGate = { inFlightRef: { current: false }, epochRef: { current: 1 } };
  const first = await runDurableStoryMutation({
    gate,
    envelope,
    scope: {
      role: "seller",
      shopId: "22222222-2222-2222-2222-222222222222",
      actorUserId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    },
  });
  assert.equal(first.state, "failed");
  assert.equal(
    listPendingStoryIntents(
      "seller",
      "22222222-2222-2222-2222-222222222222",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    ).length,
    1,
  );

  const { host, unmount } = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      initialStoryId: "11111111-1111-1111-1111-111111111111",
      navigate: () => undefined,
    }),
  );
  await flush(40);
  const retry = q(host, '[data-testid="story-recovery-retry"]') as HTMLButtonElement | null;
  assert.ok(retry, "recovery button must mount");
  await act(async () => {
    retry!.click();
    await flush(40);
  });
  const posts = seen.filter((row) => row.method === "POST" && row.url === "/v1/seller/stories");
  assert.equal(posts.length, 2);
  assert.equal(posts[0]!.body, body);
  assert.equal(posts[1]!.body, body);
  await unmount();
});

test("same-key changed body blocked — focused negative control fails if overwrite allowed", () => {
  sessionStorage.clear();
  const first: StoryIntentEnvelope = {
    version: 2,
    action: "submit",
    occurrenceKey: "same-key",
    method: "POST",
    path: "/v1/seller/stories/x/submit",
    routeTargetId: "x",
    expectedVersion: 1,
    bodyText: "{\"expectedVersion\":1}",
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "seller",
    shopId: "shop",
    actorUserId: "actor",
    recordedAt: new Date().toISOString(),
  };
  assert.equal(storeStoryIntent(first), true);
  function brokenStore(envelope: StoryIntentEnvelope): boolean {
    sessionStorage.setItem(
      `tooba.story.intent.v2:${envelope.role}:${envelope.shopId}:${envelope.actorUserId}:${envelope.action}:${envelope.routeTargetId}`,
      JSON.stringify(envelope),
    );
    return true;
  }
  assert.equal(brokenStore({ ...first, bodyText: "{\"expectedVersion\":9}" }), true);
  sessionStorage.clear();
  assert.equal(storeStoryIntent(first), true);
  assert.equal(storeStoryIntent({ ...first, bodyText: "{\"expectedVersion\":9}" }), false);
});

test("Tehran schedule conversion does not use browser Date(string) authority", () => {
  const iso = combineTehranDayAndTime("2026-03-21", "00:00");
  assert.equal(iso, "2026-03-20T20:30:00.000Z");
});

test("admin product lookup URL is story product-options not seller offers", async () => {
  sessionStorage.clear();
  localStorage.setItem("tooba.adminActorUserId", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  const urls: string[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    urls.push(url);
    if (url.includes("/product-options") || url.includes("/locale-options")) {
      return new Response(JSON.stringify([{ productId: "p1", title: "A" }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/admin/stories/") && !url.includes("options") && !url.includes("query")) {
      return new Response(JSON.stringify(baseStory({ origin: "Platform", sellerPartyId: null })), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/query") || url.includes("/v1/admin/stories")) {
      return new Response(JSON.stringify({ items: [baseStory()], page: 1, pageSize: 50, totalCount: 1 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/admin/sellers")) {
      return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  const { unmount } = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: "11111111-1111-1111-1111-111111111111",
      navigate: () => undefined,
    }),
  );
  await flush(50);
  assert.ok(urls.some((u) => u.includes("/v1/admin/stories/locale-options")));
  assert.ok(urls.some((u) => u.includes("/v1/admin/stories/product-options")));
  assert.ok(!urls.some((u) => u.includes("/v1/seller/offers")));
  assert.ok(!urls.some((u) => u.includes("/v1/admin/languages")));
  await unmount();
});
