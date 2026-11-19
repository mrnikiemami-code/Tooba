/**
 * TB-282-R6 focused mounted facts: full /new form, list-only review filter,
 * truthful platform/shop location, merchant destination labels.
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
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { faStoryCopy } from "./story-management-copy.ts";

ensureDom();

const STORY_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const SHOP_ID = "22222222-2222-2222-2222-222222222222";

function shopStory(over: Record<string, unknown> = {}) {
  return {
    storyId: STORY_ID,
    tenantId: "store-alpha",
    origin: "Seller",
    reviewStatus: "None",
    sellerPartyId: SHOP_ID,
    title: "استوری فروشگاه",
    status: "Draft",
    versionToken: 1,
    items: [
      {
        storyItemId: "item-internal",
        displayOrder: 0,
        mediaType: "image",
        mediaAssetId: "media-1",
        mediaUrl: "https://cdn.example/x.jpg",
        caption: "کپشن",
        durationMs: 5000,
        ctaType: "internal",
        ctaTarget: "shop-home",
        ctaProductTitle: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ],
    shopDisplayName: "فروشگاه آلفا",
    actionCodes: ["update", "add_item"],
    effectivelyPublic: false,
    ...over,
  };
}

function stubFetch(detail?: Record<string, unknown> | null) {
  return (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes("/v1/admin/stories/") || url.includes("/v1/seller/stories/")) {
      return new Response(JSON.stringify(detail ?? shopStory()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/admin/sellers")) {
      return new Response(
        JSON.stringify([{ sellerPartyId: SHOP_ID, displayName: "فروشگاه آلفا" }]),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("locales") || url.includes("languages")) {
      return new Response(
        JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("product")) {
      return new Response(JSON.stringify({ items: [], totalCount: 0 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
}

test("A: admin and seller /new open the full form; title-only modal is gone", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = stubFetch();
  window.localStorage.setItem("tooba.sellerPartyId", SHOP_ID);
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");

  const admin = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      navigate: () => undefined,
      createMode: true,
    }),
  );
  await flush();
  await waitFor(() => qq(admin.container, '[data-testid="story-create-workspace"]').length === 1);
  assert.equal(qq(admin.container, '[data-testid="story-create-title"]').length, 1);
  assert.equal(qq(admin.container, '[data-testid="story-slide-open-new"]').length, 1);
  assert.equal(qq(admin.container, '[data-testid="story-create-preview"]').length, 1);
  assert.match(admin.container.textContent ?? "", new RegExp(faStoryCopy.mainStorefront));
  assert.doesNotMatch(admin.container.textContent ?? "", /ایجاد پیش‌نویس/);
  assert.equal(qq(admin.container, '[data-testid="admin-story-review-filter"]').length, 0);
  await admin.unmount();

  const seller = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      navigate: () => undefined,
      createMode: true,
    }),
  );
  await flush();
  await waitFor(() => qq(seller.container, '[data-testid="story-create-workspace"]').length === 1);
  assert.equal(qq(seller.container, '[data-testid="story-seller-shop-fixed"]').length, 1);
  assert.equal(qq(seller.container, '[data-testid="story-admin-shop-picker"]').length, 0);
  assert.equal(qq(seller.container, '[data-testid="admin-story-review-filter"]').length, 0);
  globalThis.fetch = original;
  await seller.unmount();
});

test("B: review filter only on admin list; absent on seller list and both details", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = stubFetch();
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-1");
  const adminList = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      navigate: () => undefined,
    }),
  );
  await flush();
  assert.equal(qq(adminList.container, '[data-testid="admin-story-review-filter"]').length, 1);
  await adminList.unmount();

  const sellerList = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      navigate: () => undefined,
    }),
  );
  await flush();
  assert.equal(qq(sellerList.container, '[data-testid="admin-story-review-filter"]').length, 0);
  await sellerList.unmount();

  const detail = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      navigate: () => undefined,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => qq(detail.container, '[data-testid="story-detail-workspace"]').length === 1);
  assert.equal(qq(detail.container, '[data-testid="admin-story-review-filter"]').length, 0);
  globalThis.fetch = original;
  await detail.unmount();
});

test("B/C: platform vs shop location and internal destination stay merchant-labeled", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = stubFetch(
    shopStory({
      sellerPartyId: null,
      shopDisplayName: null,
      origin: "Admin",
      items: shopStory().items,
    }),
  );
  const platform = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      navigate: () => undefined,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => qq(platform.container, '[data-testid="story-detail-location"]').length === 1);
  assert.match(platform.container.textContent ?? "", new RegExp(faStoryCopy.platformStorefront));
  assert.match(platform.container.textContent ?? "", new RegExp(faStoryCopy.destinationUnavailable));
  assert.doesNotMatch(platform.container.textContent ?? "", /\binternal\b/);
  assert.doesNotMatch(platform.container.textContent ?? "", /Story/);
  await platform.unmount();

  globalThis.fetch = stubFetch(
    shopStory({
      sellerPartyId: SHOP_ID,
      shopDisplayName: null,
    }),
  );
  const missingName = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      navigate: () => undefined,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => (missingName.container.textContent ?? "").includes(faStoryCopy.shopNameUnavailable));
  globalThis.fetch = original;
  await missingName.unmount();
});
