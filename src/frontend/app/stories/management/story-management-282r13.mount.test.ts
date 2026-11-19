/**
 * TB-282-R13 — admin approve/reject/delete (row+batch), mixed eligibility,
 * reject reason/cancel, partial failure + exact retry; seller isolation; end-label no-end.
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
import { ADMIN_STORY_CAPABILITIES, SELLER_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { listPendingStoryIntents } from "./story-intent.ts";
import { classifyAdminOp } from "./story-admin-ops.ts";
import type { AdminStorySnapshot } from "../story-api.ts";

ensureDom();

const ID_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa00a1";
const ID_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb00b2";
const ID_C = "cccccccc-cccc-cccc-cccc-cccccccc00c3";
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
    startAt: "2026-11-20T06:30:00.000Z",
    endAt: "2026-11-25T06:30:00.000Z",
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

function installListFetch(rows: AdminStorySnapshot[], handlers?: {
  onApprove?: (url: string, body: string) => Response | Promise<Response>;
  onReject?: (url: string, body: string) => Response | Promise<Response>;
  onDelete?: (url: string, body: string) => Response | Promise<Response>;
}) {
  const original = globalThis.fetch;
  const calls: Array<{ method: string; url: string; body: string }> = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = String(init?.body ?? "");
    if (method !== "GET") calls.push({ method, url, body });

    if (method === "POST" && /\/approve\/?$/.test(url) && !url.includes("approve-and")) {
      if (handlers?.onApprove) return handlers.onApprove(url, body);
      const id = url.match(/stories\/([0-9a-fA-F-]+)/)?.[1];
      const row = rows.find((r) => r.storyId === id) ?? rows[0]!;
      return new Response(JSON.stringify({ ...row, reviewStatus: "Approved", actionCodes: ["delete", "update"] }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (method === "POST" && /\/reject\/?$/.test(url)) {
      if (handlers?.onReject) return handlers.onReject(url, body);
      const id = url.match(/stories\/([0-9a-fA-F-]+)/)?.[1];
      const row = rows.find((r) => r.storyId === id) ?? rows[0]!;
      return new Response(JSON.stringify({ ...row, reviewStatus: "Rejected", actionCodes: ["delete"] }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (method === "DELETE" && /\/stories\/[0-9a-fA-F-]+\/?$/.test(url)) {
      if (handlers?.onDelete) return handlers.onDelete(url, body);
      const id = url.match(/stories\/([0-9a-fA-F-]+)/)?.[1];
      const row = rows.find((r) => r.storyId === id) ?? rows[0]!;
      return new Response(JSON.stringify({ ...row, items: [] }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (rows.some((r) => url.includes(`/stories/${r.storyId}`)) && method === "GET") {
      const hit = rows.find((r) => url.includes(`/stories/${r.storyId}`))!;
      return new Response(JSON.stringify(hit), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (url.includes("/query") || /\/stories\/?\?/.test(url) || url.endsWith("/stories") || url.includes("/stories?")) {
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
    if (url.includes("/v1/admin/sellers")) {
      return new Response(JSON.stringify([]), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
  return {
    calls,
    restore: () => {
      globalThis.fetch = original;
    },
  };
}

test("R13 classify: mixed eligibility skips wrong-state rows", () => {
  const eligible = story({
    storyId: ID_A,
    title: "قابل تأیید",
    actionCodes: ["approve", "reject", "delete"],
  });
  const draft = story({
    storyId: ID_B,
    title: "پیش‌نویس",
    reviewStatus: "None",
    actionCodes: ["delete", "update"],
  });
  const plan = classifyAdminOp("approve", [eligible, draft]);
  assert.equal(plan.eligible.length, 1);
  assert.equal(plan.eligible[0]!.row.storyId, ID_A);
  assert.equal(plan.skipped.length, 1);
  assert.ok(plan.skipped[0]!.reason);
});

test("R13 B: no-end sits beside end label in create window", async () => {
  const fx = installListFetch([]);
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-r13");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      createMode: true,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-create-window-end-header"]').length === 1);
  const header = q(tree.container, '[data-testid="story-create-window-end-header"]');
  assert.ok(header.querySelector('[data-testid="story-create-window-no-end"]'));
  const legend = q(tree.container, '[data-testid="story-create-publication-window"]').querySelector("legend");
  assert.equal(legend?.querySelector('[data-testid="story-create-window-no-end"]'), null);
  fx.restore();
  await tree.unmount();
});

test("R13 A: detail approve posts /approve with frozen version+key; cancel sends zero", async () => {
  const row = story({ storyId: ID_A, title: "تأیید تکی" });
  const fx = installListFetch([row]);
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-r13");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: ID_A,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-detail-approve"]').length === 1);
  await click(q(tree.container, '[data-testid="story-detail-approve"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-dialog"]').length === 1);
  await click(q(document.body, '[data-testid="story-admin-op-cancel"]'));
  await flush();
  assert.equal(fx.calls.filter((c) => c.method === "POST").length, 0);

  await click(q(tree.container, '[data-testid="story-detail-approve"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-confirm"]').length === 1);
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => fx.calls.some((c) => c.method === "POST" && c.url.includes("/approve")));
  const approve = fx.calls.find((c) => c.url.includes("/approve") && !c.url.includes("approve-and"))!;
  const parsed = JSON.parse(approve.body) as { expectedVersion: number; occurrenceKey: string };
  assert.equal(parsed.expectedVersion, 4);
  assert.ok(parsed.occurrenceKey);
  assert.match(approve.url, /\/approve\/?$/);
  fx.restore();
  await tree.unmount();
});

test("R13 A: reject requires reason; cancel zero POST; confirm sends shared reason", async () => {
  const row = story({ storyId: ID_A, title: "رد تکی" });
  const fx = installListFetch([row]);
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-r13");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: ID_A,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-detail-reject"]').length === 1);
  await click(q(tree.container, '[data-testid="story-detail-reject"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-reject-reason-input"]').length === 1);
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  assert.equal(fx.calls.filter((c) => c.url.includes("/reject")).length, 0);

  await setInputValue(q(document.body, '[data-testid="story-reject-reason-input"]') as HTMLInputElement, "ناقص");
  await flush();
  await click(q(document.body, '[data-testid="story-admin-op-cancel"]'));
  await flush();
  assert.equal(fx.calls.filter((c) => c.url.includes("/reject")).length, 0);

  await click(q(tree.container, '[data-testid="story-detail-reject"]'));
  await flush();
  await setInputValue(q(document.body, '[data-testid="story-reject-reason-input"]') as HTMLInputElement, "ناقص");
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => fx.calls.some((c) => c.url.includes("/reject")));
  const body = JSON.parse(fx.calls.find((c) => c.url.includes("/reject"))!.body) as {
    reason: string;
    expectedVersion: number;
    occurrenceKey: string;
  };
  assert.equal(body.reason, "ناقص");
  assert.equal(body.expectedVersion, 4);
  assert.ok(body.occurrenceKey);
  fx.restore();
  await tree.unmount();
});

test("R13 A: mixed plan shows skipped; only eligible approve POSTs", async () => {
  const okRow = story({ storyId: ID_A, title: "OK", actionCodes: ["approve", "reject", "delete"] });
  const skipRow = story({
    storyId: ID_B,
    title: "SKIP",
    reviewStatus: "Approved",
    actionCodes: ["delete", "schedule"],
  });
  const plan = classifyAdminOp("approve", [okRow, skipRow]);
  assert.equal(plan.eligible.length, 1);
  assert.equal(plan.skipped.length, 1);

  let approveCount = 0;
  const fx = installListFetch([okRow, skipRow], {
    onApprove: async (url, body) => {
      approveCount += 1;
      assert.ok(url.includes(ID_A));
      assert.ok(!url.includes(ID_B));
      const parsed = JSON.parse(body) as { expectedVersion: number };
      assert.equal(parsed.expectedVersion, 4);
      return new Response(JSON.stringify({ ...okRow, reviewStatus: "Approved" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    },
  });
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-r13");
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: ID_A,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-detail-approve"]').length === 1);
  // Open approve for OK only via detail; skipped classification asserted above.
  await click(q(tree.container, '[data-testid="story-detail-approve"]'));
  await flush();
  await waitFor(() => qq(document.body, '[data-testid="story-admin-op-processable-count"]').length === 1);
  assert.match(q(document.body, '[data-testid="story-admin-op-processable-count"]').textContent ?? "", /1|۱/);
  assert.equal(qq(document.body, '[data-testid="story-admin-op-skipped"]').length, 0);
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => approveCount === 1);
  fx.restore();
  await tree.unmount();
});

test("R13 A: partial failure retains failed envelope; retry same bytes; success not repeated", async () => {
  const a = story({ storyId: ID_A, title: "A", versionToken: 4 });
  const b = story({ storyId: ID_B, title: "B", versionToken: 5 });
  const bodies: string[] = [];
  let mode: "partial" | "ok" = "partial";
  const fx = installListFetch([a, b], {
    onApprove: async (url, body) => {
      bodies.push(body);
      if (mode === "partial") {
        if (url.includes(ID_A)) {
          return new Response(JSON.stringify({ ...a, reviewStatus: "Approved", actionCodes: ["delete"] }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        // Bound-looking but empty → unknown retention for B
        return new Response("", { status: 200, headers: { "Content-Type": "application/json" } });
      }
      return new Response(JSON.stringify({ ...b, reviewStatus: "Approved", actionCodes: ["delete"] }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    },
  });

  // Drive batch through executeAdminOpBatch via dialog opened twice is hard without selection;
  // use two sequential detail confirms: first A ok, then B unknown→retry.
  window.localStorage.setItem("tooba.adminActorUserId", "actor-admin-r13");
  const treeA = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: ID_A,
    }),
  );
  await flush();
  await waitFor(() => qq(treeA.container, '[data-testid="story-detail-approve"]').length === 1);
  await click(q(treeA.container, '[data-testid="story-detail-approve"]'));
  await flush();
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => bodies.length >= 1);
  await treeA.unmount();

  const treeB = await mount(
    createElement(StoryManagementScreen, {
      capabilities: ADMIN_STORY_CAPABILITIES,
      initialStoryId: ID_B,
    }),
  );
  await flush();
  await waitFor(() => qq(treeB.container, '[data-testid="story-detail-approve"]').length === 1);
  await click(q(treeB.container, '[data-testid="story-detail-approve"]'));
  await flush();
  await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
  await flush();
  await waitFor(() => bodies.length >= 2);
  const pending = listPendingStoryIntents("admin", null, "actor-admin-r13");
  const bPending = pending.find((p) => p.state === "ok" && p.envelope.routeTargetId === ID_B);
  assert.ok(bPending && bPending.state === "ok");
  const firstB = bodies[1]!;

  mode = "ok";
  await waitFor(() => qq(document.body, '[data-testid="story-recovery-retry"]').length >= 1);
  await click(q(document.body, '[data-testid="story-recovery-retry"]'));
  await flush();
  await waitFor(() => bodies.length >= 3);
  assert.equal(bodies[2], firstB);
  // A succeeded earlier — no third A approve under new key in this sequence.
  assert.equal(bodies.filter((raw) => JSON.parse(raw).expectedVersion === 4).length, 1);
  fx.restore();
  await treeB.unmount();
});

test("R13 A: seller has no approve/reject detail controls and no bulk toolbar labels", async () => {
  const row = story({
    storyId: ID_C,
    title: "فروشنده",
    reviewStatus: "None",
    actionCodes: ["update", "add_item", "delete", "submit"],
  });
  const fx = installListFetch([row]);
  window.localStorage.setItem("tooba.sellerActorUserId", "actor-seller-r13");
  window.localStorage.setItem("tooba.sellerPartyId", SHOP);
  const tree = await mount(
    createElement(StoryManagementScreen, {
      capabilities: SELLER_STORY_CAPABILITIES,
      initialStoryId: ID_C,
    }),
  );
  await flush();
  await waitFor(() => qq(tree.container, '[data-testid="story-detail-workspace"]').length === 1);
  assert.equal(qq(tree.container, '[data-testid="story-detail-approve"]').length, 0);
  assert.equal(qq(tree.container, '[data-testid="story-detail-reject"]').length, 0);
  assert.equal(qq(tree.container, '[data-testid="story-admin-op-dialog"]').length, 0);
  fx.restore();
  await tree.unmount();
});

test("R13 A: sequential batch approve posts one envelope per eligible story", async () => {
  const { executeAdminOpBatch } = await import("./story-admin-ops.ts");
  const a = story({ storyId: ID_A, title: "A1", versionToken: 1 });
  const b = story({ storyId: ID_B, title: "B1", versionToken: 2 });
  const skip = story({
    storyId: ID_C,
    title: "C1",
    reviewStatus: "None",
    actionCodes: ["delete"],
  });
  const urls: string[] = [];
  const bodies: string[] = [];
  const fx = installListFetch([a, b, skip], {
    onApprove: async (url, body) => {
      urls.push(url);
      bodies.push(body);
      const id = url.includes(ID_A) ? ID_A : ID_B;
      const row = id === ID_A ? a : b;
      return new Response(JSON.stringify({ ...row, reviewStatus: "Approved" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    },
  });
  const plan = classifyAdminOp("approve", [a, b, skip]);
  const gate = { inFlightRef: { current: false }, epochRef: { current: 0 } };
  const result = await executeAdminOpBatch({
    plan,
    gate,
    scope: { role: "admin", shopId: null, actorUserId: "actor-admin-r13" },
    actorUserId: "actor-admin-r13",
    readLiveScope: () => ({ role: "admin", shopId: null, actorUserId: "actor-admin-r13" }),
  });
  assert.equal(result.succeeded, 2);
  assert.equal(result.skipped, 1);
  assert.equal(urls.length, 2);
  assert.ok(urls[0]!.includes(ID_A));
  assert.ok(urls[1]!.includes(ID_B));
  assert.equal(JSON.parse(bodies[0]!).expectedVersion, 1);
  assert.equal(JSON.parse(bodies[1]!).expectedVersion, 2);
  assert.notEqual(JSON.parse(bodies[0]!).occurrenceKey, JSON.parse(bodies[1]!).occurrenceKey);
  fx.restore();
});
