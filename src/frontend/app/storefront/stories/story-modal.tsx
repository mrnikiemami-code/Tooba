"use client";

import { useCallback, useEffect, useRef, useState, type MouseEvent, type TouchEvent } from "react";
import {
  Camera,
  ChevronLeft,
  ChevronRight,
  Clock,
  Share2,
  Volume2,
  VolumeX,
  X,
} from "lucide-react";
import { useLocalizedPath } from "../../../lib/i18n/locale-context.tsx";
import type { PublicStoryCard, PublicStoryItem } from "../../stories/story-api.ts";

const toPersianDigits = (n: number | string | null | undefined) => {
  if (n === null || n === undefined) return "";
  return String(n).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number.parseInt(d, 10)]!);
};

function imageProgressStep(item: PublicStoryItem | undefined): number {
  const durationMs = item?.durationMs;
  if (durationMs != null && durationMs > 0) {
    return 5000 / durationMs;
  }
  return 1;
}

const VIDEO_URL_RE = /\.(mp4|m4v|webm|mov|ogv|ogg)(?:[?#]|$)/i;

/**
 * Video detection belongs to the slide, not the story: a story that contains a video slide
 * still has a JPEG cover, and that cover must dwell like a photo (not wait for `ended`).
 */
function itemIsVideo(item: PublicStoryItem | undefined): boolean {
  const type = (item?.mediaType ?? "").trim().toLowerCase();
  if (type === "video") return true;
  if (type === "image") return false;
  return VIDEO_URL_RE.test((item?.mediaUrl ?? "").trim());
}

function isExternalHttp(url: string): boolean {
  try {
    const parsed = new URL(url);
    return parsed.protocol === "http:" || parsed.protocol === "https:";
  } catch {
    return false;
  }
}

function currentCta(
  item: PublicStoryItem | undefined,
  story: PublicStoryCard | undefined,
): { type: string; target: string; href: string | null } | null {
  if (item?.ctaTarget && item.ctaType && item.ctaType !== "none") {
    return { type: item.ctaType, target: item.ctaTarget, href: item.ctaHref ?? null };
  }
  if (story?.ctaTarget && story.ctaType && story.ctaType !== "none") {
    return { type: story.ctaType, target: story.ctaTarget, href: story.ctaHref ?? null };
  }
  return null;
}

function shopHref(story: PublicStoryCard | undefined, localizePath: (path: string) => string): string | null {
  const publicId = story?.shopPublicId?.trim();
  if (!publicId) return null;
  return localizePath(`/seller-profile/${encodeURIComponent(publicId)}`);
}

/**
 * مودال استوری Shopeiva — کروم بصری حفظ‌شده؛ بدون لایک/کامنت/ذخیرهٔ جعلی؛ پیشرفت ویدیو از metadata واقعی.
 */
export function StoryModal({
  isOpen,
  onClose,
  stories,
  initialStoryId,
}: {
  isOpen: boolean;
  onClose: () => void;
  stories: PublicStoryCard[];
  initialStoryId: string | null;
}) {
  const localizePath = useLocalizedPath();
  const [currentStoryIndex, setCurrentStoryIndex] = useState(0);
  const [currentItemIndex, setCurrentItemIndex] = useState(0);
  const [isMuted, setIsMuted] = useState(true);
  const [progress, setProgress] = useState(0);
  const [isPaused, setIsPaused] = useState(false);
  const [isHidden, setIsHidden] = useState(false);
  const [imageLoaded, setImageLoaded] = useState(false);
  const [mediaError, setMediaError] = useState(false);
  const [shareNotice, setShareNotice] = useState<string | null>(null);
  const dialogRef = useRef<HTMLDivElement | null>(null);
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);

  const videoRef = useRef<HTMLVideoElement | null>(null);
  const progressTimer = useRef<ReturnType<typeof setInterval> | null>(null);
  const startX = useRef(0);
  const isDragging = useRef(false);

  const currentStory = stories[currentStoryIndex];
  const items = currentStory?.items?.length ? currentStory.items : [];
  const currentItem = items[currentItemIndex];
  const totalStories = stories.length;
  const isVideo = itemIsVideo(currentItem);
  const isHeld = isPaused || isHidden;
  const mediaUrl = currentItem?.mediaUrl ?? currentStory?.coverMediaUrl ?? "";
  const avatarUrl = currentStory?.coverMediaUrl ?? items[0]?.mediaUrl ?? "";
  const shopName = currentStory?.shopDisplayName?.trim() || null;
  const shopLink = shopHref(currentStory, localizePath);

  const goToNextItem = useCallback(() => {
    setIsPaused(false);
    setProgress(0);
    setShareNotice(null);
    if (currentItemIndex < items.length - 1) {
      setCurrentItemIndex((prev) => prev + 1);
      return;
    }
    if (currentStoryIndex < totalStories - 1) {
      setCurrentStoryIndex((prev) => prev + 1);
      setCurrentItemIndex(0);
      return;
    }
    onClose();
  }, [currentItemIndex, items.length, currentStoryIndex, totalStories, onClose]);

  const goToPrevItem = useCallback(() => {
    setIsPaused(false);
    setProgress(0);
    setShareNotice(null);
    if (currentItemIndex > 0) {
      setCurrentItemIndex((prev) => prev - 1);
      return;
    }
    if (currentStoryIndex > 0) {
      const prevStory = stories[currentStoryIndex - 1];
      const prevItems = prevStory?.items ?? [];
      setCurrentStoryIndex((prev) => prev - 1);
      setCurrentItemIndex(Math.max(0, prevItems.length - 1));
    }
  }, [currentItemIndex, currentStoryIndex, stories]);

  useEffect(() => {
    if (isOpen && initialStoryId) {
      const index = stories.findIndex((s) => s.storyId === initialStoryId);
      if (index !== -1) {
        setCurrentStoryIndex(index);
        setCurrentItemIndex(0);
      }
    }
  }, [isOpen, initialStoryId, stories]);

  useEffect(() => {
    setProgress(0);
    setIsPaused(false);
    setImageLoaded(false);
    setMediaError(false);
    setShareNotice(null);
    if (videoRef.current) {
      videoRef.current.currentTime = 0;
      videoRef.current.muted = isMuted;
      void videoRef.current.play().catch(() => undefined);
    }
    // Intentionally omit isMuted — mute toggle must not reset progress.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentStoryIndex, currentItemIndex, currentStory]);

  useEffect(() => {
    if (!isOpen) return;
    const onVisibility = () => setIsHidden(document.visibilityState === "hidden");
    onVisibility();
    document.addEventListener("visibilitychange", onVisibility);
    return () => document.removeEventListener("visibilitychange", onVisibility);
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    closeButtonRef.current?.focus();
    return () => {
      document.body.style.overflow = previousOverflow;
      previousFocus?.focus?.();
    };
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen || !currentStory || items.length === 0) return;
    if (isHeld) {
      if (progressTimer.current) clearInterval(progressTimer.current);
      if (isVideo && videoRef.current) videoRef.current.pause();
      return;
    }

    if (isVideo) {
      if (progressTimer.current) clearInterval(progressTimer.current);
      const video = videoRef.current;
      if (video) {
        void video.play().catch(() => undefined);
        const onTime = () => {
          const duration = video.duration;
          if (!Number.isFinite(duration) || duration <= 0) return;
          setProgress(Math.min(100, (video.currentTime / duration) * 100));
        };
        const onEnded = () => {
          setProgress(100);
          goToNextItem();
        };
        video.addEventListener("timeupdate", onTime);
        video.addEventListener("ended", onEnded);
        return () => {
          video.removeEventListener("timeupdate", onTime);
          video.removeEventListener("ended", onEnded);
        };
      }
      return;
    }

    // A photo's dwell starts only after the image actually painted; otherwise a slow or
    // failed load would consume the slide's time and jump to the next item.
    if (!imageLoaded) return;

    const step = imageProgressStep(currentItem);
    progressTimer.current = setInterval(() => {
      setProgress((prev) => {
        if (prev >= 100) {
          if (progressTimer.current) clearInterval(progressTimer.current);
          goToNextItem();
          return 0;
        }
        return prev + step;
      });
    }, 50);

    return () => {
      if (progressTimer.current) clearInterval(progressTimer.current);
    };
  }, [isOpen, isHeld, isVideo, imageLoaded, currentStoryIndex, currentItemIndex, currentItem, currentStory, items.length, goToNextItem]);

  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "ArrowLeft") goToPrevItem();
      if (e.key === "ArrowRight") goToNextItem();
      if (e.key === " ") {
        e.preventDefault();
        setIsPaused((prev) => !prev);
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose, goToNextItem, goToPrevItem]);

  const toggleMute = () => {
    setIsMuted((prev) => !prev);
    if (videoRef.current) videoRef.current.muted = !isMuted;
  };

  const handleShare = async () => {
    const storyId = currentStory?.storyId;
    if (!storyId) return;
    const url = new URL(window.location.href);
    url.searchParams.set("story", storyId);
    const shareUrl = url.toString();
    if (navigator.share) {
      try {
        await navigator.share({
          title: currentStory?.title ?? "استوری توبا",
          text: shopName ? `${shopName} · ${currentStory?.title ?? ""}` : currentStory?.title ?? "استوری توبا",
          url: shareUrl,
        });
        return;
      } catch (error) {
        if (error instanceof Error && error.name === "AbortError") return;
      }
    }
    try {
      await navigator.clipboard.writeText(shareUrl);
      setShareNotice("لینک کپی شد");
    } catch {
      setShareNotice(shareUrl);
    }
  };

  const handleCta = (e: MouseEvent) => {
    e.stopPropagation();
    const cta = currentCta(currentItem, currentStory);
    if (!cta) return;
    const resolved = (cta.href ?? cta.target).trim();
    if (!resolved || /^javascript:/i.test(resolved) || /^data:/i.test(resolved) || /^vbscript:/i.test(resolved)) {
      return;
    }
    if (cta.type === "external" || isExternalHttp(resolved)) {
      if (!isExternalHttp(resolved)) return;
      window.location.href = resolved;
      return;
    }
    // Product Guid without Catalog-resolved href must not navigate silently.
    if (/^[0-9a-fA-F-]{36}$/.test(resolved)) {
      return;
    }
    const path = resolved.startsWith("/") ? resolved : `/${resolved}`;
    window.location.href = localizePath(path);
  };

  const handleTouchStart = (e: TouchEvent) => {
    startX.current = e.touches[0]!.clientX;
    isDragging.current = true;
  };

  const handleTouchEnd = (e: TouchEvent) => {
    if (!isDragging.current) return;
    const diff = startX.current - e.changedTouches[0]!.clientX;
    if (Math.abs(diff) > 50) {
      if (diff > 0) goToNextItem();
      else goToPrevItem();
    }
    isDragging.current = false;
  };

  if (!isOpen || !currentStory || items.length === 0) return null;

  const cta = currentCta(currentItem, currentStory);

  return (
    <div
      ref={dialogRef}
      role="dialog"
      aria-modal="true"
      aria-label={currentStory.title || "استوری"}
      data-testid="story-modal"
      className="fixed inset-0 z-[200] bg-black flex items-center justify-center p-2 sm:p-4"
    >
      <div
        className="relative w-full max-w-4xl h-[90vh] max-h-[750px] bg-black rounded-3xl overflow-hidden shadow-2xl"
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
        onClick={(e) => {
          const rect = e.currentTarget.getBoundingClientRect();
          const x = e.clientX - rect.left;
          if (x > (rect.width * 2) / 3) goToPrevItem();
          else if (x < rect.width / 3) goToNextItem();
          else setIsPaused((prev) => !prev);
        }}
      >
        <div className="absolute top-0 left-0 right-0 z-20 flex gap-1 p-2">
          {items.map((item, index) => (
            <div key={item.storyItemId} className="flex-1 h-1 bg-surface/20 rounded-full overflow-hidden">
              <div
                className="h-full bg-surface transition-all duration-100 rounded-full"
                style={{
                  width: index === currentItemIndex ? `${progress}%` : index < currentItemIndex ? "100%" : "0%",
                }}
              />
            </div>
          ))}
        </div>

        <button
          ref={closeButtonRef}
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onClose();
          }}
          className="absolute top-3 right-3 z-30 w-10 h-10 bg-black/50 backdrop-blur-sm rounded-full flex items-center justify-center text-white hover:bg-black/70 transition-all border border-white/10"
          aria-label="بستن"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="w-full h-full relative flex items-center justify-center">
          {isVideo ? (
            // eslint-disable-next-line jsx-a11y/media-has-caption
            <video
              ref={videoRef}
              src={mediaUrl}
              className="w-full h-full object-contain"
              muted={isMuted}
              playsInline
              autoPlay
              onError={() => setMediaError(true)}
              onLoadedData={() => setMediaError(false)}
            />
          ) : (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={mediaUrl}
              alt={currentStory.title}
              className="w-full h-full object-contain"
              onError={() => setMediaError(true)}
              onLoad={() => {
                setMediaError(false);
                setImageLoaded(true);
              }}
            />
          )}

          {mediaError ? (
            <div className="w-full h-full flex items-center justify-center text-white/30 absolute inset-0 bg-black/60">
              <div className="text-center px-4">
                <Camera className="w-16 h-16 mx-auto mb-4 opacity-50" />
                <p className="text-sm text-white/80">نمایش این بخش ممکن نشد</p>
                <button
                  type="button"
                  className="mt-4 min-h-11 rounded-full bg-primary px-4 py-2 text-sm font-bold text-white"
                  onClick={(e) => {
                    e.stopPropagation();
                    goToNextItem();
                  }}
                >
                  برو به اسلاید بعد
                </button>
              </div>
            </div>
          ) : null}

          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              goToPrevItem();
            }}
            className="absolute right-2 top-1/2 -translate-y-1/2 z-20 w-10 h-10 bg-black/30 backdrop-blur-sm rounded-full flex items-center justify-center text-white hover:bg-black/50 transition-all border border-white/10"
            aria-label="قبلی"
          >
            <ChevronRight className="w-5 h-5" />
          </button>
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              goToNextItem();
            }}
            className="absolute left-2 top-1/2 -translate-y-1/2 z-20 w-10 h-10 bg-black/30 backdrop-blur-sm rounded-full flex items-center justify-center text-white hover:bg-black/50 transition-all border border-white/10"
            aria-label="بعدی"
          >
            <ChevronLeft className="w-5 h-5" />
          </button>

          <div className="absolute top-12 left-4 right-4 z-20 flex items-center gap-3">
            <div className="w-10 h-10 rounded-full overflow-hidden border-2 border-primary bg-black/40">
              {avatarUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={avatarUrl} alt="" width={40} height={40} className="object-cover w-full h-full" loading="lazy" />
              ) : null}
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className="text-white font-bold text-sm truncate">{currentStory.title}</span>
              </div>
              <div className="flex items-center gap-2 text-white/70 text-xs">
                <Clock className="w-3 h-3 shrink-0" />
                {shopLink && shopName ? (
                  <a
                    href={shopLink}
                    className="truncate underline-offset-2 hover:underline"
                    data-testid="story-shop-link"
                    onClick={(e) => e.stopPropagation()}
                  >
                    {shopName}
                  </a>
                ) : (
                  <span className="truncate" data-testid="story-shop-name">
                    {shopName ?? "ویترین اصلی"}
                  </span>
                )}
              </div>
            </div>
          </div>

          {currentItem?.caption?.trim() ? (
            <p
              className="absolute bottom-32 left-4 right-4 z-20 text-center text-sm font-medium text-white drop-shadow-[0_1px_3px_rgba(0,0,0,0.8)]"
              data-testid="story-caption"
            >
              {currentItem.caption.trim()}
            </p>
          ) : null}

          {cta ? (
            <div className="absolute bottom-20 left-4 right-4 z-20 flex justify-center">
              <button
                type="button"
                onClick={handleCta}
                className="px-6 py-2.5 rounded-full bg-primary text-white text-sm font-bold shadow-lg hover:bg-primary-strong transition-colors min-h-11"
              >
                مشاهده
              </button>
            </div>
          ) : null}

          <div className="absolute bottom-4 left-4 right-4 z-20 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  void handleShare();
                }}
                className="p-2 rounded-full bg-black/30 backdrop-blur-sm hover:bg-black/50 transition-all border border-white/10 min-h-11 min-w-11 flex items-center justify-center"
                aria-label="اشتراک"
                data-testid="story-share"
              >
                <Share2 className="w-6 h-6 text-white" />
              </button>

              {isVideo ? (
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    toggleMute();
                  }}
                  className="p-2 rounded-full bg-black/30 backdrop-blur-sm hover:bg-black/50 transition-all border border-white/10 min-h-11 min-w-11 flex items-center justify-center"
                  aria-label={isMuted ? "صدا روشن" : "صدا خاموش"}
                >
                  {isMuted ? <VolumeX className="w-6 h-6 text-white" /> : <Volume2 className="w-6 h-6 text-white" />}
                </button>
              ) : null}
            </div>
            {shareNotice ? (
              <span className="text-xs text-white/80 bg-black/50 rounded-full px-3 py-1" data-testid="story-share-notice">
                {shareNotice}
              </span>
            ) : null}
          </div>

          <div className="absolute top-16 right-1/2 translate-x-1/2 z-20 bg-black/50 backdrop-blur-sm px-3 py-1 rounded-full text-white/60 text-xs">
            {toPersianDigits(currentStoryIndex + 1)} / {toPersianDigits(totalStories)}
          </div>
        </div>
      </div>
    </div>
  );
}
