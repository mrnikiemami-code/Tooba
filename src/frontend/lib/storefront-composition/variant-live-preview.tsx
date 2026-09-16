"use client";

/**
 * Variant Picker / Review live preview — real production Section components
 * via shared-composition-renderer + PreviewFakeData (LOCK-SF-336…341).
 */

import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type MouseEvent as ReactMouseEvent,
  type ReactNode,
} from "react";
import type { LandingRenderContext, StorefrontLandingSection } from "../../app/storefront/storefront-landing-api.ts";
import type { CompositionSectionInstance } from "./types.ts";
import { getVariant } from "./registry.ts";
import { renderSharedLandingSection } from "./shared-composition-renderer.tsx";
import { resolveVariantPreviewCardinality } from "./preview-fill-policy.ts";
import { canonicalizeVariantKey } from "./resolve-variant.ts";

const EMPTY_CONTEXT: LandingRenderContext = {
  products: [],
  categories: [],
  brands: [],
  articles: [],
  reviews: [],
  menus: {},
};

function buildPreviewSection(variantKey: string, sectionTypeKey: string): {
  section: StorefrontLandingSection;
  composition: CompositionSectionInstance;
  config: Record<string, unknown>;
} {
  const key = canonicalizeVariantKey(variantKey);
  const { previewTargetItems } = resolveVariantPreviewCardinality(key);
  const pageSectionId = `variant-picker-preview-${key.replace(/\./g, "-")}`;
  const config: Record<string, unknown> = {
    variantKey: key,
    title: "پیش‌نمایش",
    take: Math.max(previewTargetItems, 1),
    enabled: true,
    heightPreset: "Medium",
  };

  if (key.startsWith("banner.")) {
    config.items = Array.from({ length: previewTargetItems }, (_, index) => ({
      imageUrl: "",
      href: "/offers",
      title: `بنر ${(index + 1).toLocaleString("fa-IR")}`,
    }));
  }

  const section: StorefrontLandingSection = {
    pageSectionId,
    sectionType: sectionTypeKey,
    sortOrder: 0,
    config: JSON.stringify(config),
    items: [],
  };

  const composition: CompositionSectionInstance = {
    id: pageSectionId,
    sectionTypeKey,
    variantKey: key,
    enabled: true,
    displayOrder: 0,
    settings: config,
    surfaceRole: "section",
  };

  return { section, composition, config };
}

function suppressBusinessNavigation(event: ReactMouseEvent) {
  const target = event.target as HTMLElement | null;
  if (!target) return;
  const interactive = target.closest(
    "a[href], button[data-add-to-cart], [data-testid*=add-to-cart], [data-action=add-to-cart]",
  );
  if (!interactive) return;
  // Allow tab/carousel controls (role=tab, swiper buttons) — only block navigation / ATC.
  if (interactive.getAttribute("role") === "tab") return;
  if (interactive.classList.contains("swiper-button-prev") || interactive.classList.contains("swiper-button-next")) {
    return;
  }
  if (interactive.classList.contains("swiper-pagination-bullet")) return;
  event.preventDefault();
  event.stopPropagation();
}

type Props = {
  variantKey: string;
  /** When false, render a lightweight placeholder until scrolled into view. */
  eager?: boolean;
  /** Taller frame for Review step. */
  size?: "card" | "review";
  testId?: string;
  className?: string;
};

/**
 * Constrained preview frame — no CSS transform zoom.
 * Mounts production renderer with Store PreviewFake fill; suppresses business mutations.
 */
export function VariantLivePreview({
  variantKey,
  eager = false,
  size = "card",
  testId = "variant-live-preview",
  className = "",
}: Props) {
  const key = canonicalizeVariantKey(variantKey);
  const variant = getVariant(key);
  const rootRef = useRef<HTMLDivElement | null>(null);
  const [visible, setVisible] = useState(eager);

  useEffect(() => {
    if (eager || visible) return;
    const node = rootRef.current;
    if (!node || typeof IntersectionObserver === "undefined") {
      setVisible(true);
      return;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          setVisible(true);
          observer.disconnect();
        }
      },
      { rootMargin: "120px 0px", threshold: 0.01 },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, [eager, visible]);

  const frame = useMemo(() => {
    if (!variant) return null;
    return buildPreviewSection(key, variant.sectionTypeKey);
  }, [key, variant]);

  const rendered: ReactNode = useMemo(() => {
    if (!visible || !frame || !variant) return null;
    return renderSharedLandingSection({
      section: frame.section,
      composition: frame.composition,
      config: frame.config,
      context: EMPTY_CONTEXT,
      preview: true,
      previewSource: "store",
      previewLocale: "fa",
    });
  }, [visible, frame, variant]);

  const onCaptureClick = useCallback((event: ReactMouseEvent) => {
    suppressBusinessNavigation(event);
  }, []);

  const heightClass = size === "review" ? "max-h-[320px] min-h-[180px]" : "max-h-[160px] min-h-[112px]";

  return (
    <div
      ref={rootRef}
      className={`relative overflow-hidden rounded-xl border border-slate-200 bg-white ${className}`}
      data-testid={testId}
      data-variant-live-preview={key}
      data-preview-fingerprint={key}
      data-live-preview="1"
      data-preview-source="PreviewFake"
      dir="rtl"
      onClickCapture={onCaptureClick}
    >
      <div
        className={`pointer-events-auto overflow-x-auto overflow-y-hidden ${heightClass}`}
        data-preview-frame="constrained"
        data-preview-interaction="safe"
      >
        {visible ? (
          <div className="origin-top w-full min-w-[280px] [&_section]:!py-3 [&_section]:!px-2" data-preview-content="production">
            {rendered ?? (
              <p className="p-3 text-center text-xs text-muted" data-preview-empty="1">
                پیش‌نمایش در دسترس نیست
              </p>
            )}
          </div>
        ) : (
          <div
            className="flex h-28 items-center justify-center bg-slate-50 text-xs text-muted"
            data-preview-lazy-placeholder="1"
            aria-hidden
          >
            در حال آماده‌سازی پیش‌نمایش…
          </div>
        )}
      </div>
      <span
        className="pointer-events-none absolute bottom-1 left-1 rounded bg-amber-50/95 px-1.5 py-0.5 text-[10px] font-bold text-amber-800 ring-1 ring-amber-200"
        data-preview-fake-badge="1"
      >
        محتوای نمونه
      </span>
    </div>
  );
}
