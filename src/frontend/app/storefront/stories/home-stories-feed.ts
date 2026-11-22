import { useCallback, useEffect, useRef, useState } from "react";
import { fetchPublicStories, type PublicStoriesResult, type PublicStoryCard } from "../../stories/story-api.ts";
import { filterLiveStories } from "./home-stories-revalidation.ts";

type Fetcher = (locale: string) => Promise<PublicStoriesResult>;

export function useHomeStoriesFeed(locale: string, fetcher: Fetcher = fetchPublicStories) {
  const [stories, setStories] = useState<PublicStoryCard[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const requestSeq = useRef(0);
  const activeLocale = useRef<string | null>(null);
  const mounted = useRef(true);
  const loadedOnce = useRef(false);

  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);

  const load = useCallback(async () => {
    if (activeLocale.current === locale) return;
    const requestId = ++requestSeq.current;
    activeLocale.current = locale;
    try {
      const result = await fetcher(locale);
      if (!mounted.current || requestId !== requestSeq.current) return;
      if (result.state === "error") {
        setLoadError(result.message);
        setStories((previous) => filterLiveStories(previous, Date.now()));
        setLoaded(true);
        return;
      }
      setLoadError(null);
      const eligible = filterLiveStories(
        result.stories.filter((story) => story.items.length > 0),
        Date.now(),
      );
      setStories(eligible);
      setLoaded(true);
      loadedOnce.current = true;
    } finally {
      if (requestId === requestSeq.current) activeLocale.current = null;
    }
  }, [fetcher, locale]);

  useEffect(() => { void load(); }, [load]);

  const revalidate = useCallback(() => {
    // Expiry is local truth: remove ended stories before the network can answer.
    setStories((previous) => filterLiveStories(previous, Date.now()));
    void load();
  }, [load]);

  return { stories, loaded, loadError, revalidate, loadedOnce: loadedOnce.current };
}
