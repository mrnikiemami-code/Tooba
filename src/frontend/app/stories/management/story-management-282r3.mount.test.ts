/**
 * TB-282-R3 S1/S2 — verified response retention + server product search page.
 * Run:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/stories/management/story-management-282r3.mount.test.ts
 */
import assert from "node:assert/strict";
import test from "node:test";
import { flush, mount } from "./story-mount-harness.ts";
import {
  clearStoryIntentIfCurrent,
  listPendingStoryIntents,
  storeStoryIntent,
  type StoryIntentEnvelope,
} from "./story-intent.ts";
import { runDurableStoryMutation, type StoryExecutorGate } from "./story-executor.ts";
import { bindStoryEnvelopeRoute, verifyStorySuccessPayload } from "./story-response.ts";
import { loadStoryProductOptions } from "./story-lookups.ts";

function baseEnvelope(overrides: Partial<StoryIntentEnvelope> = {}): StoryIntentEnvelope {
  return {
    version: 2,
    action: "create",
    occurrenceKey: "occ-r3-1",
    method: "POST",
    path: "/v1/seller/stories",
    routeTargetId: null,
    expectedVersion: null,
    bodyText: JSON.stringify({ title: "ت", locale: "fa-IR", occurrenceKey: "occ-r3-1" }),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "seller",
    shopId: "22222222-2222-2222-2222-222222222222",
    actorUserId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    recordedAt: new Date().toISOString(),
    ...overrides,
  };
}

test("negative control: pre-fix finalize would clear on empty 200 — verify rejects empty", () => {
  const envelope = baseEnvelope();
  const broken = verifyStorySuccessPayload(envelope, undefined);
  assert.equal(broken.ok, false);
  const emptyObj = verifyStorySuccessPayload(envelope, {});
  assert.equal(emptyObj.ok, false);
});

test("malformed 200 empty/non-JSON keeps envelope byte-identical; valid replay clears", async () => {
  window.sessionStorage.clear();
  const envelope = baseEnvelope();
  assert.equal(storeStoryIntent(envelope), true);
  const before = window.sessionStorage.getItem(
    `tooba.story.intent.v2:seller:22222222-2222-2222-2222-222222222222:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa:create:new`,
  );
  assert.ok(before);

  const gate: StoryExecutorGate = { inFlightRef: { current: false }, epochRef: { current: 1 } };
  const scope = {
    role: "seller" as const,
    shopId: "22222222-2222-2222-2222-222222222222",
    actorUserId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  };

  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () => new Response("", { status: 200 })) as typeof fetch;
  const empty = await runDurableStoryMutation({ gate, envelope, scope });
  assert.equal(empty.state, "failed");
  assert.equal(empty.state === "failed" && empty.code, "story.response.unverified");
  const afterEmpty = window.sessionStorage.getItem(
    `tooba.story.intent.v2:seller:22222222-2222-2222-2222-222222222222:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa:create:new`,
  );
  assert.equal(afterEmpty, before);

  globalThis.fetch = (async () =>
    new Response("not-json", { status: 200, headers: { "Content-Type": "text/plain" } })) as typeof fetch;
  const badJson = await runDurableStoryMutation({ gate, envelope, scope });
  assert.equal(badJson.state, "failed");
  assert.equal(
    window.sessionStorage.getItem(
      `tooba.story.intent.v2:seller:22222222-2222-2222-2222-222222222222:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa:create:new`,
    ),
    before,
  );

  globalThis.fetch = (async () =>
    new Response(JSON.stringify({ storyId: "11111111-1111-1111-1111-111111111111" }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    })) as typeof fetch;
  const ok = await runDurableStoryMutation({ gate, envelope, scope });
  assert.equal(ok.state, "ok");
  assert.equal(
    listPendingStoryIntents(
      "seller",
      "22222222-2222-2222-2222-222222222222",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    ).filter((r) => r.state === "ok").length,
    0,
  );
  globalThis.fetch = originalFetch;
});

test("wrong route/method binding sends zero fetch", async () => {
  window.sessionStorage.clear();
  const envelope = baseEnvelope({ method: "DELETE", path: "/v1/returns/anything" });
  assert.equal(bindStoryEnvelopeRoute(envelope).ok, false);
  const gate: StoryExecutorGate = { inFlightRef: { current: false }, epochRef: { current: 1 } };
  let fetches = 0;
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () => {
    fetches += 1;
    return new Response("{}", { status: 200 });
  }) as typeof fetch;
  const blocked = await runDurableStoryMutation({
    gate,
    envelope,
    scope: {
      role: "seller",
      shopId: "22222222-2222-2222-2222-222222222222",
      actorUserId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    },
  });
  assert.equal(blocked.state, "blocked");
  assert.equal(fetches, 0);
  globalThis.fetch = originalFetch;
});

test("storage enumeration throw yields explicit blocked row not empty success", () => {
  const real = window.sessionStorage;
  const broken = {
    get length() {
      throw new Error("boom-enumerate");
    },
    key() {
      throw new Error("boom-enumerate");
    },
    getItem() {
      throw new Error("boom-enumerate");
    },
    setItem() {
      throw new Error("boom-enumerate");
    },
    removeItem() {},
    clear() {},
  };
  Object.defineProperty(globalThis, "sessionStorage", { configurable: true, value: broken });
  try {
    const rows = listPendingStoryIntents("seller", "s", "a");
    assert.equal(rows.length, 1);
    assert.equal(rows[0]?.state, "blocked");
  } finally {
    Object.defineProperty(globalThis, "sessionStorage", { configurable: true, value: real });
  }
});

test("product options request carries search+page and returns totalCount", async () => {
  const originalFetch = globalThis.fetch;
  let seen = "";
  globalThis.fetch = (async (input: RequestInfo | URL) => {
    seen = String(input);
    return new Response(
      JSON.stringify({
        items: [{ productId: "11111111-1111-1111-1111-111111111111", title: "کالای دور" }],
        totalCount: 55,
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  }) as typeof fetch;
  window.localStorage.setItem("tooba.adminActorUserId", "admin-1");
  const page = await loadStoryProductOptions({
    mode: "admin",
    shopId: null,
    actorUserId: "admin-1",
    search: "کالای دور",
    page: 3,
    pageSize: 20,
  });
  assert.match(seen, /search=/);
  assert.match(seen, /page=3/);
  assert.match(seen, /pageSize=20/);
  assert.equal(page.totalCount, 55);
  assert.equal(page.items.length, 1);
  globalThis.fetch = originalFetch;
});

void flush;
void mount;
void clearStoryIntentIfCurrent;
