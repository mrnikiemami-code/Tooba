import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { click, ensureDom, flush, mount, q, qq, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";
import type { AdminStorySnapshot } from "../story-api.ts";

ensureDom();

const STORY = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa21a1";
const ITEM = "dddddddd-dddd-dddd-dddd-dddddddd21d1";
const ACTOR = "actor-product-r21";

function story(): AdminStorySnapshot {
  const now = new Date().toISOString();
  return {
    id: STORY,
    storyId: STORY,
    tenantId: "store-alpha",
    origin: "Admin",
    reviewStatus: "Approved",
    sellerPartyId: null,
    rejectionReason: null,
    submittedAt: now,
    reviewedAt: now,
    submittedByActorUserId: null,
    reviewedByActorUserId: null,
    locale: "fa",
    market: null,
    title: "استوری محصول",
    coverMediaAssetId: null,
    coverMediaUrl: "https://cdn.example.test/cover.jpg",
    displayOrder: 0,
    startAt: null,
    endAt: null,
    status: "Draft",
    ctaType: "none",
    ctaTarget: null,
    versionToken: 4,
    createdAt: now,
    updatedAt: now,
    createdByActorUserId: ACTOR,
    shopDisplayName: null,
    contentReady: true,
    effectivelyPublic: false,
    actionCodes: ["update", "add_item"],
    items: [{
      storyItemId: ITEM,
      displayOrder: 0,
      mediaType: "image",
      mediaAssetId: "44444444-4444-4444-4444-444444444444",
      mediaUrl: "https://cdn.example.test/item.jpg",
      caption: "زیرنویس",
      durationMs: 5000,
      ctaType: "product",
      ctaTarget: "55555555-5555-5555-5555-555555555555",
      ctaProductTitle: "محصول قبلی",
      createdAt: now,
      updatedAt: now,
    }],
  };
}

function installFetch(options: { failProducts: boolean }) {
  const calls: string[] = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    calls.push(url);
    if (url.includes("locale-options")) {
      return new Response(JSON.stringify([{ code: "fa", name: "فارسی", isDefault: true }]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("product-options")) {
      if (options.failProducts) return new Response("{}", { status: 500 });
      return new Response(JSON.stringify({ items: [{ productId: "55555555-5555-5555-5555-555555555555", title: "محصول" }], totalCount: 1 }), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes(`/stories/${STORY}`)) {
      return new Response(JSON.stringify(story()), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("/stories/query")) {
      return new Response(JSON.stringify({ items: [story()], page: 1, pageSize: 50, totalCount: 1 }), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
  return { calls, restore() { globalThis.fetch = original; } };
}

test("product options are not requested while the slide dialog is closed", async () => {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);
  window.sessionStorage.clear();
  const spy = installFetch({ failProducts: false });
  try {
    const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES, initialStoryId: STORY, navigate: () => undefined }));
    await flush();
    await waitFor(() => qq(tree.container, '[data-testid="story-detail-workspace"]').length === 1);
    assert.equal(spy.calls.filter((url) => url.includes("product-options")).length, 0);
    await click(q(tree.container, '[data-testid="story-slide-edit"]'));
    await waitFor(() => qq(tree.container, '[data-testid="story-product-option"]').length === 1);
    assert.ok(spy.calls.some((url) => url.includes("product-options")));
    await tree.unmount();
  } finally {
    spy.restore();
  }
});

test("a product lookup failure is shown as an error, not as an empty catalogue", async () => {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);
  window.sessionStorage.clear();
  const spy = installFetch({ failProducts: true });
  try {
    const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES, initialStoryId: STORY, navigate: () => undefined }));
    await flush();
    await waitFor(() => qq(tree.container, '[data-testid="story-detail-workspace"]').length === 1);
    await click(q(tree.container, '[data-testid="story-slide-edit"]'));
    await waitFor(() => qq(tree.container, '[data-testid="story-product-error"]').length === 1);
    assert.equal(qq(tree.container, '[data-testid="story-product-empty"]').length, 0);
    await tree.unmount();
  } finally {
    spy.restore();
  }
});
