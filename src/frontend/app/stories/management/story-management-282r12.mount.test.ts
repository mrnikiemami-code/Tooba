/**
 * TB-282-R12 — publication window header no-end + stable end column (create+edit).
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
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { parseOptionalPublicationWindow } from "./story-schedule-time.ts";

ensureDom();

const STORY_ID = "01a04211-7dd7-7000-83dc-5c79db49abc4";

function storyPayload() {
  return {
    id: STORY_ID,
    storyId: STORY_ID,
    tenantId: "store-alpha",
    origin: "Admin",
    reviewStatus: "None",
    sellerPartyId: null,
    title: "بازه انتشار",
    status: "Draft",
    versionToken: 2,
    locale: "fa-IR",
    market: null,
    coverMediaAssetId: null,
    coverMediaUrl: null,
    displayOrder: 0,
    ctaType: "none",
    ctaTarget: null,
    startAt: "2026-11-20T06:30:00.000Z",
    endAt: "2026-11-25T06:30:00.000Z",
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    createdByActorUserId: null,
    reviewedByActorUserId: null,
    items: [],
    shopDisplayName: null,
    actionCodes: ["update", "add_item", "delete"],
    effectivelyPublic: false,
    contentReady: false,
  };
}

test("R12: requireEnd semantics preserved with no-end toggle", () => {
  const draft = { startDayIso: "2026-11-20", startTime: "10:00", endDayIso: "2026-11-25", endTime: "18:00" };
  const withEnd = parseOptionalPublicationWindow(draft, { requireEnd: true });
  assert.equal(withEnd.ok, true);
  const cleared = parseOptionalPublicationWindow(
    { ...draft, endDayIso: "", endTime: "" },
    { requireEnd: false },
  );
  assert.equal(cleared.ok, true);
  if (cleared.ok) assert.equal(cleared.endAt, null);
  const refused = parseOptionalPublicationWindow(
    { ...draft, endDayIso: "", endTime: "" },
    { requireEnd: true },
  );
  assert.equal(refused.ok, false);
});

test("R12: create form keeps end fields mounted; checkbox outside columns; values retained", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes("/query") || url.includes("/stories?")) {
      return new Response(JSON.stringify({ items: [], page: 1, pageSize: 50, totalCount: 0 }), {
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
    if (url.includes("/v1/admin/sellers")) {
      return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-r12");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      createMode: true,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-create-publication-window"]').length === 1);

  const section = q(tree.container, '[data-testid="story-create-publication-window"]');
  assert.equal(qq(section, '[data-testid="story-create-window-no-end"]').length, 1);
  assert.equal(qq(section, '[data-testid="story-create-window-end-day"]').length, 1);
  assert.equal(qq(section, '[data-testid="story-create-window-end-time"]').length, 1);

  const endTime = q(section, '[data-testid="story-create-window-end-time"]') as HTMLInputElement;
  assert.equal(endTime.disabled, true);

  // Nested label around checkbox must not exist (checkbox has dedicated label via htmlFor).
  const nested = section.querySelector('label label');
  assert.equal(nested, null);

  const endHeader = q(section, '[data-testid="story-create-window-end-header"]');
  assert.ok(endHeader.contains(q(section, '[data-testid="story-create-window-no-end"]')));
  const legend = section.querySelector("legend");
  assert.ok(legend);
  assert.equal(legend.querySelector('[data-testid="story-create-window-no-end"]'), null);

  const noEnd = q(section, '[data-testid="story-create-window-no-end"]') as HTMLInputElement;
  assert.equal(noEnd.checked, true);
  await click(noEnd);
  await flush();
  assert.equal(noEnd.checked, false);
  assert.equal(endTime.disabled, false);

  await setInputValue(endTime, "15:30");
  await flush();
  assert.equal((q(section, '[data-testid="story-create-window-end-time"]') as HTMLInputElement).value, "15:30");

  await click(noEnd);
  await flush();
  assert.equal(noEnd.checked, true);
  const endAfter = q(section, '[data-testid="story-create-window-end-time"]') as HTMLInputElement;
  assert.equal(endAfter.disabled, true);
  assert.equal(endAfter.value, "15:30", "no-end must not erase end clock");

  globalThis.fetch = original;
  await tree.unmount();
});

test("R12: edit form shares header no-end layout and retains end values", async () => {
  const original = globalThis.fetch;
  const story = storyPayload();
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes(`/stories/${STORY_ID}`) && !url.includes("/items")) {
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

  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-r12");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: STORY_ID,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-edit-publication-window"]').length === 1);
  const section = q(tree.container, '[data-testid="story-edit-publication-window"]');
  const noEnd = q(section, '[data-testid="story-edit-window-no-end"]') as HTMLInputElement;
  const endTime = q(section, '[data-testid="story-edit-window-end-time"]') as HTMLInputElement;
  assert.ok(endTime);
  // Loaded with endAt → no-end unchecked
  assert.equal(noEnd.checked, false);
  assert.equal(endTime.disabled, false);
  const before = endTime.value;
  await click(noEnd);
  await flush();
  assert.equal(noEnd.checked, true);
  assert.equal(endTime.disabled, true);
  assert.equal(endTime.value, before);
  await click(noEnd);
  await flush();
  assert.equal(endTime.disabled, false);
  assert.equal(endTime.value, before);

  globalThis.fetch = original;
  await tree.unmount();
});
