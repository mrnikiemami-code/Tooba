"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { ImagePlus, Pause, Play, Volume2, VolumeX } from "lucide-react";
import { StoryEmptyState } from "./story-management-ui";
import type { StoryCopy } from "./story-management-copy";
import type { AdminStoryItem } from "../story-api";
import { useLocalizedPath } from "../../../lib/i18n/locale-context.tsx";

function isVideoItem(item: AdminStoryItem | undefined): boolean {
  return (item?.mediaType ?? "").trim().toLowerCase() === "video";
}

function photoDurationMs(item: AdminStoryItem | undefined): number {
  const duration = item?.durationMs;
  return duration != null && duration > 0 ? duration : 5000;
}

function destinationLabel(item: AdminStoryItem, copy: StoryCopy): string {
  const type = (item.ctaType ?? "").trim().toLowerCase();
  if (type === "product") return item.ctaProductTitle?.trim() || copy.destinationProduct;
  if (type === "external") return copy.destinationExternal;
  if (type === "shop" || type === "seller") return copy.destinationShop;
  if (type === "internal" || type === "category" || type === "article") return copy.destinationInternal;
  return copy.destinationUnavailable;
}

function destinationHref(item: AdminStoryItem, localizePath: (path: string) => string): string | null {
  const type = (item.ctaType ?? "").trim().toLowerCase();
  if (!type || type === "none") return null;
  const raw = (item.ctaHref ?? item.ctaTarget ?? "").trim();
  if (!raw) return null;
  if (/^(javascript|data|vbscript):/i.test(raw)) return null;
  if (type === "external") return /^https?:\/\//i.test(raw) ? raw : null;
  if (!item.ctaHref && /^[0-9a-f-]{36}$/i.test(raw)) return null;
  const path = raw.startsWith("/") ? raw : `/${raw}`;
  return /^\/(fa|en)(\/|$)/.test(path) ? path : localizePath(path);
}

export function StoryPreviewPlayer({
  items,
  copy,
  numberLocale,
  testId,
}: {
  items: AdminStoryItem[];
  copy: StoryCopy;
  numberLocale: string;
  testId: string;
}) {
  const [index, setIndex] = useState(0);
  const [progress, setProgress] = useState(0);
  const [paused, setPaused] = useState(false);
  const [hidden, setHidden] = useState(false);
  const [muted, setMuted] = useState(true);
  const [imageLoaded, setImageLoaded] = useState(false);
  const [mediaError, setMediaError] = useState(false);
  const [mediaReloadKey, setMediaReloadKey] = useState(0);
  const localizePath = useLocalizedPath();
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const current = items[index];
  const video = isVideoItem(current);
  const ctaHref = current ? destinationHref(current, localizePath) : null;

  useEffect(() => {
    setIndex((value) => Math.min(value, Math.max(0, items.length - 1)));
    setProgress(0);
    setImageLoaded(false);
    setMediaError(false);
    setPaused(false);
  }, [items]);

  useEffect(() => {
    const onVisibility = () => setHidden(document.visibilityState === "hidden");
    onVisibility();
    document.addEventListener("visibilitychange", onVisibility);
    return () => document.removeEventListener("visibilitychange", onVisibility);
  }, []);

  const next = useCallback(() => {
    setProgress(0);
    setImageLoaded(false);
    setMediaError(false);
    if (items.length <= 1) {
      setProgress(100);
      setPaused(true);
      return;
    }
    setIndex((value) => (value + 1) % items.length);
  }, [items.length]);

  const previous = useCallback(() => {
    setProgress(0);
    setImageLoaded(false);
    setMediaError(false);
    if (items.length <= 1) {
      setPaused(false);
      setMediaReloadKey((value) => value + 1);
      return;
    }
    setIndex((value) => (value - 1 + items.length) % items.length);
  }, [items.length]);

  const togglePaused = useCallback(() => {
    if (paused && progress >= 100) {
      setProgress(0);
      setImageLoaded(false);
      setMediaError(false);
      setMediaReloadKey((value) => value + 1);
      setPaused(false);
      return;
    }
    setPaused((value) => !value);
  }, [paused, progress]);
  useEffect(() => {
    if (!current || mediaError) return;
    if (paused || hidden) {
      if (video && videoRef.current) videoRef.current.pause();
      return;
    }

    if (video) {
      const element = videoRef.current;
      if (!element) return;
      void element.play()?.catch(() => undefined);
      const onTime = () => {
        const duration = element.duration;
        if (!Number.isFinite(duration) || duration <= 0) return;
        setProgress(Math.min(100, (element.currentTime / duration) * 100));
      };
      const onEnded = () => {
        setProgress(100);
        next();
      };
      element.addEventListener("timeupdate", onTime);
      element.addEventListener("ended", onEnded);
      return () => {
        element.removeEventListener("timeupdate", onTime);
        element.removeEventListener("ended", onEnded);
      };
    }

    if (!imageLoaded || mediaError) return;
    const duration = photoDurationMs(current);
    const step = duration > 0 ? 100 / (duration / 50) : 100;
    timerRef.current = setInterval(() => {
      setProgress((value) => {
        if (value >= 100) {
          next();
          return 0;
        }
        return Math.min(100, value + step);
      });
    }, 50);
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
      timerRef.current = null;
    };
  }, [current, hidden, imageLoaded, mediaError, mediaReloadKey, next, paused, video]);

  useEffect(() => {
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  if (items.length === 0 || !current) {
    return (
      <aside className="rounded-xl border border-border bg-background/40 p-4" data-testid={testId}>
        <h3 className="mb-3 text-sm font-bold text-foreground">{copy.previewHeading}</h3>
        <StoryEmptyState
          compact
          title={copy.previewEmpty}
          description={copy.slidesEmptyHint}
          icon={<ImagePlus className="h-5 w-5" aria-hidden="true" />}
        />
      </aside>
    );
  }

  return (
    <aside className="rounded-xl border border-border bg-background/40 p-4" data-testid={testId}>
      <div className="mb-3 flex items-center justify-between gap-2">
        <h3 className="text-sm font-bold text-foreground">{copy.previewHeading}</h3>
        <div className="flex items-center gap-1">
          <button
            type="button"
            className="inline-flex min-h-9 min-w-9 items-center justify-center rounded-full border border-border bg-surface"
            aria-label={paused ? copy.previewPlay : copy.previewPause}
            title={paused ? copy.previewPlay : copy.previewPause}
            onClick={togglePaused}
          >
            {paused ? <Play className="h-4 w-4" aria-hidden="true" /> : <Pause className="h-4 w-4" aria-hidden="true" />}
          </button>
          {video ? (
            <button
              type="button"
              className="inline-flex min-h-9 min-w-9 items-center justify-center rounded-full border border-border bg-surface"
              aria-label={muted ? copy.previewUnmute : copy.previewMute}
              title={muted ? copy.previewUnmute : copy.previewMute}
              onClick={() => setMuted((value) => !value)}
              data-testid="story-preview-mute"
            >
              {muted ? <VolumeX className="h-4 w-4" aria-hidden="true" /> : <Volume2 className="h-4 w-4" aria-hidden="true" />}
            </button>
          ) : null}
        </div>
      </div>

      <div
        className="relative overflow-hidden rounded-xl border border-border bg-black"
        data-testid="story-preview-player"
        data-preview-index={index + 1}
        data-preview-paused={paused ? "true" : "false"}
      >
        <div className="absolute inset-x-0 top-0 z-10 flex gap-1 p-2">
          {items.map((item, itemIndex) => (
            <div key={item.storyItemId} className="h-1 flex-1 overflow-hidden rounded-full bg-white/25">
              <div
                className="h-full rounded-full bg-white transition-all duration-100"
                style={{
                  width:
                    itemIndex === index
                      ? `${progress}%`
                      : itemIndex < index
                        ? "100%"
                        : "0%",
                }}
              />
            </div>
          ))}
        </div>

        {video ? (
          // eslint-disable-next-line jsx-a11y/media-has-caption
          <video
            ref={videoRef}
            key={`${current.storyItemId}:${mediaReloadKey}`}
            src={current.mediaUrl ?? undefined}
            className="aspect-[9/16] max-h-[22rem] w-full object-contain"
            muted={muted}
            playsInline
            autoPlay
            onError={() => setMediaError(true)}
            data-testid="story-preview-video"
          />
        ) : (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            key={`${current.storyItemId}:${mediaReloadKey}`}
            src={current.mediaUrl ?? ""}
            alt={current.caption ?? ""}
            className="aspect-[9/16] max-h-[22rem] w-full object-contain"
            onLoad={() => setImageLoaded(true)}
            onError={() => { setMediaError(true); setImageLoaded(true); }}
            data-testid="story-preview-image"
          />
        )}

        {mediaError ? (
          <div className="pointer-events-none absolute inset-0 flex items-center justify-center bg-black/60 p-4 text-center" data-testid="story-preview-media-error">
            <p className="text-sm font-bold text-white">{copy.previewMediaError}</p>
          </div>
        ) : null}

        <div className="pointer-events-none absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/80 to-transparent p-3 pt-10">
          <p className="text-xs font-bold text-white/80">
            {(index + 1).toLocaleString(numberLocale)} / {items.length.toLocaleString(numberLocale)} ·{" "}
            {video ? copy.videoLabel : copy.photoLabel}
          </p>
          {current.caption ? (
            <p className="mt-1 text-sm text-white" data-testid="story-preview-caption">
              {current.caption}
            </p>
          ) : null}
          {current.ctaType !== "none" ? (
            <div className="pointer-events-auto mt-2 flex flex-wrap items-center gap-2" data-testid="story-preview-cta-destination">
              <span className="text-xs text-white/75">
                {copy.destinationPrefix} {destinationLabel(current, copy)}
              </span>
              {ctaHref ? (
                <a
                  href={ctaHref}
                  className="rounded-lg bg-white px-2.5 py-1 text-xs font-bold text-black"
                  data-testid="story-preview-cta"
                >
                  {copy.previewCta}
                </a>
              ) : (
                <span className="text-xs font-bold text-amber-300">{copy.destinationUnavailable}</span>
              )}
            </div>
          ) : null}
        </div>

        <button
          type="button"
          className="absolute right-1 top-1/2 z-20 flex min-h-9 min-w-9 -translate-y-1/2 items-center justify-center rounded-full bg-black/45 text-white"
          aria-label={copy.previewPrevious}
          title={copy.previewPrevious}
          onClick={previous}
        >
          ›
        </button>
        <button
          type="button"
          className="absolute left-1 top-1/2 z-20 flex min-h-9 min-w-9 -translate-y-1/2 items-center justify-center rounded-full bg-black/45 text-white"
          aria-label={copy.previewNext}
          title={copy.previewNext}
          onClick={next}
        >
          ‹
        </button>
      </div>
    </aside>
  );
}
