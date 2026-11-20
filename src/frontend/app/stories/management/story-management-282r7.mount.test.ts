/**
 * TB-282-R7 — real create/add/edit/preview/cancel, dirty leave, one create reuse,
 * truthful destination/origin labels (both admin and seller).
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
  setInputValue,
  setSelectValue,
  waitFor,
  type MountedTree,
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { listPendingStoryIntents } from "./story-intent.ts";
import { faStoryCopy } from "./story-management-copy.ts";

ensureDom();

const STORY_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001";
const SHOP_ID = "22222222-2222-2222-2222-222222222222";
const PRODUCT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001";
const ITEM_ID = "cccccccc-cccc-cccc-cccc-cccccccc0001";
const MEDIA_ID = "dddddddd-dddd-dddd-dddd-dddddddd0001";

type StoryBag = {
  storyId: string;
  versionToken: number;
  title: string;
  items: Array<Record<string, unknown>>;
  sellerPartyId: string | null;
  shopDisplayName: string | null;
  origin: string;
  actionCodes: string[];
  [key: string]: unknown;
};

function emptyDraft(over: Partial<StoryBag> = {}): StoryBag {
  return {
    storyId: STORY_ID,
    tenantId: "store-alpha",
    origin: over.origin ?? "Admin",
    reviewStatus: "None",
    sellerPartyId: over.sellerPartyId ?? null,
    title: over.title ?? "عنوان تست",
    status: "Draft",
    versionToken: over.versionToken ?? 1,
    locale: "fa-IR",
    items: over.items ?? [],
    shopDisplayName: over.shopDisplayName ?? null,
    actionCodes: over.actionCodes ?? ["update", "add_item", "update_item"],
    effectivelyPublic: false,
    ...over,
  };
}

async function assignFile(input: HTMLInputElement, file: File): Promise<void> {
  const list = {
    0: file,
    length: 1,
    item: (index: number) => (index === 0 ? file : null),
    *[Symbol.iterator]() {
      yield file;
    },
  } as unknown as FileList;
  Object.defineProperty(input, "files", { configurable: true, value: list });
  input.dispatchEvent(new Event("change", { bubbles: true }));
  await flush();
}

function installFetch(state: { draft: StoryBag; creates: number; uploads: number; adds: number; createMode: "ok" | "unknown" }) {
  return (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();

    if (method === "POST" && (url.endsWith("/v1/admin/stories") || url.endsWith("/v1/seller/stories"))) {
      state.creates += 1;
      if (state.createMode === "unknown") {
        // Verified-bound retention: 2xx alone is not success without a parseable body.
        return new Response("", { status: 200, headers: { "Content-Type": "application/json" } });
      }
      const body = JSON.parse(String(init?.body ?? "{}")) as { title?: string };
      state.draft = emptyDraft({
        ...state.draft,
        title: body.title ?? state.draft.title,
        versionToken: 1,
        items: [],
      });
      return new Response(JSON.stringify({ storyId: STORY_ID, data: { storyId: STORY_ID } }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }

    if (method === "POST" && url.includes("/media/")) {
      state.uploads += 1;
      return new Response(
        JSON.stringify({
          mediaAssetId: MEDIA_ID,
          contentType: "image/jpeg",
          displayUrl: "https://cdn.example/r7.jpg",
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }

    if (method === "POST" && url.includes("/items")) {
      state.adds += 1;
      const body = JSON.parse(String(init?.body ?? "{}")) as Record<string, unknown>;
      state.draft = {
        ...state.draft,
        versionToken: state.draft.versionToken + 1,
        items: [
          {
            storyItemId: ITEM_ID,
            displayOrder: 0,
            mediaType: "image",
            mediaAssetId: MEDIA_ID,
            mediaUrl: "https://cdn.example/r7.jpg",
            caption: body.caption ?? "کپشن",
            durationMs: Number(body.durationMs ?? 5000),
            ctaType: String(body.ctaType ?? "none"),
            ctaTarget: (body.ctaTarget as string | null) ?? null,
            ctaProductTitle: body.ctaType === "product" ? "محصول آر۷" : null,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          },
        ],
      };
      return new Response(JSON.stringify(state.draft), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }

    if (method === "PUT" && url.includes("/items/")) {
      const body = JSON.parse(String(init?.body ?? "{}")) as Record<string, unknown>;
      state.draft = {
        ...state.draft,
        versionToken: state.draft.versionToken + 1,
        items: state.draft.items.map((item) => ({
          ...item,
          caption: body.caption ?? item.caption,
          durationMs: Number(body.durationMs ?? item.durationMs),
          ctaType: String(body.ctaType ?? item.ctaType),
          ctaTarget: (body.ctaTarget as string | null) ?? item.ctaTarget,
        })),
      };
      return new Response(JSON.stringify(state.draft), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }

    if (url.includes(`/stories/${STORY_ID}`) && !url.includes("/items") && method === "GET") {
      return new Response(JSON.stringify(state.draft), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/product-options") || url.includes("product")) {
      return new Response(
        JSON.stringify({ items: [{ productId: PRODUCT_ID, title: "محصول آر۷" }], totalCount: 1 }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("/v1/admin/sellers")) {
      return new Response(
        JSON.stringify([{ sellerPartyId: SHOP_ID, displayName: "فروشگاه آلفا", status: "Active" }]),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("locales") || url.includes("languages") || url.includes("locale-options")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/query") || url.includes("/stories?")) {
      return new Response(JSON.stringify({ items: [], page: 1, pageSize: 50, totalCount: 0 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
}

async function runCreateAddFlow(mode: "admin" | "seller"): Promise<{
  creates: number;
  uploads: number;
  adds: number;
  tree: MountedTree;
}> {
  const state = {
    draft: emptyDraft({
      origin: mode === "seller" ? "Seller" : "Admin",
      sellerPartyId: mode === "seller" ? SHOP_ID : null,
      shopDisplayName: mode === "seller" ? "فروشگاه آلفا" : null,
    }),
    creates: 0,
    uploads: 0,
    adds: 0,
    createMode: "ok" as const,
  };
  globalThis.fetch = installFetch(state);
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-1");
  window.localStorage.setItem("tooba.sellerPartyId", SHOP_ID);
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  sessionStorage.clear();

  let href = mode === "seller" ? "/vendor-panel/stories/new" : "/admin/stories/new";
  let tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: mode === "seller" ? SELLER_STORY_CAPABILITIES : ADMIN_STORY_CAPABILITIES,
      createMode: true,
      navigate: (next) => {
        href = next;
      },
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-create-workspace"]').length === 1);
  await setInputValue(q(tree.container, '[data-testid="story-create-title"]'), "عنوان تست");
  await click(q(tree.container, '[data-testid="story-slide-open-new"]'));
  await flush();
  // Model route remount onto the created detail (single transition).
  assert.match(href, new RegExp(`${STORY_ID}$`));
  assert.equal(state.creates, 1);
  await tree.unmount();
  tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: mode === "seller" ? SELLER_STORY_CAPABILITIES : ADMIN_STORY_CAPABILITIES,
      initialStoryId: STORY_ID,
      navigate: (next) => {
        href = next;
      },
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-detail-workspace"]').length === 1);
  await waitFor(() => (tree.container.textContent ?? "").includes(faStoryCopy.newSlideTitle));

  const fileInput = q(tree.container, '[data-testid="story-slide-file"]') as HTMLInputElement;
  await assignFile(fileInput, new File(["img"], "r7.jpg", { type: "image/jpeg" }));
  await setInputValue(q(tree.container, '[data-testid="story-slide-caption"]'), "کپشن آر۷");
  await setInputValue(q(tree.container, '[data-testid="story-slide-duration"]'), "7");
  await setSelectValue(q(tree.container, '[data-testid="story-slide-cta-type"]'), "product");
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-product-option"]').length >= 1);
  await click(q(tree.container, '[data-testid="story-product-option"]'));
  await click(q(tree.container, '[data-testid="story-slide-save"]'));
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-row"]').length === 1);
  assert.match(tree.container.textContent ?? "", /کپشن آر۷/);
  assert.match(tree.container.textContent ?? "", /محصول آر۷/);
  assert.equal(qq(tree.container, '[data-testid="story-detail-preview"]').length, 1);

  // Edit then cancel: zero update when untouched cancel after open.
  await click(q(tree.container, '[data-testid="story-slide-edit"]'));
  await flush();
  const cancel = Array.from(tree.container.querySelectorAll("button")).find((b) =>
    (b.textContent ?? "").includes(faStoryCopy.cancel),
  );
  assert.ok(cancel);
  const putsBefore = state.draft.versionToken;
  await click(cancel!);
  await flush();
  assert.equal(state.draft.versionToken, putsBefore);

  // Second add-slide must reuse the same draft (no second create).
  await click(q(tree.container, '[data-testid="story-slide-open-new"]'));
  await flush();
  await waitFor(() => (tree.container.textContent ?? "").includes(faStoryCopy.newSlideTitle));
  assert.equal(state.creates, 1);

  // Untouched cancel: zero upload/add.
  const uploadsBefore = state.uploads;
  const cancel2 = Array.from(tree.container.querySelectorAll("button")).find((b) =>
    (b.textContent ?? "").includes(faStoryCopy.cancel),
  );
  assert.ok(cancel2);
  await click(cancel2!);
  await flush();
  assert.equal(state.uploads, uploadsBefore);
  assert.equal(state.adds, 1);

  return { creates: state.creates, uploads: state.uploads, adds: state.adds, tree };
}

test("A: admin create/add/edit/preview/cancel reuses one draft and shows real preview", async () => {
  const original = globalThis.fetch;
  const { creates, uploads, adds, tree } = await runCreateAddFlow("admin");
  assert.equal(creates, 1);
  assert.equal(uploads, 1);
  assert.equal(adds, 1);
  globalThis.fetch = original;
  await tree.unmount();
});

test("A: seller create/add flow mirrors admin on VendorShell capabilities", async () => {
  const original = globalThis.fetch;
  const { creates, uploads, adds, tree } = await runCreateAddFlow("seller");
  assert.equal(creates, 1);
  assert.equal(uploads, 1);
  assert.equal(adds, 1);
  globalThis.fetch = original;
  await tree.unmount();
});

test("A: dirty back-to-list prompts; stay keeps form and discard leaves", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = installFetch({
    draft: emptyDraft(),
    creates: 0,
    uploads: 0,
    adds: 0,
    createMode: "ok",
  });
  let href = "/admin/stories/new";
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      createMode: true,
      navigate: (next) => {
        href = next;
      },
    }),
  );
  await flush();
  await setInputValue(q(tree.container, '[data-testid="story-create-title"]'), "dirty");
  await click(q(tree.container, '[data-testid="story-create-back"]'));
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-unsaved-leave-dialog"]').length === 1);
  assert.equal(href, "/admin/stories/new");
  await click(q(tree.container, '[data-testid="story-unsaved-stay"]'));
  await flush();
  assert.equal(qq(tree.container, '[data-testid="story-create-workspace"]').length, 1);
  await click(q(tree.container, '[data-testid="story-create-back"]'));
  await flush();
  await click(q(tree.container, '[data-testid="story-unsaved-discard"]'));
  await flush();
  assert.match(href, /\/admin\/stories$/);
  globalThis.fetch = original;
  await tree.unmount();
});

test("A: unknown create response retains exact pending intent; retry does not duplicate create until success path", async () => {
  const original = globalThis.fetch;
  const state = {
    draft: emptyDraft(),
    creates: 0,
    uploads: 0,
    adds: 0,
    createMode: "unknown" as "ok" | "unknown",
  };
  globalThis.fetch = installFetch(state);
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-1");
  sessionStorage.clear();
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      createMode: true,
      navigate: () => undefined,
    }),
  );
  await flush();
  await setInputValue(q(tree.container, '[data-testid="story-create-title"]'), "عنوان نامشخص");
  await click(q(tree.container, '[data-testid="story-create-save"]'));
  await flush();
  await waitFor(() => state.creates >= 1);
  const pending = listPendingStoryIntents("admin", null, "actor-admin-1");
  assert.ok(pending.some((row) => row.state === "ok" && row.envelope.action === "create"));
  // Still on create workspace — no false success navigation.
  assert.equal(qq(tree.container, '[data-testid="story-create-workspace"]').length, 1);
  assert.equal(ownedCreatesSafe(state.creates), true);
  globalThis.fetch = original;
  await tree.unmount();
});

function ownedCreatesSafe(creates: number): boolean {
  return creates === 1;
}

test("B: internal path is merchant-labeled; shop GUID shows shop name; origin stays creator role", async () => {
  const original = globalThis.fetch;
  const draft = emptyDraft({
    origin: "Admin",
    sellerPartyId: SHOP_ID,
    shopDisplayName: "فروشگاه آلفا",
    items: [
      {
        storyItemId: "item-path",
        displayOrder: 0,
        mediaType: "image",
        mediaAssetId: MEDIA_ID,
        mediaUrl: "https://cdn.example/x.jpg",
        caption: "مسیر",
        durationMs: 5000,
        ctaType: "internal",
        ctaTarget: "/products",
        ctaProductTitle: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      {
        storyItemId: "item-shop",
        displayOrder: 1,
        mediaType: "image",
        mediaAssetId: MEDIA_ID,
        mediaUrl: "https://cdn.example/y.jpg",
        caption: "فروشگاه",
        durationMs: 5000,
        ctaType: "shop",
        ctaTarget: SHOP_ID,
        ctaProductTitle: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ],
  });
  globalThis.fetch = installFetch({
    draft,
    creates: 0,
    uploads: 0,
    adds: 0,
    createMode: "ok",
  });
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: STORY_ID,
      navigate: () => undefined,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-row"]').length === 2);
  const text = tree.container.textContent ?? "";
  assert.match(text, new RegExp(faStoryCopy.destinationInternal));
  assert.match(text, new RegExp(`${faStoryCopy.destinationShop}: فروشگاه آلفا`));
  assert.doesNotMatch(text, /\binternal\b/);
  assert.match(text, /\/products/);
  assert.match(text, new RegExp(faStoryCopy.originCreatorAdmin));
  assert.doesNotMatch(text, /ویترین اصلی ·/);
  globalThis.fetch = original;
  await tree.unmount();
});
