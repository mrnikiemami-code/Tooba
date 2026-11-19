import assert from "node:assert/strict";
import test from "node:test";
import {
  clearStoryIntentIfCurrent,
  newStoryOccurrenceKey,
  readStoryIntent,
  storeStoryIntent,
  type StoryIntentEnvelope,
} from "./story-intent.ts";

function memoryStorage(): Storage {
  const map = new Map<string, string>();
  return {
    get length() {
      return map.size;
    },
    clear() {
      map.clear();
    },
    getItem(key: string) {
      return map.has(key) ? map.get(key)! : null;
    },
    key(index: number) {
      return [...map.keys()][index] ?? null;
    },
    removeItem(key: string) {
      map.delete(key);
    },
    setItem(key: string, value: string) {
      map.set(key, String(value));
    },
  } as Storage;
}

function baseEnvelope(overrides: Partial<StoryIntentEnvelope> = {}): StoryIntentEnvelope {
  return {
    version: 2,
    action: "submit",
    occurrenceKey: newStoryOccurrenceKey(),
    method: "POST",
    path: "/v1/seller/stories/x/submit",
    routeTargetId: "story-1",
    expectedVersion: 3,
    bodyText: JSON.stringify({ expectedVersion: 3, occurrenceKey: "k" }),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "seller",
    shopId: "shop-a",
    actorUserId: "actor-1",
    recordedAt: "2026-11-19T00:00:00.000Z",
    ...overrides,
  };
}

test("story intent stores exact bytes and clears only the same envelope", () => {
  const previous = globalThis.sessionStorage;
  Object.defineProperty(globalThis, "sessionStorage", {
    configurable: true,
    value: memoryStorage(),
  });
  try {
    const envelope = baseEnvelope();
    assert.equal(storeStoryIntent(envelope), true);
    const read = readStoryIntent("seller", "shop-a", "actor-1", "submit", "story-1");
    assert.equal(read.state, "ok");
    if (read.state === "ok") assert.deepEqual(read.envelope, envelope);
    clearStoryIntentIfCurrent({ ...envelope, occurrenceKey: "other" });
    assert.equal(readStoryIntent("seller", "shop-a", "actor-1", "submit", "story-1").state, "ok");
    clearStoryIntentIfCurrent(envelope);
    assert.equal(readStoryIntent("seller", "shop-a", "actor-1", "submit", "story-1").state, "missing");
  } finally {
    Object.defineProperty(globalThis, "sessionStorage", {
      configurable: true,
      value: previous,
    });
  }
});

test("pending intent refuses overwrite with a different occurrence key", () => {
  const previous = globalThis.sessionStorage;
  Object.defineProperty(globalThis, "sessionStorage", {
    configurable: true,
    value: memoryStorage(),
  });
  try {
    const first = baseEnvelope({ occurrenceKey: "key-a" });
    assert.equal(storeStoryIntent(first), true);
    const second = baseEnvelope({ occurrenceKey: "key-b" });
    assert.equal(storeStoryIntent(second), false);
    const read = readStoryIntent("seller", "shop-a", "actor-1", "submit", "story-1");
    assert.equal(read.state, "ok");
    if (read.state === "ok") assert.equal(read.envelope.occurrenceKey, "key-a");
  } finally {
    Object.defineProperty(globalThis, "sessionStorage", {
      configurable: true,
      value: previous,
    });
  }
});

test("same key changed body is blocked", () => {
  const previous = globalThis.sessionStorage;
  Object.defineProperty(globalThis, "sessionStorage", {
    configurable: true,
    value: memoryStorage(),
  });
  try {
    const first = baseEnvelope({ occurrenceKey: "key-a", bodyText: "{\"a\":1}" });
    assert.equal(storeStoryIntent(first), true);
    const second = baseEnvelope({ occurrenceKey: "key-a", bodyText: "{\"a\":2}" });
    assert.equal(storeStoryIntent(second), false);
  } finally {
    Object.defineProperty(globalThis, "sessionStorage", {
      configurable: true,
      value: previous,
    });
  }
});

test("newStoryOccurrenceKey is stable UUID-shaped and distinct", () => {
  const a = newStoryOccurrenceKey();
  const b = newStoryOccurrenceKey();
  assert.match(a, /^[0-9a-f-]{36}$/i);
  assert.notEqual(a, b);
});
