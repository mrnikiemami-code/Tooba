"use client";

import { useCallback, useEffect, useRef, useState, type FormEvent, type MouseEvent, type TouchEvent } from "react";
import {
  Bookmark,
  Camera,
  ChevronLeft,
  ChevronRight,
  Clock,
  Flag,
  Heart,
  MessageCircle,
  MoreHorizontal,
  Send,
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

type StoryComment = {
  id: number;
  user: string;
  avatar: string;
  text: string;
  time: string;
};

function progressStep(item: PublicStoryItem | undefined, isVideo: boolean): number {
  const durationMs = item?.durationMs;
  if (durationMs != null && durationMs > 0) {
    return 5000 / durationMs;
  }
  return isVideo ? 0.3 : 1;
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
): { type: string; target: string } | null {
  if (item?.ctaTarget && item.ctaType && item.ctaType !== "none") {
    return { type: item.ctaType, target: item.ctaTarget };
  }
  if (story?.ctaTarget && story.ctaType && story.ctaType !== "none") {
    return { type: story.ctaType, target: story.ctaTarget };
  }
  return null;
}

/**
 * مودال استوری Shopeiva — کروم بصری/تعاملی دقیق با مدل آیتم‌های زنده.
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
  const [isLiked, setIsLiked] = useState(false);
  const [isSaved, setIsSaved] = useState(false);
  const [isMuted, setIsMuted] = useState(true);
  const [progress, setProgress] = useState(0);
  const [commentText, setCommentText] = useState("");
  const [showComments, setShowComments] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  const [showActions, setShowActions] = useState(false);
  const [likesCount, setLikesCount] = useState(0);
  const [comments, setComments] = useState<StoryComment[]>([]);
  const [mediaError, setMediaError] = useState(false);

  const videoRef = useRef<HTMLVideoElement | null>(null);
  const progressTimer = useRef<ReturnType<typeof setInterval> | null>(null);
  const startX = useRef(0);
  const isDragging = useRef(false);

  const currentStory = stories[currentStoryIndex];
  const items = currentStory?.items?.length ? currentStory.items : [];
  const currentItem = items[currentItemIndex];
  const totalStories = stories.length;
  const isVideo = (currentItem?.mediaType ?? (currentStory?.isVideo ? "video" : "image")) === "video";
  const mediaUrl = currentItem?.mediaUrl ?? currentStory?.coverMediaUrl ?? "";
  const avatarUrl = currentStory?.coverMediaUrl ?? items[0]?.mediaUrl ?? "/images/stories/1.jpg";

  const goToNextItem = useCallback(() => {
    setShowComments(false);
    setIsPaused(false);
    setProgress(0);
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
    setShowComments(false);
    setIsPaused(false);
    setProgress(0);
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
    setIsLiked(false);
    setIsSaved(false);
    setLikesCount(0);
    setComments([]);
    setProgress(0);
    setShowComments(false);
    setIsPaused(false);
    setMediaError(false);
    if (videoRef.current) {
      videoRef.current.currentTime = 0;
      videoRef.current.muted = isMuted;
      void videoRef.current.play();
    }
    // Intentionally omit isMuted — mute toggle must not reset progress.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentStoryIndex, currentItemIndex, currentStory]);

  useEffect(() => {
    if (!isOpen || !currentStory || items.length === 0) return;
    if (isPaused || showComments) {
      if (progressTimer.current) clearInterval(progressTimer.current);
      return;
    }

    const step = progressStep(currentItem, isVideo);
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
  }, [isOpen, isPaused, showComments, isVideo, currentStoryIndex, currentItemIndex, currentItem, currentStory, items.length, goToNextItem]);

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

  const toggleLike = () => {
    setIsLiked((prev) => !prev);
    setLikesCount((prev) => (isLiked ? Math.max(0, prev - 1) : prev + 1));
  };

  const toggleSave = () => setIsSaved((prev) => !prev);

  const toggleMute = () => {
    setIsMuted((prev) => !prev);
    if (videoRef.current) videoRef.current.muted = !isMuted;
  };

  const handleCommentSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!commentText.trim()) return;
    setComments((prev) => [
      ...prev,
      {
        id: prev.length + 1,
        user: "شما",
        avatar: "/images/stories/1.jpg",
        text: commentText,
        time: "اکنون",
      },
    ]);
    setCommentText("");
  };

  const handleShare = async () => {
    if (navigator.share) {
      try {
        await navigator.share({
          title: "استوری توبا",
          text: "این استوری رو ببین!",
          url: window.location.href,
        });
      } catch (error) {
        if (error instanceof Error && error.name !== "AbortError") console.error("Share failed:", error);
      }
    } else {
      try {
        await navigator.clipboard.writeText(window.location.href);
        alert("لینک کپی شد!");
      } catch {
        alert(`لینک: ${window.location.href}`);
      }
    }
  };

  const handleCta = (e: MouseEvent) => {
    e.stopPropagation();
    const cta = currentCta(currentItem, currentStory);
    if (!cta) return;
    const target = cta.target.trim();
    if (!target || /^javascript:/i.test(target) || /^data:/i.test(target) || /^vbscript:/i.test(target)) {
      return;
    }
    if (cta.type === "external" || isExternalHttp(target)) {
      if (!isExternalHttp(target)) return;
      window.location.href = target;
      return;
    }
    const path = target.startsWith("/") ? target : `/${target}`;
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
      data-testid="story-modal"
      className="fixed inset-0 z-[200] bg-black flex items-center justify-center p-2 sm:p-4"
      onClick={() => setShowActions(false)}
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
            <div key={item.storyItemId} className="flex-1 h-1 bg-white/20 rounded-full overflow-hidden">
              <div
                className="h-full bg-white transition-all duration-100 rounded-full"
                style={{
                  width: index === currentItemIndex ? `${progress}%` : index < currentItemIndex ? "100%" : "0%",
                }}
              />
            </div>
          ))}
        </div>

        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onClose();
          }}
          className="absolute top-3 right-3 z-30 w-10 h-10 bg-black/50 backdrop-blur-sm rounded-full flex items-center justify-center text-white hover:bg-black/70 transition-all border border-white/10"
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
              loop
              playsInline
              autoPlay
              onError={() => setMediaError(true)}
              onLoadedData={() => setMediaError(false)}
            />
          ) : (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={mediaUrl}
              alt="Story"
              className="w-full h-full object-contain"
              onError={() => setMediaError(true)}
              onLoad={() => setMediaError(false)}
            />
          )}

          {mediaError ? (
            <div className="w-full h-full flex items-center justify-center text-white/30 absolute inset-0">
              <div className="text-center">
                <Camera className="w-16 h-16 mx-auto mb-4 opacity-50" />
                <p className="text-sm">محتوایی برای نمایش وجود ندارد</p>
                <p className="text-xs text-white/20 mt-2">مسیر: {mediaUrl}</p>
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
          >
            <ChevronLeft className="w-5 h-5" />
          </button>

          <div className="absolute top-12 left-4 right-4 z-20 flex items-center gap-3">
            <div className="w-10 h-10 rounded-full overflow-hidden border-2 border-[#E53935]">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={avatarUrl} alt={currentStory.title} width={40} height={40} className="object-cover w-full h-full" loading="lazy" />
            </div>
            <div className="flex-1">
              <div className="flex items-center gap-2">
                <span className="text-white font-bold text-sm">{currentStory.title}</span>
                <span className="text-white/50 text-xs">@story</span>
              </div>
              <div className="flex items-center gap-2 text-white/40 text-xs">
                <Clock className="w-3 h-3" />
                <span>اکنون</span>
              </div>
            </div>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setShowActions((prev) => !prev);
              }}
              className="text-white/60 hover:text-white transition-colors p-2"
            >
              <MoreHorizontal className="w-5 h-5" />
            </button>
          </div>

          {showActions ? (
            <div
              className="absolute top-20 left-4 right-4 z-30 bg-black/80 backdrop-blur-md rounded-xl p-3 border border-white/10"
              onClick={(e) => e.stopPropagation()}
            >
              <button type="button" className="flex items-center gap-3 w-full p-2 text-white hover:bg-white/10 rounded-lg transition-colors">
                <Flag className="w-4 h-4 text-[#E53935]" />
                <span className="text-sm">گزارش مشکل</span>
              </button>
            </div>
          ) : null}

          {cta ? (
            <div className="absolute bottom-20 left-4 right-4 z-20 flex justify-center">
              <button
                type="button"
                onClick={handleCta}
                className="px-6 py-2.5 rounded-full bg-[#E53935] text-white text-sm font-bold shadow-lg hover:bg-[#c62828] transition-colors"
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
                  toggleLike();
                }}
                className="p-2 rounded-full bg-black/30 backdrop-blur-sm hover:bg-black/50 transition-all border border-white/10 group relative"
              >
                <Heart
                  className={`w-6 h-6 transition-all ${isLiked ? "text-[#E53935] fill-[#E53935] scale-110" : "text-white group-hover:text-[#E53935]"}`}
                />
                {likesCount > 0 ? (
                  <span className="absolute -top-1 -right-1 text-[10px] text-white bg-[#E53935] px-1.5 py-0.5 rounded-full">
                    {toPersianDigits(likesCount)}
                  </span>
                ) : null}
              </button>

              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setShowComments((prev) => !prev);
                }}
                className="p-2 rounded-full bg-black/30 backdrop-blur-sm hover:bg-black/50 transition-all border border-white/10 relative"
              >
                <MessageCircle className="w-6 h-6 text-white" />
                {comments.length > 0 ? (
                  <span className="absolute -top-1 -right-1 text-[10px] text-white bg-[#E53935] px-1.5 py-0.5 rounded-full">
                    {toPersianDigits(comments.length)}
                  </span>
                ) : null}
              </button>

              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  void handleShare();
                }}
                className="p-2 rounded-full bg-black/30 backdrop-blur-sm hover:bg-black/50 transition-all border border-white/10"
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
                  className="p-2 rounded-full bg-black/30 backdrop-blur-sm hover:bg-black/50 transition-all border border-white/10"
                >
                  {isMuted ? <VolumeX className="w-6 h-6 text-white" /> : <Volume2 className="w-6 h-6 text-white" />}
                </button>
              ) : null}
            </div>

            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  toggleSave();
                }}
                className="p-2 rounded-full bg-black/30 backdrop-blur-sm hover:bg-black/50 transition-all border border-white/10"
              >
                <Bookmark className={`w-6 h-6 transition-all ${isSaved ? "text-[#E53935] fill-[#E53935]" : "text-white"}`} />
              </button>
            </div>
          </div>

          {showComments ? (
            <div
              className="absolute bottom-16 left-0 right-0 z-20 bg-black/80 backdrop-blur-md rounded-t-2xl max-h-[250px] overflow-hidden"
              onClick={(e) => e.stopPropagation()}
            >
              <div className="p-3 max-h-[190px] overflow-y-auto scrollbar-thin scrollbar-thumb-gray-600 scrollbar-track-transparent">
                {comments.length === 0 ? (
                  <p className="text-white/50 text-center text-sm py-4">هنوز کامنتی ثبت نشده</p>
                ) : (
                  comments.map((comment) => (
                    <div key={comment.id} className="flex items-start gap-2 mb-3">
                      <div className="w-8 h-8 rounded-full overflow-hidden shrink-0">
                        {/* eslint-disable-next-line @next/next/no-img-element */}
                        <img src={comment.avatar} alt={comment.user} width={32} height={32} className="object-cover w-full h-full" loading="lazy" />
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <span className="text-white text-xs font-bold">{comment.user}</span>
                          <span className="text-white/30 text-[10px]">{comment.time}</span>
                        </div>
                        <p className="text-white/80 text-sm">{comment.text}</p>
                      </div>
                    </div>
                  ))
                )}
              </div>

              <form onSubmit={handleCommentSubmit} className="p-2 border-t border-white/10 flex items-center gap-2 bg-black/40">
                <input
                  type="text"
                  value={commentText}
                  onChange={(e) => setCommentText(e.target.value)}
                  placeholder="نظرت چیه؟..."
                  className="flex-1 bg-white/10 text-white placeholder-white/40 text-sm rounded-full px-4 py-2 outline-none focus:ring-1 focus:ring-[#E53935]"
                />
                <button
                  type="submit"
                  disabled={!commentText.trim()}
                  className="p-2 rounded-full bg-[#E53935] text-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-[#c62828] transition-colors"
                >
                  <Send className="w-4 h-4" />
                </button>
              </form>
            </div>
          ) : null}

          <div className="absolute top-16 right-1/2 translate-x-1/2 z-20 bg-black/50 backdrop-blur-sm px-3 py-1 rounded-full text-white/60 text-xs">
            {toPersianDigits(currentStoryIndex + 1)} / {toPersianDigits(totalStories)}
          </div>
        </div>
      </div>
    </div>
  );
}
