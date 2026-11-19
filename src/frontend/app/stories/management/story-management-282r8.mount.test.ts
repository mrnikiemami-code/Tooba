/**
 * TB-282-R8 A — unknown create → no second create while unresolved → recovery retry
 * adopts the same occurrence response and binds the recovered draft (admin + seller).
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
  waitFor,
  type MountedTree,
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { listPendingStoryIntents } from "./story-intent.ts";

ensureDom();

const STORY_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0008";
const SHOP_ID = "22222222-2222-2222-2222-222222222222";

type CreateCall = { occurrenceKey: string; bodyText: string };

function installCreateFetch(state: {
  mode: "unknown" | "ok" | "malformed";
  creates: CreateCall[];
  draftTitle: string;
}) {
  return (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST" && (url.endsWith("/v1/admin/stories") || url.endsWith("/v1/seller/stories"))) {
      const bodyText = String(init?.body ?? "");
      const parsed = JSON.parse(bodyText) as { occurrenceKey?: string; title?: string };
      state.creates.push({ occurrenceKey: String(parsed.occurrenceKey ?? ""), bodyText });
      if (state.mode === "unknown") {
        return new Response("", { status: 200, headers: { "Content-Type": "application/json" } });
      }
      if (state.mode === "malformed") {
        return new Response(JSON.stringify({ ok: true }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }
      state.draftTitle = String(parsed.title ?? state.draftTitle);
      return new Response(JSON.stringify({ storyId: STORY_ID, data: { storyId: STORY_ID } }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes(`/stories/${STORY_ID}`) && method === "GET") {
      return new Response(
        JSON.stringify({
          storyId: STORY_ID,
          title: state.draftTitle || "عنوان بازیابی",
          status: "Draft",
          versionToken: 1,
          origin: "Admin",
          reviewStatus: "None",
          sellerPartyId: null,
          shopDisplayName: null,
          items: [],
          actionCodes: ["update", "add_item"],
          effectivelyPublic: false,
          locale: "fa-IR",
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("locales") || url.includes("languages") || url.includes("locale-options")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/admin/sellers")) {
      return new Response(
        JSON.stringify([{ sellerPartyId: SHOP_ID, displayName: "فروشگاه آلفا", status: "Active" }]),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    if (url.includes("product")) {
      return new Response(JSON.stringify({ items: [], totalCount: 0 }), {
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

async function recoveredCreateFlow(role: "admin" | "seller"): Promise<void> {
  const original = globalThis.fetch;
  const actor = role === "admin" ? "actor-admin-8" : "actor-seller-8";
  const shopId = role === "seller" ? SHOP_ID : null;
  window.localStorage.setItem("tooba.adminActorUserId", actor);
  window.localStorage.setItem("tooba.sellerActorUserId", actor);
  window.localStorage.setItem("tooba.sellerPartyId", SHOP_ID);
  sessionStorage.clear();

  const state = { mode: "unknown" as "unknown" | "ok" | "malformed", creates: [] as CreateCall[], draftTitle: "" };
  globalThis.fetch = installCreateFetch(state);

  let href = role === "seller" ? "/vendor-panel/stories/new" : "/admin/stories/new";
  let tree: MountedTree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: role === "seller" ? SELLER_STORY_CAPABILITIES : ADMIN_STORY_CAPABILITIES,
      createMode: true,
      navigate: (next) => {
        href = next;
      },
    }),
  );
  await flush();
  await setInputValue(q(tree.container, '[data-testid="story-create-title"]'), "عنوان بازیابی");
  await click(q(tree.container, '[data-testid="story-create-save"]'));
  await flush();
  await waitFor(() => state.creates.length === 1);
  const firstKey = state.creates[0]!.occurrenceKey;
  const firstBody = state.creates[0]!.bodyText;
  assert.equal(qq(tree.container, '[data-testid="story-create-workspace"]').length, 1);
  assert.equal(href.endsWith("/new") || href.includes("/new"), true);

  const pending = listPendingStoryIntents(role, shopId, actor);
  assert.ok(pending.some((row) => row.state === "ok" && row.envelope.action === "create"));

  // While unresolved, Save/Add must not mint a fresh create occurrence.
  await click(q(tree.container, '[data-testid="story-create-save"]'));
  await flush();
  await click(q(tree.container, '[data-testid="story-slide-open-new"]'));
  await flush();
  assert.equal(state.creates.length, 1);
  assert.equal(state.creates[0]!.occurrenceKey, firstKey);

  // Genuine replay of the SAME frozen occurrence.
  state.mode = "ok";
  await waitFor(() => qq(tree.container, '[data-testid="story-recovery-retry"]').length === 1);
  await click(q(tree.container, '[data-testid="story-recovery-retry"]'));
  await flush();
  await waitFor(() => state.creates.length === 2);
  assert.equal(state.creates[1]!.occurrenceKey, firstKey);
  assert.equal(state.creates[1]!.bodyText, firstBody);
  assert.match(href, new RegExp(`${STORY_ID}$`));

  await tree.unmount();
  tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: role === "seller" ? SELLER_STORY_CAPABILITIES : ADMIN_STORY_CAPABILITIES,
      initialStoryId: STORY_ID,
      navigate: (next) => {
        href = next;
      },
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-detail-workspace"]').length === 1);
  // Reuse recovered id: Add must not create again.
  const createsBeforeAdd = state.creates.length;
  await click(q(tree.container, '[data-testid="story-slide-open-new"]'));
  await flush();
  assert.equal(state.creates.length, createsBeforeAdd);

  // No fresh pending create remains after successful replay.
  const after = listPendingStoryIntents(role, shopId, actor).filter(
    (row) => row.state === "ok" && row.envelope.action === "create",
  );
  assert.equal(after.length, 0);

  globalThis.fetch = original;
  await tree.unmount();
}

test("A: admin unknown create blocks fresh create then recovery adopts bound draft", async () => {
  await recoveredCreateFlow("admin");
});

test("A: seller unknown create blocks fresh create then recovery adopts bound draft", async () => {
  await recoveredCreateFlow("seller");
});

test("A: malformed recovery response retains pending and does not navigate", async () => {
  const original = globalThis.fetch;
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-8");
  sessionStorage.clear();
  const state = { mode: "unknown" as "unknown" | "ok" | "malformed", creates: [] as CreateCall[], draftTitle: "" };
  globalThis.fetch = installCreateFetch(state);
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
  await setInputValue(q(tree.container, '[data-testid="story-create-title"]'), "خراب");
  await click(q(tree.container, '[data-testid="story-create-save"]'));
  await flush();
  await waitFor(() => state.creates.length === 1);
  state.mode = "malformed";
  await click(q(tree.container, '[data-testid="story-recovery-retry"]'));
  await flush();
  await waitFor(() => state.creates.length === 2);
  assert.equal(qq(tree.container, '[data-testid="story-create-workspace"]').length, 1);
  assert.ok(href.includes("/new"));
  const pending = listPendingStoryIntents("admin", null, "actor-admin-8");
  assert.ok(pending.some((row) => row.state === "ok" && row.envelope.action === "create"));
  globalThis.fetch = original;
  await tree.unmount();
});
