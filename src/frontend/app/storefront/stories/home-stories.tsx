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
export function HomeStoriesSection() {
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

  if (!loaded || stories.length === 0) {
    return null;
  }

  const openStory = (storyId: string) => {
    setSelectedStoryId(storyId);
    setModalOpen(true);
  };

  return (
    <div className="w-full px-2 sm:px-4 py-2" data-testid="home-stories">
      <div className="relative">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg md:text-xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <span className="w-1 h-5 bg-[#E53935] rounded-full" />
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
                  <SwiperSlide key={story.storyId} className="!w-[80px] my-2 mb-7 md:!w-[100px]">
                    <button
                      type="button"
                      onClick={() => openStory(story.storyId)}
                      className="flex flex-col items-center gap-1.5 group relative w-full"
                      onMouseEnter={() => setHoveredId(story.storyId)}
                      onMouseLeave={() => setHoveredId(null)}
                    >
                      <div className="relative w-[80px] h-[80px] md:w-[100px] md:h-[100px] rounded-full p-[3px] group-hover:scale-105 transition-transform duration-300 bg-gradient-to-tr from-[#E53935] via-purple-500 to-pink-500">
                        <div className="w-full h-full rounded-full p-[2px] bg-white dark:bg-zinc-950">
                          <div className="relative w-full h-full rounded-full overflow-hidden bg-gray-200 dark:bg-zinc-800">
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

                      <span className="text-[10px] md:text-xs text-gray-600 dark:text-gray-400 font-medium truncate w-[80px] md:w-[100px] text-center group-hover:text-[#E53935] transition-colors">
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
