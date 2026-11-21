import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import { act } from "react-dom/test-utils";
import test from "node:test";
import { click, ensureDom, flush, mount, q, qq, setInputValue, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";
import type { AdminStorySnapshot } from "../story-api.ts";

ensureDom();

function row(index: number): AdminStorySnapshot {
  const now = new Date().toISOString();
  const id = `aaaaaaaa-0000-4000-8000-${index.toString(16).padStart(12, "0")}`;
  return {
    id, storyId: id, tenantId: "store-alpha", origin: "Seller", reviewStatus: "None",
    sellerPartyId: "22222222-2222-2222-2222-222222222222", rejectionReason: null,
    submittedAt: null, reviewedAt: null, submittedByActorUserId: null, reviewedByActorUserId: null,
    locale: "fa", market: null, title: `Row ${index}`, coverMediaAssetId: null, coverMediaUrl: null,
    displayOrder: index, startAt: null, endAt: null, status: "Draft", ctaType: "none", ctaTarget: null,
    versionToken: 1, createdAt: now, updatedAt: now, createdByActorUserId: null,
    shopDisplayName: "فروشگاه", contentReady: true, effectivelyPublic: false, actionCodes: ["submit"],
    items: [],
  };
}

test("grid search and non-default page survive a successful row operation", async () => {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-grid-r21");
  window.sessionStorage.clear();
  const rows = Array.from({ length: 25 }, (_, index) => row(index));
  let lastQuery: Record<string, unknown> = {};
  let queryCount = 0;
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (url.includes("locale-options")) return new Response(JSON.stringify([{ code: "fa", name: "فارسی", isDefault: true }]), { status: 200, headers: { "Content-Type": "application/json" } });
    if (method === "POST" && url.includes("/stories/query")) {
      lastQuery = JSON.parse(String(init?.body ?? "{}")) as Record<string, unknown>;
      queryCount += 1;
      const page = Number(lastQuery.page ?? 1);
      const pageSize = Number(lastQuery.pageSize ?? 50);
      return new Response(JSON.stringify({ items: rows.slice((page - 1) * pageSize, page * pageSize), page, pageSize, totalCount: rows.length }), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    if (method === "POST" && /\/stories\/[^/]+\/submit$/.test(url)) {
      const id = url.match(/stories\/([^/]+)\/submit/)?.[1];
      const hit = rows.find((candidate) => candidate.storyId === id)!;
      return new Response(JSON.stringify({ ...hit, reviewStatus: "Submitted", versionToken: 2 }), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  try {
    const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }));
    await flush();
    await waitFor(() => qq(tree.container, ".ag-row").length > 0);
    const search = q(tree.container, "[data-app-grid-search]");
    await setInputValue(search, "Row");
    await act(async () => {
      search.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true }));
    });
    await waitFor(() => lastQuery.search === "Row");
    const next = Array.from(tree.container.querySelectorAll("[data-app-grid-pagination] button")).find((button) => (button.textContent ?? "").trim() === "بعدی") as HTMLButtonElement;
    assert.ok(next);
    await click(next);
    await waitFor(() => lastQuery.page === 2);
    const before = queryCount;
    await click(q(tree.container, '[data-testid="app-grid-action-submit"]'));
    await waitFor(() => queryCount > before);
    assert.equal(lastQuery.search, "Row");
    assert.equal(lastQuery.page, 2);
    await tree.unmount();
  } finally {
    globalThis.fetch = original;
  }
});
