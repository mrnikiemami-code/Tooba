/**
 * Story shop picker must page server-side: first 50, then the next 50 on scroll,
 * and search must restart at page 1 with the server filter.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import test from "node:test";
import { createElement } from "react";
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
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";

ensureDom();

type SellerQuery = { page: number; pageSize: number; search?: string };

function jsonResponse(value: unknown): Response {
  return new Response(JSON.stringify(value), {
    status: 200,
    headers: { "Content-Type": "application/json" },
  });
}

function sellerRows(start: number, count: number, prefix = "فروشگاه") {
  return Array.from({ length: count }, (_, index) => {
    const number = start + index;
    return {
      sellerPartyId: `seller-${number}`,
      displayName: `${prefix} ${number}`,
      status: "Active",
    };
  });
}

test("shop picker loads 50 at a time and server-searches from page 1", async () => {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-shop-pages");
  const sellerQueries: SellerQuery[] = [];
  const originalFetch = globalThis.fetch;

  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST" && url.includes("/v1/admin/sellers/query")) {
      const body = JSON.parse(String(init?.body ?? "{}")) as SellerQuery;
      sellerQueries.push(body);
      const isSearch = Boolean(body.search?.trim());
      const total = isSearch ? 7 : 70;
      const items = isSearch
        ? sellerRows(1, 7, "فروشگاه ویژه")
        : sellerRows((body.page - 1) * 50 + 1, body.page === 1 ? 50 : 20);
      return jsonResponse({ items, page: body.page, pageSize: body.pageSize, totalCount: total });
    }
    if (url.includes("/locale-options") || url.includes("locales") || url.includes("languages")) {
      return jsonResponse([{ code: "fa", name: "فارسی", isDefault: true }]);
    }
    if (url.includes("/v1/admin/stories/query")) {
      return jsonResponse({ items: [], page: 1, pageSize: 50, totalCount: 0 });
    }
    return jsonResponse({});
  }) as typeof fetch;

  try {
    const tree = await mount(
      createElement(StoryManagementScreen, {
        capabilities: ADMIN_STORY_CAPABILITIES,
        createMode: true,
        navigate: () => undefined,
      }),
    );
    await flush();
    await waitFor(() => qq(tree.container, '[data-testid="story-create-workspace"]').length === 1);

    await click(q(tree.container, '[data-testid="story-admin-shop-mode"]'));
    await waitFor(() => qq(tree.container, '[data-testid="story-admin-shop-option"]').length === 50);
    assert.equal(sellerQueries[0]?.page, 1);
    assert.equal(sellerQueries[0]?.pageSize, 50);
    assert.equal(sellerQueries[0]?.search, undefined);

    const list = q(tree.container, '[data-testid="story-admin-shop-list"]');
    Object.defineProperty(list, "scrollHeight", { value: 1000, configurable: true });
    Object.defineProperty(list, "clientHeight", { value: 200, configurable: true });
    Object.defineProperty(list, "scrollTop", { value: 800, configurable: true, writable: true });
    list.dispatchEvent(new Event("scroll", { bubbles: true }));

    await waitFor(() => qq(tree.container, '[data-testid="story-admin-shop-option"]').length === 70);
    assert.equal(sellerQueries[1]?.page, 2);
    assert.equal(sellerQueries[1]?.pageSize, 50);
    assert.equal(sellerQueries[1]?.search, undefined);

    await setInputValue(q(tree.container, '[data-testid="story-admin-shop-search"]'), "فروشگاه ویژه");
    await waitFor(() => sellerQueries.some((query) => query.search === "فروشگاه ویژه"));
    await waitFor(() => qq(tree.container, '[data-testid="story-admin-shop-option"]').length === 7);
    const searchQuery = sellerQueries.find((query) => query.search === "فروشگاه ویژه");
    assert.equal(searchQuery?.page, 1);
    assert.equal(searchQuery?.pageSize, 50);
    assert.match(tree.container.textContent ?? "", /فروشگاه ویژه 1/);

    await tree.unmount();
  } finally {
    globalThis.fetch = originalFetch;
  }
});
