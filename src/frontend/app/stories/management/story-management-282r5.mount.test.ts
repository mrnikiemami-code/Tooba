/**
 * TB-282-R5 F1/F2 — product label ownership + real update_item transport + grid constraints.
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
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { normalizeStoryColumnOrder, applyStoryListColumnConstraints } from "./story-column-order.ts";
import { runDurableStoryMutation, type StoryExecutorGate } from "./story-executor.ts";
import {
  clearStoryIntentIfCurrent,
  listPendingStoryIntents,
  storeStoryIntent,
  type StoryIntentEnvelope,
} from "./story-intent.ts";
import { faStoryCopy } from "./story-management-copy.ts";

ensureDom();

const PRODUCT_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001";
const PRODUCT_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0002";
const STORY_ID = "11111111-1111-1111-1111-111111111111";
const ITEM_A = "33333333-3333-3333-3333-333333333331";
const ITEM_B = "33333333-3333-3333-3333-333333333332";

function detailStory(overrides: Record<string, unknown> = {}) {
  return {
    storyId: STORY_ID,
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
    title: "استوری دو محصول",
    coverMediaAssetId: null,
    coverMediaUrl: null,
    displayOrder: 1,
    startAt: null,
    endAt: null,
    status: "Draft",
    ctaType: "none",
    ctaTarget: null,
    versionToken: 3,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    items: [
      {
        storyItemId: ITEM_A,
        displayOrder: 0,
        mediaType: "image",
        mediaAssetId: "55555555-5555-5555-5555-555555555551",
        mediaUrl: "https://cdn.example/a.jpg",
        caption: "کا",
        durationMs: 5000,
        ctaType: "product",
        ctaTarget: PRODUCT_A,
        ctaProductTitle: "محصول آلفای ذخیره",
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      {
        storyItemId: ITEM_B,
        displayOrder: 1,
        mediaType: "image",
        mediaAssetId: "55555555-5555-5555-5555-555555555552",
        mediaUrl: "https://cdn.example/b.jpg",
        caption: "کب",
        durationMs: 7000,
        ctaType: "product",
        ctaTarget: PRODUCT_B,
        ctaProductTitle: "محصول بتای ذخیره",
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ],
    id: STORY_ID,
    createdByActorUserId: null,
    shopDisplayName: "فروشگاه آلفا",
    contentReady: true,
    effectivelyPublic: false,
    actionCodes: ["update", "add_item", "submit", "update_item"],
    ...overrides,
  };
}

function stubDetailFetch(opts?: { productPageOnlyA?: boolean }) {
  const puts: Array<{ url: string; body: string }> = [];
  const fetchImpl = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "PUT" && url.includes("/items/")) {
      puts.push({ url, body: String(init?.body ?? "") });
      const body = JSON.parse(String(init?.body ?? "{}")) as Record<string, unknown>;
      const story = detailStory({
        versionToken: 4,
        items: detailStory().items.map((item) =>
          item.storyItemId === ITEM_A
            ? {
                ...item,
                caption: String(body.caption ?? item.caption),
                durationMs: Number(body.durationMs ?? item.durationMs),
                ctaType: String(body.ctaType ?? item.ctaType),
                ctaTarget: (body.ctaTarget as string | null) ?? item.ctaTarget,
                ctaProductTitle:
                  body.ctaTarget === PRODUCT_A
                    ? "محصول آلفای ذخیره"
                    : body.ctaTarget === PRODUCT_B
                      ? "محصول بتای ذخیره"
                      : null,
                mediaAssetId: item.mediaAssetId,
                mediaUrl: item.mediaUrl,
              }
            : item,
        ),
      });
      return new Response(JSON.stringify(story), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("/product-options")) {
      const onlyA = opts?.productPageOnlyA !== false;
      const items = onlyA
        ? [{ productId: PRODUCT_A, title: "محصول آلفای ذخیره" }]
        : [
            { productId: PRODUCT_A, title: "محصول آلفای ذخیره" },
            { productId: PRODUCT_B, title: "محصول بتای ذخیره" },
          ];
      return new Response(JSON.stringify({ items, totalCount: onlyA ? 55 : 2 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/query")) {
      return new Response(JSON.stringify({ items: [detailStory()], page: 1, pageSize: 50, totalCount: 1 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes(`/stories/${STORY_ID}`) && !url.includes("/items")) {
      return new Response(JSON.stringify(detailStory()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/admin/languages") || url.includes("/locale-options")) {
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
      return new Response(JSON.stringify([{ sellerPartyId: "22222222-2222-2222-2222-222222222222", displayName: "فروشگاه آلفا", status: "Active" }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
  return { fetchImpl, puts };
}

test("F1: two stored product slides keep own names after reload; picker A cancel does not rename B", async () => {
  window.localStorage.setItem("tooba.sellerPartyId", "22222222-2222-2222-2222-222222222222");
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  const { fetchImpl } = stubDetailFetch({ productPageOnlyA: true });
  const original = globalThis.fetch;
  globalThis.fetch = fetchImpl;
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      navigate: () => undefined,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-row"]').length === 2);
  const text = tree.container.textContent ?? "";
  assert.match(text, /محصول آلفای ذخیره/);
  assert.match(text, /محصول بتای ذخیره/);
  // B is outside current picker page (only A returned) yet still labelled from owning DTO.
  assert.doesNotMatch(text, /محصول انتخاب‌شده(?!‌)/);

  const edits = qq(tree.container, '[data-testid="story-slide-edit"]');
  assert.equal(edits.length, 2);
  edits[0]!.dispatchEvent(new MouseEvent("click", { bubbles: true }));
  await flush();
  await waitFor(() => (tree.container.textContent ?? "").includes(faStoryCopy.editSlideTitle));
  // Cancel without save.
  const cancel = Array.from(tree.container.querySelectorAll("button")).find((b) =>
    (b.textContent ?? "").includes(faStoryCopy.cancel),
  );
  assert.ok(cancel);
  cancel!.dispatchEvent(new MouseEvent("click", { bubbles: true }));
  await flush();
  const after = tree.container.textContent ?? "";
  assert.match(after, /محصول آلفای ذخیره/);
  assert.match(after, /محصول بتای ذخیره/);
  globalThis.fetch = original;
  await tree.unmount();
});

test("F1: unavailable product title stays explicit and does not borrow another slide label", async () => {
  window.localStorage.setItem("tooba.sellerPartyId", "22222222-2222-2222-2222-222222222222");
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  const story = detailStory({
    items: [
      {
        ...detailStory().items[0],
        ctaProductTitle: "محصول آلفای ذخیره",
      },
      {
        ...detailStory().items[1],
        ctaProductTitle: null,
        ctaTarget: "99999999-9999-9999-9999-999999999999",
      },
    ],
  });
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes(`/stories/${STORY_ID}`) && !url.includes("/items")) {
      return new Response(JSON.stringify(story), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("/product-options")) {
      return new Response(JSON.stringify({ items: [{ productId: PRODUCT_A, title: "محصول آلفای ذخیره" }], totalCount: 1 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/locale-options") || url.includes("/languages")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response(JSON.stringify({ items: [], page: 1, pageSize: 50, totalCount: 0 }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  }) as typeof fetch;
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      navigate: () => undefined,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-row"]').length === 2);
  const text = tree.container.textContent ?? "";
  assert.match(text, /محصول آلفای ذخیره/);
  assert.ok(text.includes(faStoryCopy.productUnknown), "unavailable product is explicit");
  assert.ok(text.includes("محصول آلفای ذخیره") && text.includes(faStoryCopy.productUnknown));
  globalThis.fetch = original;
  await tree.unmount();
});

async function runUpdateItemSave(role: "admin" | "seller") {
  const caps = role === "admin" ? ADMIN_STORY_CAPABILITIES : SELLER_STORY_CAPABILITIES;
  if (role === "seller") {
    window.localStorage.setItem("tooba.sellerPartyId", "22222222-2222-2222-2222-222222222222");
    window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-1");
  } else {
    window.localStorage.setItem("tooba.adminActorUserId", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  }
  const { fetchImpl, puts } = stubDetailFetch({ productPageOnlyA: false });
  const original = globalThis.fetch;
  globalThis.fetch = fetchImpl;
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: caps,
      navigate: () => undefined,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-slide-edit"]').length >= 1);
  await click(qq(tree.container, '[data-testid="story-slide-edit"]')[0]!);
  await waitFor(() => q(tree.container, '[data-testid="story-slide-save"]') != null);
  await setInputValue(q(tree.container, '[data-testid="story-slide-caption"]'), "زیرنویس تازه");
  await setInputValue(q(tree.container, '[data-testid="story-slide-duration"]'), "9");
  await setSelectValue(q(tree.container, '[data-testid="story-slide-cta-type"]'), "product");
  await waitFor(() => qq(tree.container, '[data-testid="story-product-option"]').length >= 1);
  const productB = qq(tree.container, '[data-testid="story-product-option"]').find(
    (el) => el.getAttribute("data-product-id") === PRODUCT_B,
  );
  assert.ok(productB, "product B option present");
  await click(productB!);
  await click(q(tree.container, '[data-testid="story-slide-save"]'));
  await waitFor(() => puts.length >= 1);
  assert.equal(puts.length, 1);
  assert.match(puts[0]!.url, new RegExp(`/v1/${role}/stories/${STORY_ID}/items/${ITEM_A}$`));
  const body = JSON.parse(puts[0]!.body) as Record<string, unknown>;
  assert.equal(body.mediaAssetId, "55555555-5555-5555-5555-555555555551");
  assert.equal(body.mediaUrl, "https://cdn.example/a.jpg");
  assert.equal(body.expectedVersion, 3);
  assert.equal(body.ctaTarget, PRODUCT_B);
  assert.equal(body.caption, "زیرنویس تازه");
  assert.equal(body.durationMs, 9000);
  assert.equal(typeof body.occurrenceKey, "string");
  assert.ok(String(body.occurrenceKey).length > 0);
  // cancel path: reopen and cancel → zero additional puts
  const before = puts.length;
  await click(qq(tree.container, '[data-testid="story-slide-edit"]')[0]!);
  await flush();
  const cancel = Array.from(tree.container.querySelectorAll("button")).find((b) =>
    (b.textContent ?? "").includes(faStoryCopy.cancel),
  );
  assert.ok(cancel);
  await click(cancel!);
  assert.equal(puts.length, before);
  globalThis.fetch = original;
  await tree.unmount();
  return body;
}

test("F2: seller edit existing slide sends one owning PUT retaining media/version/key", async () => {
  await runUpdateItemSave("seller");
});

test("F2: admin edit existing slide sends one owning PUT retaining media/version/key", async () => {
  await runUpdateItemSave("admin");
});

test("F2: empty/non-JSON/wrong-story 200 keeps envelope; genuine replay clears", async () => {
  window.sessionStorage.clear();
  const envelope: StoryIntentEnvelope = {
    version: 2,
    action: "update_item",
    occurrenceKey: "occ-r5-update-1",
    method: "PUT",
    path: `/v1/seller/stories/${STORY_ID}/items/${ITEM_A}`,
    routeTargetId: STORY_ID,
    expectedVersion: 3,
    bodyText: JSON.stringify({
      mediaType: "image",
      mediaAssetId: "55555555-5555-5555-5555-555555555551",
      mediaUrl: "https://cdn.example/a.jpg",
      caption: "x",
      durationMs: 5000,
      ctaType: "product",
      ctaTarget: PRODUCT_A,
      expectedVersion: 3,
      occurrenceKey: "occ-r5-update-1",
    }),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "seller",
    shopId: "22222222-2222-2222-2222-222222222222",
    actorUserId: "actor-seller-1",
    recordedAt: new Date().toISOString(),
  };
  assert.equal(storeStoryIntent(envelope), true);
  const scope = {
    role: "seller" as const,
    shopId: "22222222-2222-2222-2222-222222222222",
    actorUserId: "actor-seller-1",
  };
  const gate: StoryExecutorGate = { inFlightRef: { current: false }, epochRef: { current: 1 } };
  const original = globalThis.fetch;
  let step = 0;
  globalThis.fetch = (async () => {
    step += 1;
    if (step === 1) return new Response("", { status: 200 });
    if (step === 2) return new Response("{not-json", { status: 200 });
    if (step === 3)
      return new Response(JSON.stringify({ storyId: "00000000-0000-0000-0000-000000000099", versionToken: 9 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    return new Response(
      JSON.stringify({
        ...detailStory(),
        storyId: STORY_ID,
        versionToken: 4,
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  }) as typeof fetch;

  const empty = await runDurableStoryMutation({ gate, envelope, scope });
  assert.equal(empty.state, "failed");
  assert.equal(empty.state === "failed" && empty.code, "story.response.unverified");
  const retained1 = listPendingStoryIntents("seller", scope.shopId, scope.actorUserId).find(
    (row) => row.state === "ok" && row.envelope.occurrenceKey === envelope.occurrenceKey,
  );
  assert.ok(retained1 && retained1.state === "ok");
  assert.equal(retained1.envelope.bodyText, envelope.bodyText);

  const badJson = await runDurableStoryMutation({ gate, envelope, scope });
  assert.equal(badJson.state, "failed");
  const retained2 = listPendingStoryIntents("seller", scope.shopId, scope.actorUserId).find(
    (row) => row.state === "ok" && row.envelope.occurrenceKey === envelope.occurrenceKey,
  );
  assert.ok(retained2 && retained2.state === "ok");

  const wrongStory = await runDurableStoryMutation({ gate, envelope, scope });
  assert.equal(wrongStory.state, "failed");
  const retained3 = listPendingStoryIntents("seller", scope.shopId, scope.actorUserId).find(
    (row) => row.state === "ok" && row.envelope.occurrenceKey === envelope.occurrenceKey,
  );
  assert.ok(retained3 && retained3.state === "ok");

  const ok = await runDurableStoryMutation({ gate, envelope, scope });
  assert.equal(ok.state, "ok");
  clearStoryIntentIfCurrent(envelope);
  const cleared = listPendingStoryIntents("seller", scope.shopId, scope.actorUserId).find(
    (row) => row.state === "ok" && row.envelope.occurrenceKey === envelope.occurrenceKey,
  );
  assert.equal(cleared, undefined);
  globalThis.fetch = original;
});

test("A: old saved column order remaps title-first actions-last without wiping filters", () => {
  const constrained = applyStoryListColumnConstraints({
    columnOrder: ["actions", "shopDisplayName", "updatedAt", "title"],
    pinned: { title: "right", actions: "left", shopDisplayName: "left" },
  });
  assert.deepEqual(constrained.columnOrder?.[0], "title");
  assert.deepEqual(constrained.columnOrder?.at(-1), "actions");
  assert.equal(constrained.pinned?.title, undefined);
  assert.equal(constrained.pinned?.actions, undefined);
  assert.equal(constrained.pinned?.shopDisplayName, "left");
  assert.deepEqual(normalizeStoryColumnOrder(["actions", "title"]), ["title", "actions"]);
});
