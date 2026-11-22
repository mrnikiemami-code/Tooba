import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { ensureDom, flush, mount, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { useHomeStoriesFeed } from "./home-stories-feed.ts";
import type { PublicStoriesResult, PublicStoryCard } from "../../stories/story-api.ts";

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
    items: [{ storyItemId: `${id}-item`, mediaType: "image", mediaUrl: "https://cdn.test/a.jpg", caption: null, durationMs: 1000, ctaType: "none", ctaTarget: null }],
    endAt,
  };
}

test("R22 F05: a slow refresh cannot keep an expired story or start overlapping requests", async () => {
  const live = story("live", new Date(Date.now() + 60_000).toISOString());
  const expiring = story("expiring", new Date(Date.now() + 30).toISOString());
  let calls = 0;
  let resolveSlow: ((value: PublicStoriesResult) => void) | null = null;
  const slow = new Promise<PublicStoriesResult>((resolve) => { resolveSlow = resolve; });
  const fetcher = async (): Promise<PublicStoriesResult> => {
    calls += 1;
    if (calls === 1) return { state: "ok", stories: [expiring, live] };
    return slow;
  };
  let feed: ReturnType<typeof useHomeStoriesFeed> | null = null;
  function Probe() {
    feed = useHomeStoriesFeed("fa", fetcher);
    return null;
  }
  const tree = await mount(createElement(Probe));
  await waitFor(() => feed?.loaded === true, { timeoutMs: 1000 });
  await new Promise((resolve) => setTimeout(resolve, 55));
  feed!.revalidate();
  await flush();
  assert.deepEqual(feed!.stories.map((row) => row.storyId), ["live"]);
  assert.equal(calls, 2);
  feed!.revalidate();
  feed!.revalidate();
  await flush();
  assert.equal(calls, 2, "an in-flight refresh must suppress duplicate requests");
  resolveSlow!({ state: "ok", stories: [live] });
  await flush();
  await tree.unmount();
});

test("R22 F05: a stale locale response cannot replace the current locale feed", async () => {
  const pending = new Map<string, (value: PublicStoriesResult) => void>();
  const calls: string[] = [];
  const fetcher = (locale: string): Promise<PublicStoriesResult> => {
    calls.push(locale);
    return new Promise((resolve) => pending.set(locale, resolve));
  };
  let feed: ReturnType<typeof useHomeStoriesFeed> | null = null;
  function Probe({ locale }: { locale: string }) {
    feed = useHomeStoriesFeed(locale, fetcher);
    return null;
  }
  const tree = await mount(createElement(Probe, { locale: "fa" }));
  await waitFor(() => calls.length === 1, { timeoutMs: 1000 });
  await tree.rerender(createElement(Probe, { locale: "en" }));
  await waitFor(() => calls.length === 2, { timeoutMs: 1000 });
  pending.get("en")!({ state: "ok", stories: [story("en-live", null)] });
  await flush();
  pending.get("fa")!({ state: "ok", stories: [story("fa-stale", null)] });
  await flush();
  assert.deepEqual(feed!.stories.map((row) => row.storyId), ["en-live"]);
  await tree.unmount();
});
