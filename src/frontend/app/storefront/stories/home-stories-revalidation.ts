import { useCallback, useEffect, useMemo, useState } from "react";
import type { PublicStoryCard } from "../../stories/story-api.ts";

export const STORY_REVALIDATE_MAX_MS = 5 * 60 * 1000;

export function storyIsLiveAt(story: PublicStoryCard, nowMs: number): boolean {
  if (!story.endAt) return true;
  const end = Date.parse(story.endAt);
  return !Number.isFinite(end) || end > nowMs;
}

export function filterLiveStories(stories: readonly PublicStoryCard[], nowMs: number): PublicStoryCard[] {
  return stories.filter((story) => storyIsLiveAt(story, nowMs));
}

export function storyRevalidationDelay(soonestEndMs: number, nowMs: number, maxMs = STORY_REVALIDATE_MAX_MS): number {
  return Number.isFinite(soonestEndMs)
    ? Math.min(Math.max(soonestEndMs - nowMs + 1000, 1000), maxMs)
    : maxMs;
}

export function useStoryRevalidation(
  stories: readonly PublicStoryCard[],
  onRevalidate: () => void,
  maxMs = STORY_REVALIDATE_MAX_MS,
): void {
  const [cycle, setCycle] = useState(0);
  const soonestEnd = useMemo(() => {
    const ends = stories
      .map((story) => (story.endAt ? Date.parse(story.endAt) : Number.NaN))
      .filter((value) => Number.isFinite(value));
    return ends.length > 0 ? Math.min(...ends) : Number.NaN;
  }, [stories]);

  const tick = useCallback(() => {
    onRevalidate();
    setCycle((value) => value + 1);
  }, [onRevalidate]);

  useEffect(() => {
    if (typeof window === "undefined" || stories.length === 0) return;
    const timer = setTimeout(tick, storyRevalidationDelay(soonestEnd, Date.now(), maxMs));
    const onVisible = () => {
      if (document.visibilityState === "visible") tick();
    };
    document.addEventListener("visibilitychange", onVisible);
    return () => {
      clearTimeout(timer);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [cycle, maxMs, soonestEnd, stories.length, tick]);
}
