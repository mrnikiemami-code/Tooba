/**
 * TB-282-R15 — rejected fetch (connection loss) must retain bulk result Dialog + exact retry.
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
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { listPendingStoryIntents } from "./story-intent.ts";
import { classifyAdminOp, executeAdminOpBatch } from "./story-admin-ops.ts";
import type { AdminStorySnapshot } from "../story-api.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";

ensureDom();

const ID_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa15a1";
const ID_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb15b2";
const ID_C = "cccccccc-cccc-cccc-cccc-cccccccc15c3";
const SHOP = "22222222-2222-2222-2222-222222222222";

function story(over: Partial<AdminStorySnapshot> & { storyId: string; title: string }): AdminStorySnapshot {
  const now = new Date().toISOString();
  return {
    id: over.storyId,
    tenantId: "store-alpha",
    origin: "Seller",
    reviewStatus: "Submitted",
    sellerPartyId: SHOP,
    status: "Draft",
    versionToken: 4,
    locale: "fa-IR",
    market: null,
    coverMediaAssetId: null,
    coverMediaUrl: null,
    displayOrder: 0,
    ctaType: "none",
    ctaTarget: null,
    startAt: null,
    endAt: null,
    createdAt: now,
    updatedAt: now,
    createdByActorUserId: null,
    reviewedByActorUserId: null,
    items: [],
    shopDisplayName: "فروشگاه",
    actionCodes: ["approve", "reject", "delete", "update", "add_item"],
    effectivelyPublic: false,
    contentReady: true,
    rejectionReason: null,
    submittedAt: now,
    reviewedAt: null,
    submittedByActorUserId: null,
    ...over,
  };
}

async function selectAgRows(container: HTMLElement, count: number) {
  await waitFor(() => qq(container, ".ag-row").length >= count);
  await waitFor(() => {
    const boxes = qq(container, '.ag-selection-checkbox input[type="checkbox"], .ag-row input[type="checkbox"]');
    return boxes.length >= count;
  });
  const boxes = qq(
    container,
    '.ag-selection-checkbox input[type="checkbox"], .ag-row input[type="checkbox"]',
  ).filter((el) => !(el as HTMLInputElement).closest(".ag-header-row"));
  assert.ok(boxes.length >= count, `need ${count} row checkboxes, got ${boxes.length}`);
  for (let i = 0; i < count; i += 1) {
    await click(boxes[i]!);
    await flush();
  }
  await waitFor(() => qq(container, '[data-testid="app-grid-selected-count"]').length >= 1);
}

test("R15: rejected fetch on second row keeps result Dialog; recovery exact bytes", async () => {
  const a = story({ storyId: ID_A, title: "ok-net", versionToken: 15 });
  const b = story({ storyId: ID_B, title: "lost-net", versionToken: 16 });
  const c = story({
    storyId: ID_C,
    title: "skip-net",
    reviewStatus: "Approved",
    versionToken: 17,
    actionCodes: ["delete", "schedule"],
  });
  const rows = [a, b, c];
  const bodies: string[] = [];
  let mode: "reject" | "ok" = "reject";
  const original = globalThis.fetch;
  let unhandled = 0;
  const onUnhandled = () => {
    unhandled += 1;
  };
  process.on("unhandledRejection", onUnhandled);

  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = String(init?.body ?? "");
    if (method === "POST" && /\/approve\/?$/.test(url) && !url.includes("approve-and")) {
      bodies.push(body);
      if (url.includes(ID_A)) {
        return new Response(JSON.stringify({ ...a, reviewStatus: "Approved", actionCodes: ["delete"] }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }
      if (url.includes(ID_B)) {
        if (mode === "reject") {
          throw new TypeError("Failed to fetch");
        }
        return new Response(JSON.stringify({ ...b, reviewStatus: "Approved", actionCodes: ["delete"] }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }
    }
    if (url.includes("/query") || url.includes("/stories")) {
      return new Response(JSON.stringify({ items: rows, page: 1, pageSize: 50, totalCount: rows.length }), {
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

  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-admin-r15");
  const tree = await mount(
    createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }),
  );
  await flush();
  await waitFor(() => qq(tree.container, ".ag-row").length >= 3);
  await selectAgRows(tree.container, 3);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-dialog"]').length === 1);
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();

  await waitFor(() => bodies.length >= 2);
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-result"]').length === 0);
  assert.equal(unhandled, 0, "rejected fetch must not escape as unhandled rejection");

  const pending = listPendingStoryIntents("admin", null, "actor-admin-r15");
  const bPending = pending.find((p) => p.state === "ok" && p.envelope.routeTargetId === ID_B);
  assert.ok(bPending && bPending.state === "ok");
  assert.ok(!pending.some((p) => p.state === "ok" && p.envelope.routeTargetId === ID_A));
  assert.ok(bodies.length >= 2);
  const lostBody = bodies[1]!;
  assert.equal(JSON.parse(lostBody).expectedVersion, 16);

  mode = "ok";
  await waitFor(() => qq(document.body, '[data-testid="story-recovery-retry"]').length >= 1);
  await click(q(document.body, '[data-testid="story-recovery-retry"]'));
  await flush();
  await waitFor(() => bodies.length >= 3);
  assert.equal(bodies[2], lostBody);
  assert.equal(bodies.filter((raw) => JSON.parse(raw).expectedVersion === 15).length, 1);

  process.off("unhandledRejection", onUnhandled);
  globalThis.fetch = original;
  await tree.unmount();
});

test("R15: rejection while epoch flips → later rows not_started; zero later dispatch", async () => {
  const a = story({ storyId: ID_A, title: "first", versionToken: 1 });
  const b = story({ storyId: ID_B, title: "second", versionToken: 2 });
  const gate = { inFlightRef: { current: false }, epochRef: { current: 9 } };
  let liveActor = "actor-admin-r15";
  let secondHits = 0;
  let firstReject!: () => void;
  const firstPromise = new Promise<Response>((_resolve, reject) => {
    firstReject = () => reject(new TypeError("Failed to fetch"));
  });
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes(ID_A) && url.includes("/approve")) return firstPromise;
    if (url.includes(ID_B) && url.includes("/approve")) {
      secondHits += 1;
      return new Response(JSON.stringify({ ...b, reviewStatus: "Approved" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  const plan = classifyAdminOp("approve", [a, b]);
  const run = executeAdminOpBatch({
    plan,
    gate,
    scope: { role: "admin", shopId: null, actorUserId: "actor-admin-r15" },
    actorUserId: "actor-admin-r15",
    readLiveScope: () => ({ role: "admin", shopId: null, actorUserId: liveActor }),
  });
  await flush();
  gate.epochRef.current += 1;
  liveActor = "actor-other";
  firstReject();
  const result = await run;
  assert.equal(secondHits, 0);
  assert.ok(result.items.some((i) => i.storyId === ID_A && i.outcome === "unknown"));
  assert.ok(result.items.some((i) => i.storyId === ID_B && i.outcome === "not_started"));
  assert.equal(result.notStarted, 1);
  globalThis.fetch = original;
});
