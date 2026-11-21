"use client";

import { useEffect, useMemo, useState } from "react";
import { Eye, Play } from "lucide-react";
import { Swiper, SwiperSlide } from "swiper/react";
import { Autoplay, FreeMode } from "swiper/modules";
import { useLocale } from "../../../lib/i18n/locale-context.tsx";
import { fetchPublicStories, type PublicStoryCard } from "../../stories/story-api.ts";
import { StoryModal } from "./story-modal.tsx";
import { createFakeStory } from "../../../lib/storefront-composition/preview-fake-data.ts";
import { applyPreviewFill } from "../../../lib/storefront-composition/preview-fill-policy.ts";
import { PreviewSampleBadge } from "../preview-sample-badge.tsx";

import "swiper/css";
import "swiper/css/free-mode";

/**
 * ریل استوری خانه — پورت بصری/تعاملی Shopeiva با دادهٔ زندهٔ Host.
 * دکمهٔ «افزودن استوری» عمداً حذف شده (ادمین می‌سازد).
 */
export type StoryLayout = "circle" | "image-circles" | "rounded-cards" | "icon-shortcuts";

const STORIES_RAIL_COPY: Record<"fa" | "en", { heading: string; shortcuts: string; empty: string; story: string; initialFallback: string; error: string; retry: string }> = {
  fa: {
    heading: "استوری‌ها",
    shortcuts: "میانبرها",
    empty: "استوری فعالی برای نمایش نیست.",
    story: "استوری",
    initialFallback: "س",
    error: "استوری‌ها فعلاً بارگذاری نشدند.",
    retry: "تلاش دوباره",
  },
  en: {
    heading: "Stories",
    shortcuts: "Shortcuts",
    empty: "No active story to show.",
    story: "Story",
    initialFallback: "S",
    error: "Stories could not be loaded.",
    retry: "Try again",
  },
};

const VIDEO_EXT_RE = /\.(mp4|m4v|webm|mov|ogv|ogg)(?:[?#]|$)/i;
/** One bounded revalidation window; a live story rail never polls faster than this. */
const STORY_REVALIDATE_MAX_MS = 5 * 60 * 1000;
const IMAGE_EXT_RE = /\.(jpe?g|png|webp|avif|gif|bmp|svg)(?:[?#]|$)/i;

/**
 * Whether the rendered cover is itself a video. A story that contains a video slide still has its
 * own cover, and when that cover is a JPEG the rail must render it as a photo (no <video> for a still).
 */
function coverLooksLikeVideo(coverUrl: string, itemMediaType: string | undefined): boolean {
  const url = (coverUrl ?? "").trim();
  if (IMAGE_EXT_RE.test(url)) return false;
  if (VIDEO_EXT_RE.test(url)) return true;
  return (itemMediaType ?? "").trim().toLowerCase() === "video";
}

export function HomeStoriesSection({
  layout = "circle",
  storePreviewFill = false,
  previewLocale = "fa",
  previewVariantKey = "story.circle",
}: {
  layout?: StoryLayout;
  /** When true and live stories are empty, render in-memory fake stories (Store preview only). */
  storePreviewFill?: boolean;
  previewLocale?: string;
  previewVariantKey?: string;
} = {}) {
  const locale = useLocale();
  const copy = STORIES_RAIL_COPY[locale === "en" ? "en" : "fa"];
  const [stories, setStories] = useState<PublicStoryCard[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [revalidateTick, setRevalidateTick] = useState(0);
  const [hoveredId, setHoveredId] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedStoryId, setSelectedStoryId] = useState<string | null>(null);
  const [coverErrors, setCoverErrors] = useState<Record<string, boolean>>({});

  useEffect(() => {
    let cancelled = false;
    void fetchPublicStories(locale).then((result) => {
      if (cancelled) return;
      if (result.state === "error") {
        setLoadError(result.message);
        setLoaded(true);
        return;
      }
      setLoadError(null);
      const eligible = result.stories.filter((story) => story.items.length > 0);
      setStories(eligible);
      setLoaded(true);
      // A story that stopped/expired while the tab was open must not stay playable.
      setSelectedStoryId((prev) =>
        prev && eligible.some((story) => story.storyId === prev) ? prev : null,
      );
      if (revalidateTick > 0) return;
      if (typeof window === "undefined") return;
      const wanted = new URLSearchParams(window.location.search).get("story");
      if (wanted && eligible.some((s) => s.storyId === wanted)) {
        setSelectedStoryId(wanted);
        setModalOpen(true);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [locale, revalidateTick]);

  const soonestEnd = useMemo(() => {
    const ends = stories
      .map((story) => (story.endAt ? Date.parse(story.endAt) : Number.NaN))
      .filter((value) => Number.isFinite(value) && value > Date.now());
    return ends.length > 0 ? Math.min(...ends) : Number.NaN;
  }, [stories]);

  // Bounded revalidation: one timer to the nearest publication-window end (never a poll loop),
  // plus a single check when the tab comes back to the foreground.
  useEffect(() => {
    if (typeof window === "undefined" || stories.length === 0) return;
    const delay = Number.isFinite(soonestEnd)
      ? Math.min(Math.max(soonestEnd - Date.now() + 1000, 1000), STORY_REVALIDATE_MAX_MS)
      : STORY_REVALIDATE_MAX_MS;
    const timer = setTimeout(() => setRevalidateTick((v) => v + 1), delay);
    const onVisible = () => {
      if (document.visibilityState === "visible") setRevalidateTick((v) => v + 1);
    };
    document.addEventListener("visibilitychange", onVisible);
    return () => {
      clearTimeout(timer);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [soonestEnd, stories.length]);

  useEffect(() => {
    if (!modalOpen || !selectedStoryId) return;
    if (!stories.some((story) => story.storyId === selectedStoryId)) {
      setSelectedStoryId(null);
      setModalOpen(false);
    }
  }, [stories, modalOpen, selectedStoryId]);
  const displayStories = useMemo(() => {
    if (!storePreviewFill) return stories;
    return applyPreviewFill({
      enabled: true,
      variantKey: previewVariantKey,
      realItems: stories,
      createFake: (index) => createFakeStory(index, previewLocale),
    }).items;
  }, [storePreviewFill, stories, previewLocale, previewVariantKey]);

  const errorNotice = loadError ? (
    <div className="mb-3 flex flex-wrap items-center justify-between gap-2 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900" data-testid="home-stories-error">
      <span>{copy.error}</span>
      <button type="button" className="min-h-11 rounded-lg border border-amber-300 bg-surface px-3 py-1 font-bold" onClick={() => setRevalidateTick((value) => value + 1)}>
        {copy.retry}
      </button>
    </div>
  ) : null;
  if (!loaded && !storePreviewFill) {
    return null;
  }

  if (loadError && displayStories.length === 0 && !storePreviewFill) {
    return (
      <div className="w-full px-2 sm:px-4 py-6 md:py-8 bg-section-surface" data-testid="home-stories" data-story-layout={layout} data-story-error="true" data-storefront-surface-role="section">
        <h3 className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2 mb-4">
          <span className="w-1 h-5 bg-primary rounded-full" />
          {copy.heading}
        </h3>
        <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-5 text-center text-sm text-amber-900">
          <p>{copy.error}</p>
          <button type="button" className="mt-3 min-h-11 rounded-xl border border-amber-300 bg-surface px-4 py-2 font-bold" onClick={() => setRevalidateTick((value) => value + 1)}>
            {copy.retry}
          </button>
        </div>
      </div>
    );
  }
  if (displayStories.length === 0) {
    return (
      <div className="w-full px-2 sm:px-4 py-6 md:py-8 bg-section-surface" data-testid="home-stories" data-story-layout={layout} data-empty="true" data-storefront-surface-role="section">
        <h3 className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2 mb-4">
          <span className="w-1 h-5 bg-primary rounded-full" />
          {copy.heading}
        </h3>
        <p className="rounded-2xl border border-dashed border-gray-200 bg-surface px-4 py-6 text-center text-sm text-gray-500">
          {copy.empty}
        </p>
      </div>
    );
  }

  const openStory = (storyId: string) => {
    setSelectedStoryId(storyId);
    setModalOpen(true);
  };

  if (layout === "icon-shortcuts") {
    return (
      <div className="w-full px-2 sm:px-4 py-6 md:py-8 bg-section-surface" data-testid="home-stories" data-story-layout="icon-shortcuts" data-storefront-surface-role="section">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2">
            <span className="w-1 h-5 bg-primary rounded-full" />
            {copy.shortcuts}
          </h3>
        </div>
        {errorNotice}
        <div className="flex gap-3 overflow-x-auto pb-2">
          {displayStories.map((story) => {
            const cover = story.coverMediaUrl ?? story.items[0]?.mediaUrl;
            const initial = story.title.trim().slice(0, 1) || copy.initialFallback;
            return (
              <button
                key={story.storyId}
                type="button"
                onClick={() => openStory(story.storyId)}
                className="relative flex w-[76px] shrink-0 flex-col items-center gap-2 min-h-11"
              >
                {story.previewFake ? <PreviewSampleBadge locale={previewLocale} className="absolute -top-1 left-0" /> : null}
                <span className="flex h-14 w-14 items-center justify-center overflow-hidden rounded-2xl border border-gray-200 bg-surface shadow-sm">
                  {cover ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={cover} alt="" className="h-full w-full object-cover" />
                  ) : (
                    <span className="text-lg font-black text-primary">{initial}</span>
                  )}
                </span>
                <span className="w-full truncate text-center text-[11px] font-bold text-gray-700">{story.title}</span>
              </button>
            );
          })}
        </div>
        <StoryModal
          isOpen={modalOpen}
          onClose={() => setModalOpen(false)}
          stories={displayStories}
          initialStoryId={selectedStoryId}
        />
      </div>
    );
  }

  return (
    <div className="w-full px-2 sm:px-4 py-6 md:py-8 bg-section-surface" data-testid="home-stories" data-story-layout={layout} data-storefront-surface-role="section">
      <div className="relative">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg md:text-xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <span className="w-1 h-5 bg-primary rounded-full" />
            {copy.heading}
          </h3>
        </div>
        {errorNotice}

        <div className="flex items-center gap-3 pb-4 scrollbar-hide">
          <div className="flex-1 overflow-x-hidden">
            <Swiper
              modules={[Autoplay, FreeMode]}
              slidesPerView="auto"
              spaceBetween={14}
              freeMode={true}
              autoplay={{
                delay: 3000,
                disableOnInteraction: false,
                pauseOnMouseEnter: true,
              }}
              dir={locale === "en" ? "ltr" : "rtl"}
              className="w-full"
            >
              {displayStories.map((story) => {
                const cover = story.coverMediaUrl ?? story.items[0]?.mediaUrl ?? "";
                // Only an exact matching slide can identify the cover; never borrow the first slide.
                const coverItem = story.items.find((item) => (item.mediaUrl ?? "") === cover);
                const coverIsVideo = coverLooksLikeVideo(cover, coverItem?.mediaType);
                const coverFailed = coverErrors[story.storyId] === true;
                const coverInitial = story.title.trim().slice(0, 1) || copy.initialFallback;
                return (
                  <SwiperSlide
                    key={story.storyId}
                    className={layout === "rounded-cards" ? "!w-[120px] my-2 mb-7 md:!w-[140px]" : "!w-[80px] my-2 mb-7 md:!w-[100px]"}
                  >
                    <button
                      type="button"
                      onClick={() => openStory(story.storyId)}
                      className="flex flex-col items-center gap-1.5 group relative w-full min-h-11"
                      onMouseEnter={() => setHoveredId(story.storyId)}
                      onMouseLeave={() => setHoveredId(null)}
                    >
                      {story.previewFake ? <PreviewSampleBadge locale={previewLocale} className="absolute top-0 left-0 z-30" /> : null}
                      <div
                        className={`relative group-hover:scale-105 transition-transform duration-300 bg-gradient-to-tr from-primary via-purple-500 to-pink-500 ${
                          layout === "rounded-cards"
                            ? "w-[120px] h-[140px] md:w-[140px] md:h-[160px] rounded-3xl p-[3px]"
                            : layout === "image-circles"
                              ? "w-[80px] h-[80px] md:w-[100px] md:h-[100px] rounded-full p-[2px] ring-2 ring-offset-2 ring-primary/40"
                              : "w-[80px] h-[80px] md:w-[100px] md:h-[100px] rounded-full p-[3px]"
                        }`}
                      >
                        <div className={`w-full h-full p-[2px] bg-surface dark:bg-zinc-950 ${layout === "rounded-cards" ? "rounded-3xl" : "rounded-full"}`}>
                          <div className={`relative w-full h-full overflow-hidden bg-gray-200 dark:bg-zinc-800 ${layout === "rounded-cards" ? "rounded-[1.25rem]" : "rounded-full"}`}>
                            {coverFailed || !cover ? (
                              <span className="flex h-full w-full items-center justify-center bg-surface text-lg font-black text-primary">{coverInitial}</span>
                            ) : coverIsVideo ? (
                              // eslint-disable-next-line jsx-a11y/media-has-caption
                              <video
                                key={cover}
                                src={cover}
                                className="w-full h-full object-cover"
                                muted
                                loop
                                playsInline
                                autoPlay
                                onError={() => setCoverErrors((prev) => ({ ...prev, [story.storyId]: true }))}
                              />
                            ) : (
                              // eslint-disable-next-line @next/next/no-img-element
                              <img key={cover} src={cover} alt={story.title} className="w-full h-full object-cover" loading="lazy" onError={() => setCoverErrors((prev) => ({ ...prev, [story.storyId]: true }))} />
                            )}

                            {coverIsVideo && !coverFailed ? (
                              <div className="absolute inset-0 flex items-center justify-center bg-black/30 pointer-events-none">
                                <div className="w-8 h-8 md:w-10 md:h-10 rounded-full bg-black/50 backdrop-blur-sm flex items-center justify-center border border-white/30">
                                  <Play className="w-4 h-4 md:w-5 md:h-5 text-white fill-white ml-0.5" />
                                </div>
                              </div>
                            ) : null}
                          </div>
                        </div>
                      </div>

                      <span className="text-[10px] md:text-xs text-gray-600 dark:text-gray-400 font-medium truncate w-[80px] md:w-[100px] text-center group-hover:text-primary transition-colors">
                        {story.shopDisplayName?.trim() || story.title}
                      </span>

                      <div
                        className={`absolute -bottom-6 left-1/2 -translate-x-1/2 bg-black/80 backdrop-blur-sm text-white text-[9px] px-2 py-0.5 rounded-full whitespace-nowrap transition-all duration-300 z-50 ${
                          hoveredId === story.storyId ? "opacity-100 translate-y-0" : "opacity-0 translate-y-1"
                        }`}
                      >
                        <Eye className="w-2.5 h-2.5 inline ml-1" />
                        {copy.story}
                      </div>
                    </button>
                  </SwiperSlide>
                );
              })}
            </Swiper>
          </div>
        </div>

        <div className="w-full h-px bg-gradient-to-r from-transparent via-gray-200 dark:via-zinc-800 to-transparent" />
      </div>

      <StoryModal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        stories={displayStories}
        initialStoryId={selectedStoryId}
      />
    </div>
  );
}
