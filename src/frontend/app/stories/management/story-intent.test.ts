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

test("story intent stores exact bytes and clears only the same envelope", () => {
  const previous = globalThis.sessionStorage;
  Object.defineProperty(globalThis, "sessionStorage", {
    configurable: true,
    value: memoryStorage(),
  });
  try {
    const envelope: StoryIntentEnvelope = {
      version: 1,
      action: "submit",
      occurrenceKey: newStoryOccurrenceKey(),
      routeTargetId: "story-1",
      expectedVersion: 3,
      body: { expectedVersion: 3 },
      role: "seller",
      shopId: "shop-a",
      recordedAt: "2026-11-19T00:00:00.000Z",
    };
    assert.equal(storeStoryIntent(envelope), true);
    const read = readStoryIntent("seller", "shop-a", "submit", "story-1");
    assert.deepEqual(read, envelope);
    clearStoryIntentIfCurrent({ ...envelope, occurrenceKey: "other" });
    assert.ok(readStoryIntent("seller", "shop-a", "submit", "story-1"));
    clearStoryIntentIfCurrent(envelope);
    assert.equal(readStoryIntent("seller", "shop-a", "submit", "story-1"), null);
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
