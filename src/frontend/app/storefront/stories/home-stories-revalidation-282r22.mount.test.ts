import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { ensureDom, flush, mount, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
import {
  filterLiveStories,
  storyRevalidationDelay,
  useStoryRevalidation,
} from "./home-stories-revalidation.ts";
import type { PublicStoryCard } from "../../stories/story-api.ts";

ensureDom();

function story(id: string, endAt: string | null): PublicStoryCard {
  return {
    storyId: id,
    title: id,
    coverMediaUrl: null,
    isVideo: false,
    displayOrder: 0,
    ctaType: "none",
    ctaTarget: null,
    items: [],
    endAt,
  };
}

function Probe({ stories, onTick, maxMs }: { stories: PublicStoryCard[]; onTick: () => void; maxMs: number }) {
  useStoryRevalidation(stories, onTick, maxMs);
  return null;
}

test("R22 F05: a 20-minute end is revalidated repeatedly even when the feed payload is unchanged", async () => {
  const endAt = new Date(Date.now() + 20 * 60 * 1000).toISOString();
  let ticks = 0;
  const tree = await mount(createElement(Probe, {
    stories: [story("future", endAt)],
    onTick: () => { ticks += 1; },
    maxMs: 20,
  }));
  await flush();
  await waitFor(() => ticks >= 3, { timeoutMs: 1500, intervalMs: 10 });
  assert.ok(ticks >= 3, "the timer must reschedule after each capped revalidation");
  await tree.unmount();
});

test("R22 F05: local expiry filtering removes a story after a failed refresh", () => {
  const now = Date.now();
  const live = story("live", new Date(now + 60_000).toISOString());
  const expired = story("expired", new Date(now - 1000).toISOString());
  assert.deepEqual(filterLiveStories([live, expired], now).map((row) => row.storyId), ["live"]);
  assert.equal(storyRevalidationDelay(now + 20 * 60_000, now, 1000), 1000);
});
