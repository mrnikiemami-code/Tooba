/**
 * TB-282-R14 — bulk stop-on-scope + per-row result panel; real AppDataGrid multi-select.
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
import { listPendingStoryIntents } from "./story-intent.ts";
import { executeAdminOpBatch, classifyAdminOp } from "./story-admin-ops.ts";
import type { AdminStorySnapshot } from "../story-api.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";

ensureDom();

const ID_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa14a1";
const ID_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb14b2";
const ID_C = "cccccccc-cccc-cccc-cccc-cccccccc14c3";
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

type GateCtl = {
  resolve: (value: Response) => void;
  promise: Promise<Response>;
};

function deferredResponse(): GateCtl {
  let resolve!: (value: Response) => void;
  const promise = new Promise<Response>((r) => {
    resolve = r;
  });
  return { resolve, promise };
}

async function selectAgRows(container: HTMLElement, count: number) {
  await waitFor(() => qq(container, ".ag-row").length >= count);
  // Selection column may be pinned; prefer row checkboxes (skip header checkbox).
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

test("R14: real grid select 2 eligible + 1 skipped; per-title results; distinct keys", async () => {
  const a = story({ storyId: ID_A, title: "A-submit", versionToken: 11 });
  const b = story({ storyId: ID_B, title: "B-submit", versionToken: 22 });
  const c = story({
    storyId: ID_C,
    title: "C-approved",
    reviewStatus: "Approved",
    versionToken: 33,
    actionCodes: ["delete", "schedule"],
  });
  const rows = [a, b, c];
  const posts: Array<{ url: string; body: string }> = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = String(init?.body ?? "");
    if (method === "POST" && /\/approve\/?$/.test(url) && !url.includes("approve-and")) {
      posts.push({ url, body });
      const id = url.includes(ID_A) ? ID_A : ID_B;
      const row = rows.find((r) => r.storyId === id)!;
      return new Response(JSON.stringify({ ...row, reviewStatus: "Approved", actionCodes: ["delete"] }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/query") || url.includes("/stories")) {
      if (method === "GET" && rows.some((r) => url.includes(r.storyId))) {
        const hit = rows.find((r) => url.includes(r.storyId))!;
        return new Response(JSON.stringify(hit), { status: 200, headers: { "Content-Type": "application/json" } });
      }
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

  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-admin-r14");
  const tree = await mount(
    createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-live-grid"]').length === 1);
  await waitFor(() => qq(tree.container, ".ag-row").length >= 3);

  await selectAgRows(tree.container, 3);
  await waitFor(() => qq(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]').length >= 1);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-dialog"]').length === 1);
  assert.equal(qq(document.body, '[data-testid="story-admin-op-eligible"] li').length, 2);
  assert.equal(qq(document.body, '[data-testid="story-admin-op-skipped"] li').length, 1);
  assert.match(q(document.body, '[data-testid="story-admin-op-skipped"]').textContent ?? "", /C-approved|تأیید/);

  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => posts.length === 2);
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-result"]').length === 1);

  const ids = posts.map((p) => p.url.match(/stories\/([0-9a-fA-F-]+)/)?.[1]);
  assert.deepEqual(new Set(ids), new Set([ID_A, ID_B]));
  assert.ok(!posts.some((p) => p.url.includes(ID_C)));
  const keys = posts.map((p) => JSON.parse(p.body).occurrenceKey);
  assert.notEqual(keys[0], keys[1]);
  assert.equal(JSON.parse(posts.find((p) => p.url.includes(ID_A))!.body).expectedVersion, 11);
  assert.equal(JSON.parse(posts.find((p) => p.url.includes(ID_B))!.body).expectedVersion, 22);

  const resultItems = qq(document.body, '[data-testid="story-admin-op-result-item"]');
  assert.ok(resultItems.length >= 3);
  const text = q(document.body, '[data-testid="story-admin-op-result-list"]').textContent ?? "";
  assert.match(text, /A-submit/);
  assert.match(text, /B-submit/);
  assert.match(text, /C-approved/);
  assert.match(text, /موفق/);
  assert.match(text, /ردشده/);

  globalThis.fetch = original;
  await tree.unmount();
});

test("R14: partial unknown retains second; retry exact bytes; first not repeated", async () => {
  const a = story({ storyId: ID_A, title: "ok-row", versionToken: 7 });
  const b = story({ storyId: ID_B, title: "lost-row", versionToken: 8 });
  const rows = [a, b];
  const bodies: string[] = [];
  let mode: "partial" | "ok" = "partial";
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = String(init?.body ?? "");
    if (method === "POST" && /\/approve\/?$/.test(url) && !url.includes("approve-and")) {
      bodies.push(body);
      if (mode === "partial") {
        if (url.includes(ID_A)) {
          return new Response(JSON.stringify({ ...a, reviewStatus: "Approved" }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        return new Response("", { status: 200, headers: { "Content-Type": "application/json" } });
      }
      return new Response(JSON.stringify({ ...b, reviewStatus: "Approved" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/query") || url.includes("/stories")) {
      return new Response(JSON.stringify({ items: rows, page: 1, pageSize: 50, totalCount: 2 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("locales")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-admin-r14");
  const tree = await mount(
    createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }),
  );
  await flush();
  await waitFor(() => qq(tree.container, ".ag-row").length >= 2);
  await selectAgRows(tree.container, 2);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]'));
  await flush();
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => bodies.length === 2);
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-result-item"][data-outcome="unknown"]').length >= 1);
  const pending = listPendingStoryIntents("admin", null, "actor-admin-r14");
  assert.ok(pending.some((p) => p.state === "ok" && p.envelope.routeTargetId === ID_B));
  const secondBody = bodies[1]!;

  mode = "ok";
  await waitFor(() => qq(document.body, '[data-testid="story-recovery-retry"]').length >= 1);
  await click(q(document.body, '[data-testid="story-recovery-retry"]'));
  await flush();
  await waitFor(() => bodies.length >= 3);
  assert.equal(bodies[2], secondBody);
  assert.equal(bodies.filter((raw) => JSON.parse(raw).expectedVersion === 7).length, 1);

  globalThis.fetch = original;
  await tree.unmount();
});

test("R14: epoch bump mid-batch → remaining not_started; no second network", async () => {
  const a = story({ storyId: ID_A, title: "first", versionToken: 1 });
  const b = story({ storyId: ID_B, title: "second", versionToken: 2 });
  const gate = { inFlightRef: { current: false }, epochRef: { current: 5 } };
  let liveActor = "actor-admin-r14";
  const first = deferredResponse();
  let secondHits = 0;
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST" && url.includes(ID_A) && /\/approve\/?$/.test(url)) {
      return first.promise;
    }
    if (method === "POST" && url.includes(ID_B) && /\/approve\/?$/.test(url)) {
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
    scope: { role: "admin", shopId: null, actorUserId: "actor-admin-r14" },
    actorUserId: "actor-admin-r14",
    readLiveScope: () => ({ role: "admin", shopId: null, actorUserId: liveActor }),
  });

  await flush();
  // While first in-flight, bump epoch (scope/unmount semantics).
  gate.epochRef.current += 1;
  liveActor = "actor-other";
  first.resolve(
    new Response(JSON.stringify({ ...a, reviewStatus: "Approved" }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }),
  );
  const result = await run;
  assert.equal(secondHits, 0);
  assert.equal(result.notStarted, 1);
  assert.ok(result.items.some((i) => i.storyId === ID_B && i.outcome === "not_started"));
  assert.ok(result.items.some((i) => i.storyId === ID_A && (i.outcome === "succeeded" || i.outcome === "unknown")));
  assert.match(result.items.find((i) => i.outcome === "not_started")!.message ?? "", /اجرا نشد/);

  globalThis.fetch = original;
});

test("R14: reject bulk requires reason; cancel zero POST", async () => {
  const a = story({ storyId: ID_A, title: "rej", versionToken: 3 });
  let posts = 0;
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST" && url.includes("/reject")) posts += 1;
    if (url.includes("/query") || url.includes("/stories")) {
      return new Response(JSON.stringify({ items: [a], page: 1, pageSize: 50, totalCount: 1 }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("locales")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-admin-r14");
  const tree = await mount(
    createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }),
  );
  await flush();
  await waitFor(() => qq(tree.container, ".ag-row").length >= 1);
  await selectAgRows(tree.container, 1);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-reject"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-reject-reason-input"]').length === 1);
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  assert.equal(posts, 0);
  await click(q(document.body, '[data-testid="story-admin-op-cancel"]'));
  await flush();
  assert.equal(posts, 0);

  await selectAgRows(tree.container, 1);
  await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-reject"]'));
  await flush();
  await setInputValue(q(document.body, '[data-testid="story-reject-reason-input"]') as HTMLInputElement, "ناقص");
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => posts === 1);

  globalThis.fetch = original;
  await tree.unmount();
});
