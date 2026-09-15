"use client";

import { useEffect, useState } from "react";
import { Eye, Play } from "lucide-react";
import { Swiper, SwiperSlide } from "swiper/react";
import { Autoplay, FreeMode } from "swiper/modules";
import { useLocale } from "../../../lib/i18n/locale-context.tsx";
import { fetchPublicStories, type PublicStoryCard } from "../../stories/story-api.ts";
import { StoryModal } from "./story-modal.tsx";

import "swiper/css";
import "swiper/css/free-mode";

/**
 * ریل استوری خانه — پورت بصری/تعاملی Shopeiva با دادهٔ زندهٔ Host.
 * دکمهٔ «افزودن استوری» عمداً حذف شده (ادمین می‌سازد).
 */
export type StoryLayout = "circle" | "image-circles" | "rounded-cards" | "icon-shortcuts";

export function HomeStoriesSection({ layout = "circle" }: { layout?: StoryLayout } = {}) {
  const locale = useLocale();
  const [stories, setStories] = useState<PublicStoryCard[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [hoveredId, setHoveredId] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedStoryId, setSelectedStoryId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    void fetchPublicStories(locale).then((rows) => {
      if (cancelled) return;
      setStories(rows.filter((story) => story.items.length > 0));
      setLoaded(true);
    });
    return () => {
      cancelled = true;
    };
  }, [locale]);

  if (!loaded) {
    return null;
  }

  if (stories.length === 0) {
    return (
      <div className="w-full px-2 sm:px-4 py-6 md:py-8 bg-section-surface" data-testid="home-stories" data-story-layout={layout} data-empty="true" data-storefront-surface-role="section">
        <h3 className="text-lg md:text-xl font-bold text-gray-900 flex items-center gap-2 mb-4">
          <span className="w-1 h-5 bg-primary rounded-full" />
          استوری‌ها
        </h3>
        <p className="rounded-2xl border border-dashed border-gray-200 bg-surface px-4 py-6 text-center text-sm text-gray-500">
          استوری فعالی برای نمایش نیست.
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
            میانبرها
          </h3>
        </div>
        <div className="flex gap-3 overflow-x-auto pb-2">
          {stories.map((story) => {
            const cover = story.coverMediaUrl ?? story.items[0]?.mediaUrl;
            const initial = story.title.trim().slice(0, 1) || "س";
            return (
              <button
                key={story.storyId}
                type="button"
                onClick={() => openStory(story.storyId)}
                className="flex w-[76px] shrink-0 flex-col items-center gap-2 min-h-11"
              >
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
          stories={stories}
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
            استوری‌ها
          </h3>
        </div>

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
              dir="rtl"
              className="w-full"
            >
              {stories.map((story) => {
                const cover = story.coverMediaUrl ?? story.items[0]?.mediaUrl ?? "/images/stories/1.jpg";
                const isVideo = story.isVideo || story.items.some((item) => item.mediaType === "video");
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
                            {isVideo ? (
                              // eslint-disable-next-line jsx-a11y/media-has-caption
                              <video
                                src={cover}
                                className="w-full h-full object-cover"
                                muted
                                loop
                                playsInline
                                autoPlay
                                onError={(e) => {
                                  const target = e.currentTarget;
                                  target.style.display = "none";
                                  const parent = target.parentElement;
                                  if (!parent) return;
                                  const img = document.createElement("img");
                                  img.src = "/images/stories/1.jpg";
                                  img.className = "w-full h-full object-cover";
                                  img.alt = story.title;
                                  parent.appendChild(img);
                                }}
                              />
                            ) : (
                              // eslint-disable-next-line @next/next/no-img-element
                              <img src={cover} alt={story.title} className="w-full h-full object-cover" loading="lazy" />
                            )}

                            {isVideo ? (
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
                        {story.title}
                      </span>

                      <div
                        className={`absolute -bottom-6 left-1/2 -translate-x-1/2 bg-black/80 backdrop-blur-sm text-white text-[9px] px-2 py-0.5 rounded-full whitespace-nowrap transition-all duration-300 z-50 ${
                          hoveredId === story.storyId ? "opacity-100 translate-y-0" : "opacity-0 translate-y-1"
                        }`}
                      >
                        <Eye className="w-2.5 h-2.5 inline ml-1" />
                        استوری
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
        stories={stories}
        initialStoryId={selectedStoryId}
      />
    </div>
  );
}
