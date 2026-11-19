/**
 * TB-282-R4 focused mounted/direct facts for S1 upload bind, update_item route,
 * Orders-style column order, and shared merchant editor placement/edit controls.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import {
  ensureDom,
  flush,
  mount,
  qq,
  waitFor,
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { bindStoryEnvelopeRoute, verifyStorySuccessPayload } from "./story-response.ts";
import { runDurableUploadAdd, runDurableStoryMutation, type StoryExecutorGate } from "./story-executor.ts";
import type { StoryIntentEnvelope } from "./story-intent.ts";
import { normalizeStoryColumnOrder } from "./story-column-order.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { enStoryCopy, faStoryCopy, storyCopy } from "./story-management-copy.ts";

ensureDom();

function baseEnvelope(over: Partial<StoryIntentEnvelope> = {}): StoryIntentEnvelope {
  return {
    version: 2,
    action: "upload_add",
    occurrenceKey: "occ-r4-1",
    method: "POST",
    path: "/v1/admin/media/upload-story-platform",
    routeTargetId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    expectedVersion: 1,
    bodyText: JSON.stringify({ caption: "c", durationSec: 5, ctaType: "none", ctaTarget: null, expectedVersion: 1 }),
    uploadMeta: { fileName: "a.jpg", fileType: "image/jpeg", fileSize: 12, digestHex: "aa" },
    addBodyText: null,
    uploadedAssetId: null,
    children: [
      { step: "upload", occurrenceKey: "occ-r4-1", done: false },
      { step: "add", occurrenceKey: "occ-r4-2", done: false },
    ],
    role: "admin",
    shopId: null,
    actorUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    recordedAt: new Date().toISOString(),
    ...over,
  };
}

test("S1: appended upload path is refused before fetch (anchored bind)", () => {
  const bad = baseEnvelope({ path: "/v1/admin/media/upload-story-platform/../other" });
  assert.equal(bindStoryEnvelopeRoute(bad).ok, false);
});

test("S1: wrong upload_add recovery path sends zero fetch", async () => {
  const gate: StoryExecutorGate = { inFlightRef: { current: false }, epochRef: { current: 1 } };
  const calls: string[] = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    calls.push(String(input));
    return new Response("{}", { status: 200 });
  }) as typeof fetch;
  try {
    const poisoned = baseEnvelope({
      children: [
        { step: "upload", occurrenceKey: "occ-r4-1", done: true },
        { step: "add", occurrenceKey: "occ-r4-2", done: false },
      ],
      uploadedAssetId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      addBodyText: JSON.stringify({ mediaAssetId: "cccccccc-cccc-cccc-cccc-cccccccccccc", mediaType: "image", expectedVersion: 1, occurrenceKey: "occ-r4-2" }),
      method: "POST",
      path: "/v1/admin/orders/query",
      bodyText: JSON.stringify({ mediaAssetId: "cccccccc-cccc-cccc-cccc-cccccccccccc", mediaType: "image", expectedVersion: 1, occurrenceKey: "occ-r4-2" }),
    });
    const outcome = await runDurableUploadAdd({
      gate,
      envelope: poisoned,
      scope: { role: "admin", shopId: null, actorUserId: poisoned.actorUserId },
    });
    assert.equal(outcome.state, "blocked");
    assert.equal(calls.length, 0);
  } finally {
    globalThis.fetch = original;
  }
});

test("S1 negative control: pre-fix upload path without bind would have fetched foreign URL", async () => {
  // Precise production regression: without bindStoryEnvelopeRoute, a foreign stored path would fetch.
  // Current production refuses before fetch.
  const foreign = baseEnvelope({
    children: [
      { step: "upload", occurrenceKey: "occ-r4-1", done: true },
      { step: "add", occurrenceKey: "occ-r4-2", done: false },
    ],
    uploadedAssetId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    addBodyText: JSON.stringify({ mediaAssetId: "cccccccc-cccc-cccc-cccc-cccccccccccc", expectedVersion: 1, occurrenceKey: "occ-r4-2" }),
    path: "/v1/admin/returns/query",
    method: "POST",
  });
  assert.equal(bindStoryEnvelopeRoute(foreign).ok, false);
  const empty = verifyStorySuccessPayload(baseEnvelope({ action: "update", path: "/v1/admin/stories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" }), {});
  assert.equal(empty.ok, false);
});

test("C: update_item route binds PUT …/items/{id} only", () => {
  const ok = bindStoryEnvelopeRoute(
    baseEnvelope({
      action: "update_item",
      method: "PUT",
      path: "/v1/admin/stories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/items/dddddddd-dddd-dddd-dddd-dddddddddddd",
      bodyText: JSON.stringify({ caption: "x", expectedVersion: 2, occurrenceKey: "k" }),
      children: [],
      uploadMeta: null,
    }),
  );
  const bad = bindStoryEnvelopeRoute(
    baseEnvelope({
      action: "update_item",
      method: "POST",
      path: "/v1/admin/stories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/items",
      bodyText: "{}",
      children: [],
      uploadMeta: null,
    }),
  );
  assert.equal(ok.ok, true);
  assert.equal(bad.ok, false);
});

test("A: title-first actions-last survives old saved column order", () => {
  const order = normalizeStoryColumnOrder(["actions", "shopDisplayName", "title", "items"]);
  assert.deepEqual(order[0], "title");
  assert.deepEqual(order.at(-1), "actions");
});

test("S1: update_item wrong method is blocked with zero fetch", async () => {
  const gate: StoryExecutorGate = { inFlightRef: { current: false }, epochRef: { current: 1 } };
  let fetches = 0;
  const original = globalThis.fetch;
  globalThis.fetch = (async () => {
    fetches += 1;
    return new Response("{}", { status: 200 });
  }) as typeof fetch;
  try {
    const outcome = await runDurableStoryMutation({
      gate,
      envelope: baseEnvelope({
        action: "update_item",
        method: "DELETE",
        path: "/v1/admin/stories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/items/dddddddd-dddd-dddd-dddd-dddddddddddd",
        bodyText: JSON.stringify({ expectedVersion: 1, occurrenceKey: "k" }),
        children: [],
        uploadMeta: null,
      }),
      scope: { role: "admin", shopId: null, actorUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" },
    });
    assert.equal(outcome.state, "blocked");
    assert.equal(fetches, 0);
  } finally {
    globalThis.fetch = original;
  }
});

function detailStory(overrides: Record<string, unknown> = {}) {
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
        ctaType: "product",
        ctaTarget: "44444444-4444-4444-4444-444444444444",
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ],
    id: "11111111-1111-1111-1111-111111111111",
    createdByActorUserId: null,
    shopDisplayName: "فروشگاه آلفا",
    contentReady: true,
    effectivelyPublic: false,
    actionCodes: ["update", "add_item", "submit", "update_item"],
    ...overrides,
  };
}

function stubEditorFetch() {
  return (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes("/query")) {
      return new Response(JSON.stringify({ items: [detailStory()], page: 1, pageSize: 50, totalCount: 1 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/product-options")) {
      return new Response(
        JSON.stringify({
          items: [{ productId: "44444444-4444-4444-4444-444444444444", title: "کالای انتخابی" }],
          totalCount: 1,
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("/v1/admin/stories/") || url.includes("/v1/seller/stories/")) {
      return new Response(JSON.stringify(detailStory()), {
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
    if (url.includes("/v1/admin/sellers")) {
      return new Response(
        JSON.stringify([{ sellerPartyId: "22222222-2222-2222-2222-222222222222", displayName: "فروشگاه آلفا" }]),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
}

test("B/C mounted: admin create shows named placement; seller shop is fixed", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = stubEditorFetch();
  window.localStorage.setItem("tooba.sellerPartyId", "22222222-2222-2222-2222-222222222222");
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  const admin = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      navigate: () => undefined,
      createMode: true,
    }),
  );
  await flush();
  await waitFor(() => (admin.container.textContent ?? "").includes(faStoryCopy.mainStorefront));
  assert.match(admin.container.textContent ?? "", new RegExp(faStoryCopy.forOneShop));
  await admin.unmount();

  const seller = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      navigate: () => undefined,
      createMode: true,
    }),
  );
  await flush();
  await waitFor(() => qq(seller.container, '[data-testid="story-seller-shop-fixed"]').length === 1);
  assert.equal(qq(seller.container, '[data-testid="story-admin-shop-picker"]').length, 0);
  globalThis.fetch = original;
  await seller.unmount();
});

test("C mounted: existing slide shows destination and opens shared edit dialog", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = stubEditorFetch();
  window.localStorage.setItem("tooba.sellerPartyId", "22222222-2222-2222-2222-222222222222");
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      navigate: () => undefined,
      initialStoryId: "11111111-1111-1111-1111-111111111111",
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-row"]').length >= 1);
  assert.match(tree.container.textContent ?? "", /مقصد:/);
  assert.equal(qq(tree.container, '[data-testid="story-slide-edit"]').length >= 1, true);
  qq(tree.container, '[data-testid="story-slide-edit"]')[0]!.dispatchEvent(new MouseEvent("click", { bubbles: true }));
  await flush();
  await waitFor(() => (tree.container.textContent ?? "").includes(faStoryCopy.editSlideTitle));
  assert.match(tree.container.textContent ?? "", new RegExp(faStoryCopy.destinationProduct));
  globalThis.fetch = original;
  await tree.unmount();
});

test("i18n catalogs stay key-aligned for extensibility", () => {
  assert.equal(storyCopy("en").createButton, enStoryCopy.createButton);
  assert.equal(Object.keys(faStoryCopy).length, Object.keys(enStoryCopy).length);
});
