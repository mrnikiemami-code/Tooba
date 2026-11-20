/**
 * Story CTA + edit-form contract: the edit surface sends the same preserved fields as the create
 * surface, validates every supported CTA before the wire, and exposes a real publish-now command.
 * Mounts the real workspace; no backend or live row is used.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import test from "node:test";
import { createElement } from "react";
import {
  click,
  ensureDom,
  flush,
  mount,
  q,
  qq,
  setInputValue,
  setSelectValue,
  waitFor,
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";
import type { AdminStorySnapshot } from "../story-api.ts";

ensureDom();

const STORY_ID = "11111111-1111-1111-1111-111111111111";
const ITEM_ID = "22222222-2222-2222-2222-222222222222";
const PRODUCT_ID = "55555555-5555-5555-5555-555555555555";
const ACTOR = "actor-cta-edit";

type Call = { method: string; url: string; body: string | null };

function story(): AdminStorySnapshot {
  const now = new Date().toISOString();
  return {
    id: STORY_ID,
    storyId: STORY_ID,
    tenantId: "store-alpha",
    origin: "Admin",
    reviewStatus: "None",
    sellerPartyId: null,
    rejectionReason: null,
    submittedAt: null,
    reviewedAt: null,
    submittedByActorUserId: null,
    reviewedByActorUserId: null,
    locale: "fa",
    market: "IR",
    title: "استوری آزمایشی",
    coverMediaAssetId: "33333333-3333-3333-3333-333333333333",
    coverMediaUrl: "/images/stories/cover.jpg",
    displayOrder: 0,
    startAt: null,
    endAt: null,
    status: "Draft",
    ctaType: "external",
    ctaTarget: "https://example.com/story",
    versionToken: 4,
    createdAt: now,
    updatedAt: now,
    createdByActorUserId: ACTOR,
    shopDisplayName: null,
    contentReady: true,
    effectivelyPublic: false,
    actionCodes: ["update", "add_item", "delete", "publish_now", "schedule"],
    items: [
      {
        storyItemId: ITEM_ID,
        displayOrder: 0,
        mediaType: "image",
        mediaAssetId: "44444444-4444-4444-4444-444444444444",
        mediaUrl: "/images/stories/item.jpg",
        caption: "زیرنویس قبلی",
        durationMs: 5000,
        ctaType: "none",
        ctaTarget: null,
        ctaProductTitle: null,
        createdAt: now,
        updatedAt: now,
      },
    ],
  };
}

function jsonResponse(value: unknown): Response {
  return new Response(JSON.stringify(value), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

test("edit form preserves story fields and validates every CTA before saving", async () => {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);
  window.sessionStorage.clear();
  const originalFetch = globalThis.fetch;
  const calls: Call[] = [];
  let current = story();

  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = typeof init?.body === "string" ? init.body : null;
    if (method === "GET" && url.includes("/locale-options")) {
      return jsonResponse([{ code: "fa", name: "فارسی", isDefault: true }]);
    }
    if (method === "GET" && url.includes("/product-options")) {
      return jsonResponse([
        { productId: PRODUCT_ID, offerId: "offer-cta-1", productTitle: "محصول تست" },
      ]);
    }
    if (method === "GET" && url.includes(`/v1/admin/stories/${STORY_ID}`)) {
      return jsonResponse(current);
    }
    if (method === "POST" && url.includes("/stories/query")) {
      return jsonResponse({ items: [current], page: 1, pageSize: 50, totalCount: 1 });
    }
    if (url.includes(`/v1/admin/stories/${STORY_ID}`)) {
      calls.push({ method, url, body });
      current = {
        ...current,
        title: body && method === "PUT" && !url.includes("/items/") ? "عنوان تازه" : current.title,
        versionToken: current.versionToken + 1,
      };
      return jsonResponse(current);
    }
    return jsonResponse({});
  }) as typeof fetch;

  try {
    const tree = await mount(
      createElement(StoryManagementScreen, {
        capabilities: ADMIN_STORY_CAPABILITIES,
        initialStoryId: STORY_ID,
        navigate: () => undefined,
      }),
    );
    await flush();
    await waitFor(() => qq(tree.container, '[data-testid="story-detail-workspace"]').length === 1);

    // Edit story fields through the same card as the create form.
    await setInputValue(q(tree.container, '[data-testid="story-edit-title"]'), "عنوان تازه");
    await click(q(tree.container, '[data-testid="story-save-title-locale"]'));
    await waitFor(() => calls.some((call) => call.method === "PUT" && !call.url.includes("/items/")));
    const storyUpdate = calls.find((call) => call.method === "PUT" && !call.url.includes("/items/"))!;
    const parsedStory = JSON.parse(storyUpdate.body!) as Record<string, unknown>;
    assert.equal(parsedStory.title, "عنوان تازه");
    assert.equal(parsedStory.market, "IR");
    assert.equal(parsedStory.coverMediaAssetId, "33333333-3333-3333-3333-333333333333");
    assert.equal(parsedStory.coverMediaUrl, "/images/stories/cover.jpg");
    assert.equal(parsedStory.ctaType, "external");
    assert.equal(parsedStory.ctaTarget, "https://example.com/story");

    // Internal CTA is a supported, validated destination and preserves the existing slide media.
    await click(q(tree.container, '[data-testid="story-slide-edit"]'));
    await waitFor(() => qq(document.body, '[data-testid="story-slide-editor"]').length === 1);
    await setSelectValue(q(document.body, '[data-testid="story-slide-cta-type"]'), "internal");
    await waitFor(() => qq(document.body, '[data-testid="story-slide-cta-internal"]').length === 1);
    await setInputValue(q(document.body, '[data-testid="story-slide-cta-internal"]'), "/products");
    await click(q(document.body, '[data-testid="story-slide-save"]'));
    await waitFor(() => calls.some((call) => call.method === "PUT" && call.url.includes("/items/")));
    const itemUpdate = calls.find((call) => call.method === "PUT" && call.url.includes("/items/"))!;
    const parsedItem = JSON.parse(itemUpdate.body!) as Record<string, unknown>;
    assert.equal(parsedItem.ctaType, "internal");
    assert.equal(parsedItem.ctaTarget, "/products");
    assert.equal(parsedItem.mediaAssetId, "44444444-4444-4444-4444-444444444444");
    assert.equal(parsedItem.mediaUrl, "/images/stories/item.jpg");
    assert.equal(parsedItem.caption, "زیرنویس قبلی");

    // An invalid external URL is refused locally; no extra PUT reaches the wire.
    const putCountBefore = calls.filter((call) => call.method === "PUT").length;
    await click(q(tree.container, '[data-testid="story-slide-edit"]'));
    await waitFor(() => qq(document.body, '[data-testid="story-slide-editor"]').length === 1);
    await setSelectValue(q(document.body, '[data-testid="story-slide-cta-type"]'), "external");
    await setInputValue(q(document.body, '[data-testid="story-slide-cta-external"]'), "ftp://bad.example");
    await click(q(document.body, '[data-testid="story-slide-save"]'));
    await waitFor(() => qq(document.body, '[data-testid="story-slide-cta-error"]').length === 1);
    assert.equal(calls.filter((call) => call.method === "PUT").length, putCountBefore);
    await setInputValue(q(document.body, '[data-testid="story-slide-cta-external"]'), "https://example.com/new");
    await click(q(document.body, '[data-testid="story-slide-save"]'));
    await waitFor(() => calls.filter((call) => call.method === "PUT").length === putCountBefore + 1);

    // Product CTA is selected from the real picker and sent with its owning product id.
    await click(q(tree.container, '[data-testid="story-slide-edit"]'));
    await waitFor(() => qq(document.body, '[data-testid="story-slide-editor"]').length === 1);
    await setSelectValue(q(document.body, '[data-testid="story-slide-cta-type"]'), "product");
    await waitFor(() => qq(document.body, '[data-testid="story-product-option"]').length === 1);
    await click(q(document.body, '[data-testid="story-product-option"]'));
    const putBeforeProduct = calls.filter((call) => call.method === "PUT").length;
    await click(q(document.body, '[data-testid="story-slide-save"]'));
    await waitFor(() => calls.filter((call) => call.method === "PUT").length === putBeforeProduct + 1);
    const productUpdate = calls.filter((call) => call.method === "PUT").at(-1)!;
    const parsedProduct = JSON.parse(productUpdate.body!) as Record<string, unknown>;
    assert.equal(parsedProduct.ctaType, "product");
    assert.equal(parsedProduct.ctaTarget, PRODUCT_ID);

    // The ready story exposes the real publish-now command, not only scheduling.
    await click(q(tree.container, '[data-testid="story-detail-publish-now"]'));
    await waitFor(() => calls.some((call) => call.method === "POST" && call.url.includes("/enable")));
    const publish = calls.find((call) => call.method === "POST" && call.url.includes("/enable"))!;
    assert.ok(publish.body && JSON.parse(publish.body).occurrenceKey);
    await tree.unmount();
  } finally {
    globalThis.fetch = originalFetch;
  }
});
