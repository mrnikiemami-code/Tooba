"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AlertTriangle, Check, Eye, ImagePlus, Layers, Plus, Send, Store, Trash2, XCircle } from "lucide-react";
import { storyToast } from "./story-toast.ts";
import {
  StoryActionBar,
  StoryEmptyState,
  StoryPageHeader,
  StorySectionCard,
  StoryStatusPill,
  storyButtonClass,
  type StoryTone,
} from "./story-management-ui";
import { StoryPublicationWindowFields } from "./story-publication-window-fields";
import { StoryPreviewPlayer } from "./story-preview-player";
import {
  AppDataGrid,
  Dialog,
  ErrorState,
  faWorkspaceMessages,
  adminGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../../design-system";
import type { GridBulkAction, GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import { ADMIN_STORY_GRID_VIEW_KEY } from "../../admin/saved-view-store";
import { formatAdminDate, type AdminLoadState } from "../../admin/admin-api";
import { ADMIN_ACTOR_STORAGE_KEY, DEFAULT_ADMIN_ACTOR_ID } from "../../../lib/admin/admin-result";
import { queryAdminSellersGrid, type AdminSellerRow } from "../../../features/admin-sellers";
import { loadSellerDevContexts, readActorUserId, readSellerPartyId } from "../../vendor-panel/seller-api";
import { JalaliDatePicker } from "../../../design-system/app-data-grid/jalali-date-picker";
import {
  getAdminStory,
  getSellerStory,
  queryAdminStoriesGrid,
  querySellerStories,
  type AdminStorySnapshot,
} from "../story-api";
import { type StoryCapabilities } from "./story-capabilities";
import {
  resolveStoryUiLocale,
  storyCopy,
  STORY_ORIGIN_LABELS_BY_LOCALE,
  type StoryCopy,
} from "./story-management-copy";
import { runDurableStoryMutation, runDurableUploadAdd, replayPendingStoryIntent } from "./story-executor";
import { loadStoryLocaleOptions, loadStoryProductOptions, type StoryProductOptionRow } from "./story-lookups";
import {
  formatStoryScheduleClock,
  isoToScheduleDraft,
  parseOptionalPublicationWindow,
  parseStoryScheduleDraft,
  type StoryScheduleDraft,
} from "./story-schedule-time";
import {
  digestFile,
  listPendingStoryIntents,
  newStoryOccurrenceKey as mintOccurrence,
  pendingStoryLabel,
  type StoryIntentEnvelope,
} from "./story-intent";
import { readStoryScope, subscribeStoryScope } from "./story-scope";
import { createScopedStorySavedViewStore } from "./story-saved-views";
import {
  classifyAdminOp,
  executeAdminOpBatch,
  explainZeroEligibleToast,
  formatAdminBatchToast,
  groupSkippedReasons,
  storyRefusalReasonFa,
  type StoryAdminOpKind,
  type StoryAdminOpPlan,
} from "./story-admin-ops";

const SELLER_STORY_GRID_VIEW_KEY = "seller.stories.grid.v1";
const SHOP_PAGE_SIZE = 50;
const SHOP_SEARCH_DEBOUNCE_MS = 300;
const PRODUCT_SEARCH_DEBOUNCE_MS = 300;

/** Preserve the active locale prefix (`/fa`, `/en`) on every Story route transition. */
function storyLocalePrefix(): string {
  if (typeof window === "undefined") return "";
  const segment = window.location.pathname.split("/").filter(Boolean)[0]?.toLowerCase();
  return segment === "fa" || segment === "en" ? `/${segment}` : "";
}

function hasAction(row: AdminStorySnapshot, code: string): boolean {
  return (row.actionCodes ?? []).includes(code);
}

function resolveActorId(mode: "admin" | "seller"): string {
  if (mode === "seller") return readActorUserId() ?? "";
  if (typeof window === "undefined") return DEFAULT_ADMIN_ACTOR_ID;
  return window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? DEFAULT_ADMIN_ACTOR_ID;
}

function Denied({ retry, copy }: { retry: () => void; copy: StoryCopy }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title={copy.deniedTitle}
        detail={copy.deniedDetail}
        onRetry={retry}
        retryLabel={faWorkspaceMessages.retry}
      />
    </div>
  );
}

function shopLabel(row: AdminStorySnapshot, copy: StoryCopy): string {
  if (row.shopDisplayName?.trim()) return row.shopDisplayName.trim();
  if (!row.sellerPartyId) return copy.platformStorefront;
  return copy.shopNameUnavailable;
}

function looksLikeGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.trim());
}

function slideDraftSignature(input: {
  editingItemId: string | null;
  caption: string;
  durationSec: string;
  ctaType: string;
  ctaTarget: string;
  productLabel: string;
  file: File | null;
}): string {
  return JSON.stringify({
    editingItemId: input.editingItemId,
    caption: input.caption,
    durationSec: input.durationSec,
    ctaType: input.ctaType,
    ctaTarget: input.ctaTarget,
    productLabel: input.productLabel,
    file: input.file
      ? { name: input.file.name, size: input.file.size, lastModified: input.file.lastModified }
      : null,
  });
}
const OPEN_SLIDE_AFTER_LOAD_KEY = "tooba.story.openSlideAfterLoad";

function destinationLabelForType(ctaType: string | null | undefined, copy: StoryCopy): string {
  const type = (ctaType ?? "").trim().toLowerCase();
  if (!type || type === "none") return copy.destinationNone;
  if (type === "product") return copy.destinationProduct;
  if (type === "external") return copy.destinationExternal;
  if (type === "internal" || type === "category" || type === "article") return copy.destinationInternal;
  if (type === "shop" || type === "seller") return copy.destinationShop;
  return copy.destinationUnavailable;
}

function displayState(row: AdminStorySnapshot, copy: StoryCopy, nowMs: number = Date.now()): string {
  // «در ویترین» فقط وقتی backend effectivelyPublic=true — نه صرفاً Status=Active.
  if (row.effectivelyPublic) return copy.displayOnStorefront;
  if (row.reviewStatus === "Submitted") return copy.displayAwaiting;
  if (row.reviewStatus === "Rejected") return copy.displayRejected;
  if (row.status === "Expired") return copy.displayExpired;
  if (row.status === "Disabled") return copy.displayStopped;
  // پنجرهٔ انتشار گذشته روی Active/Scheduled ذخیره‌شده بدون بازنویسی Status → منقضی.
  if (
    (row.status === "Active" || row.status === "Scheduled") &&
    row.endAt
  ) {
    const end = Date.parse(row.endAt);
    if (!Number.isNaN(end) && end <= nowMs) return copy.displayExpired;
  }
  if (row.status === "Scheduled") return copy.displayScheduled;
  if (row.status === "Active" && row.startAt) {
    const start = Date.parse(row.startAt);
    if (!Number.isNaN(start) && start > nowMs) return copy.displayScheduled;
  }
  // Approval is not publication: an approved story still in Draft (or Active without an
  // effectively-public window) must not read as «پیش‌نویس» nor as «در ویترین».
  if (row.reviewStatus === "Approved" && (row.status === "Draft" || row.status === "Active")) {
    return copy.displayApprovedUnpublished;
  }
  if (row.status === "Draft" || row.reviewStatus === "None" || row.status === "Active") {
    return copy.displayDraft;
  }
  return copy.displayDraft;
}

function displayStateTone(row: AdminStorySnapshot): StoryTone {
  if (row.effectivelyPublic) return "success";
  if (row.reviewStatus === "Rejected" || row.status === "Expired" || row.status === "Disabled") return "danger";
  if (row.reviewStatus === "Submitted") return "warning";
  if (row.status === "Scheduled") return "info";
  return "neutral";
}

function StoryPreviewPanel({
  items,
  copy,
  numberLocale,
  testId,
}: {
  items: AdminStorySnapshot["items"];
  copy: StoryCopy;
  numberLocale: string;
  testId: string;
}) {
  return <StoryPreviewPlayer items={items} copy={copy} numberLocale={numberLocale} testId={testId} />;
}

function validateCtaInput(ctaType: string, ctaTarget: string, copy: StoryCopy): string | null {
  const type = (ctaType ?? "").trim().toLowerCase();
  if (!type || type === "none") return null;
  const value = (ctaTarget ?? "").trim();
  if (!value) return copy.ctaRequired;
  if (type === "external") {
    try {
      const url = new URL(value);
      if (url.protocol !== "http:" && url.protocol !== "https:") return copy.ctaInvalidUrl;
    } catch {
      return copy.ctaInvalidUrl;
    }
    return null;
  }
  if (type === "internal" || type === "category" || type === "article") {
    if (!value.startsWith("/") || value.startsWith("//") || value.includes("\\")) {
      return copy.ctaInvalidPath;
    }
  }
  return null;
}
function isVideoFile(file: File | null): boolean {
  if (!file) return false;
  return file.type.startsWith("video/") || /\.(mp4|webm|mov)$/i.test(file.name);
}

export function StoryManagementScreen({
  capabilities,
  initialStoryId = null,
  createMode = false,
  navigate,
}: {
  capabilities: StoryCapabilities;
  initialStoryId?: string | null;
  createMode?: boolean;
  /** Optional navigation (pages wire Next router; tests inject a stub). */
  navigate?: (href: string) => void;
}) {
  const router = {
    push: (href: string) => {
      if (navigate) navigate(href);
      else if (typeof window !== "undefined") window.history.pushState({}, "", href);
    },
  };
  const basePath = `${storyLocalePrefix()}${
    capabilities.mode === "seller" ? "/vendor-panel/stories" : "/admin/stories"
  }`;
  // continue as single body — keep previous Body signature by aliasing
  return (
    <StoryManagementScreenBody
      capabilities={capabilities}
      initialStoryId={initialStoryId}
      createMode={createMode}
      navigate={navigate}
      router={router}
      basePath={basePath}
    />
  );
}

function StoryManagementScreenBody({
  capabilities,
  initialStoryId = null,
  createMode = false,
  navigate,
  router,
  basePath,
}: {
  capabilities: StoryCapabilities;
  initialStoryId?: string | null;
  createMode?: boolean;
  navigate?: (href: string) => void;
  router: { push: (href: string) => void };
  basePath: string;
}) {
  void navigate;
  const uiLocale = resolveStoryUiLocale();
  const copy = storyCopy(uiLocale);
  const originLabels = STORY_ORIGIN_LABELS_BY_LOCALE[uiLocale];
  const numberLocale = uiLocale === "en" ? "en-US" : "fa-IR";
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [message, setMessage] = useState<string>();
  const [showCreate, setShowCreate] = useState(createMode);
  const [selectedId, setSelectedId] = useState<string | null>(initialStoryId);
  const [detail, setDetail] = useState<AdminStorySnapshot | null>(null);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [reviewFilter, setReviewFilter] = useState<string>("");
  const [adminOpPlan, setAdminOpPlan] = useState<StoryAdminOpPlan | null>(null);
  const [adminOpReason, setAdminOpReason] = useState("");
  const [adminOpBusy, setAdminOpBusy] = useState(false);
  const adminOpCancelledRef = useRef(false);
  const productEpochRef = useRef(0);
  const runSubmitRef = useRef<(row: AdminStorySnapshot) => void>(() => undefined);
  const [scheduleOpen, setScheduleOpen] = useState(false);
  const [scheduleMode, setScheduleMode] = useState<"schedule" | "approve_schedule">("schedule");
  const [scheduleDraft, setScheduleDraft] = useState<StoryScheduleDraft>({
    startDayIso: "",
    startTime: "10:00",
    endDayIso: "",
    endTime: "",
  });
  const [scheduleFieldError, setScheduleFieldError] = useState<string | null>(null);
  const [scopeTick, setScopeTick] = useState(0);
  const [recoveryFile, setRecoveryFile] = useState<File | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const [draftTitle, setDraftTitle] = useState("");
  const [slideFile, setSlideFile] = useState<File | null>(null);
  const [adminShopMode, setAdminShopMode] = useState<"platform" | "shop">("platform");
  const [adminShopId, setAdminShopId] = useState("");
  const [adminShopName, setAdminShopName] = useState("");
  const [shopOptions, setShopOptions] = useState<AdminSellerRow[]>([]);
  const [shopSearch, setShopSearch] = useState("");
  const [shopLoading, setShopLoading] = useState(false);
  const [shopLoadingMore, setShopLoadingMore] = useState(false);
  const [shopHasMore, setShopHasMore] = useState(false);
  const [shopError, setShopError] = useState<string | null>(null);
  const shopPageRef = useRef(0);
  const shopHasMoreRef = useRef(false);
  const shopLoadingRef = useRef(false);
  const shopRequestRef = useRef(0);
  const shopSearchRef = useRef("");
  const [pendingActs, setPendingActs] = useState<StoryIntentEnvelope[]>([]);
  const [pendingRevision, setPendingRevision] = useState(0);
  const [pendingRecoveryOpen, setPendingRecoveryOpen] = useState(false);
  const [previewConfirm, setPreviewConfirm] = useState<"publish" | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [editLocale, setEditLocale] = useState("");
  const [localeOptions, setLocaleOptions] = useState<Array<{ code: string; name: string; isDefault: boolean }>>([]);
  const [productOptions, setProductOptions] = useState<StoryProductOptionRow[]>([]);
  const [productSearch, setProductSearch] = useState("");
  const [productTotalCount, setProductTotalCount] = useState(0);
  const [productPage, setProductPage] = useState(1);
  const [productLoading, setProductLoading] = useState(false);
  const [productError, setProductError] = useState<string | null>(null);
  const [productReloadToken, setProductReloadToken] = useState(0);
  const [selectedProductLabel, setSelectedProductLabel] = useState("");
  const [slideCaption, setSlideCaption] = useState("");
  const [slideDurationSec, setSlideDurationSec] = useState("5");
  const [slideCtaType, setSlideCtaType] = useState("none");
  const [slideCtaTarget, setSlideCtaTarget] = useState("");
  const [slideCtaError, setSlideCtaError] = useState<string | null>(null);
  const [slideDialogOpen, setSlideDialogOpen] = useState(false);
  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [productLabelById, setProductLabelById] = useState<Record<string, string>>({});
  const [sellerShopLabel, setSellerShopLabel] = useState("");
  const [dirty, setDirty] = useState(false);
  const [slideBaseline, setSlideBaseline] = useState("");
  const [slideDiscardOpen, setSlideDiscardOpen] = useState(false);
  const [leaveOpen, setLeaveOpen] = useState(false);
  const [pendingLeaveHref, setPendingLeaveHref] = useState<string | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<AdminStorySnapshot | null>(null);
  const [draftWindow, setDraftWindow] = useState<StoryScheduleDraft>({
    startDayIso: "",
    startTime: "10:00",
    endDayIso: "",
    endTime: "",
  });
  const [draftWindowError, setDraftWindowError] = useState<string | null>(null);
  const [noPublicationEnd, setNoPublicationEnd] = useState(true);
  const [slidePreviewUrl, setSlidePreviewUrl] = useState<string | null>(null);
  const slideFileInputRef = useRef<HTMLInputElement | null>(null);
  const ownedDraftIdRef = useRef<string | null>(initialStoryId);
  const guardUrlRef = useRef<string | null>(null);
  const slideDirty =
    slideDialogOpen &&
    slideDraftSignature({
      editingItemId,
      caption: slideCaption,
      durationSec: slideDurationSec,
      ctaType: slideCtaType,
      ctaTarget: slideCtaTarget,
      productLabel: selectedProductLabel,
      file: slideFile,
    }) !== slideBaseline;
  const hasUnsavedChanges = dirty || slideDirty;

  function resolveShopDestinationName(target: string | null | undefined): string | null {
    const raw = target?.trim() ?? "";
    if (!raw || !looksLikeGuid(raw)) return null;
    if (detail?.sellerPartyId === raw && detail.shopDisplayName?.trim()) {
      return detail.shopDisplayName.trim();
    }
    const named = shopOptions.find((row) => row.sellerPartyId === raw)?.displayName?.trim();
    return named || null;
  }

  function destinationLabelForItem(item: AdminStorySnapshot["items"][number]): string {
    const ctaType = item.ctaType;
    const ctaTarget = item.ctaTarget;
    const type = (ctaType ?? "").trim().toLowerCase();
    if (!type || type === "none") return copy.destinationNone;
    if (type === "product") {
      const id = (ctaTarget ?? "").trim();
      if (!id) return copy.productUnknown;
      const owned = item.ctaProductTitle?.trim() || productLabelById[id];
      if (owned) return owned;
      return copy.productUnknown;
    }
    if (type === "external") {
      return ctaTarget?.trim() ? `${copy.destinationExternal}: ${ctaTarget}` : copy.destinationExternal;
    }
    if (type === "internal" || type === "category" || type === "article") {
      return ctaTarget?.trim() ? `${copy.destinationInternal}: ${ctaTarget}` : copy.destinationInternal;
    }
    if (type === "shop" || type === "seller") {
      const shopName = resolveShopDestinationName(ctaTarget);
      return shopName ? `${copy.destinationShop}: ${shopName}` : copy.destinationUnavailable;
    }
    return destinationLabelForType(ctaType, copy);
  }

  function finishLeave(href: string) {
    setSlideDialogOpen(false);
    setSlideDiscardOpen(false);
    setSlideBaseline("");
    if (href === basePath || href.endsWith(basePath)) {
      setSelectedId(null);
      setDetail(null);
      ownedDraftIdRef.current = null;
    }
    setDirty(false);
    router.push(href);
  }

  function requestLeave(href: string) {
    if (!dirty && !slideDirty) {
      finishLeave(href);
      return;
    }
    setPendingLeaveHref(href);
    setLeaveOpen(true);
  }

  function openNewSlideDialog() {
    setEditingItemId(null);
    setSlideFile(null);
    setSlideCaption("");
    setSlideDurationSec("5");
    setSlideCtaType("none");
    setSlideCtaTarget("");
    setSelectedProductLabel("");
    setProductSearch("");
    setSlideCtaError(null);
    setSlideBaseline(slideDraftSignature({
      editingItemId: null,
      caption: "",
      durationSec: "5",
      ctaType: "none",
      ctaTarget: "",
      productLabel: "",
      file: null,
    }));
    setSlideDialogOpen(true);
  }

  function openEditSlideDialog(item: AdminStorySnapshot["items"][number]) {
    setEditingItemId(item.storyItemId);
    setSlideFile(null);
    setSlideCaption(item.caption ?? "");
    setSlideDurationSec(
      item.durationMs && item.durationMs > 0 ? String(Math.round(item.durationMs / 1000)) : "5",
    );
    setSlideCtaType(item.ctaType || "none");
    setSlideCtaTarget(item.ctaTarget ?? "");
    setSelectedProductLabel(
      item.ctaType === "product" && item.ctaTarget
        ? item.ctaProductTitle?.trim() || productLabelById[item.ctaTarget] || ""
        : "",
    );
    const openedProductLabel =
      item.ctaType === "product" && item.ctaTarget
        ? item.ctaProductTitle?.trim() || productLabelById[item.ctaTarget] || ""
        : "";
    setProductSearch("");
    setSlideCtaError(null);
    setSlideBaseline(slideDraftSignature({
      editingItemId: item.storyItemId,
      caption: item.caption ?? "",
      durationSec:
        item.durationMs && item.durationMs > 0 ? String(Math.round(item.durationMs / 1000)) : "5",
      ctaType: item.ctaType || "none",
      ctaTarget: item.ctaTarget ?? "",
      productLabel: openedProductLabel,
      file: null,
    }));
    setSlideDialogOpen(true);
  }

  function closeSlideEditor() {
    if (slideDirty) {
      setSlideDiscardOpen(true);
      return;
    }
    setSlideDialogOpen(false);
    setEditingItemId(null);
    setSlideBaseline("");
  }

  function discardSlideEditor() {
    setSlideDiscardOpen(false);
    setSlideDialogOpen(false);
    setEditingItemId(null);
    setSlideBaseline("");
  }
  const epochRef = useRef(0);
  const inFlightRef = useRef(false);
  const scope = readStoryScope(capabilities.mode);
  const sellerShopId = scope.shopId;
  const actorId = scope.actorUserId || resolveActorId(capabilities.mode);
  const gate = {
    inFlightRef,
    epochRef,
    onIntentChanged: () => setPendingRevision((value) => value + 1),
  };

  const bumpEpoch = useCallback(() => {
    epochRef.current += 1;
  }, []);

  const refreshGrid = useCallback(() => {
    setReloadToken((v) => v + 1);
    setState("ok");
  }, []);

  useEffect(() => {
    bumpEpoch();
    return () => bumpEpoch();
  }, [bumpEpoch, capabilities.mode, sellerShopId, actorId, selectedId, scopeTick]);

  useEffect(() => {
    return subscribeStoryScope(capabilities.mode, () => {
      bumpEpoch();
      setDetail(null);
      setSelectedId(null);
      setShowCreate(false);
      setPendingActs([]);
      setScopeTick((v) => v + 1);
      setReloadToken((v) => v + 1);
    });
  }, [capabilities.mode, bumpEpoch]);

  useEffect(() => {
    const pending = listPendingStoryIntents(capabilities.mode, sellerShopId, actorId)
      .filter((row) => row.state === "ok")
      .map((row) => row.envelope);
    setPendingActs(pending);
  }, [capabilities.mode, sellerShopId, actorId, reloadToken, scopeTick, pendingRevision]);

  const loadShopPage = useCallback(
    async (page: number, search: string, mode: "reset" | "append") => {
      if (mode === "append" && shopLoadingRef.current) return;

      const requestId = ++shopRequestRef.current;
      shopSearchRef.current = search;
      shopLoadingRef.current = true;
      if (mode === "reset") {
        setShopLoading(true);
        setShopLoadingMore(false);
        setShopOptions([]);
        setShopHasMore(false);
        shopHasMoreRef.current = false;
        shopPageRef.current = 0;
        setShopError(null);
      } else {
        setShopLoadingMore(true);
      }

      try {
        const result = await queryAdminSellersGrid({
          page,
          pageSize: SHOP_PAGE_SIZE,
          sorts: [{ columnId: "name", direction: "asc" }],
          filters: {},
          search: search.trim() || undefined,
        });
        if (requestId !== shopRequestRef.current) return;
        if (result.source === "error") {
          setShopError(result.message ?? copy.shopLoadFailed);
          return;
        }

        const rows = result.page.rows.filter((row) => row.status.toLowerCase() === "active");
        const hasMore = page * SHOP_PAGE_SIZE < result.page.total;
        shopPageRef.current = page;
        shopHasMoreRef.current = hasMore;
        setShopHasMore(hasMore);
        setShopOptions((previous) => {
          if (mode === "reset") return rows;
          const seen = new Set(previous.map((row) => row.sellerPartyId));
          return [...previous, ...rows.filter((row) => !seen.has(row.sellerPartyId))];
        });
      } catch {
        if (requestId === shopRequestRef.current) setShopError(copy.shopLoadFailed);
      } finally {
        if (requestId === shopRequestRef.current) {
          shopLoadingRef.current = false;
          setShopLoading(false);
          setShopLoadingMore(false);
        }
      }
    },
    [copy.shopLoadFailed],
  );

  useEffect(() => {
    if (capabilities.mode !== "admin" || adminShopMode !== "shop") return;
    const timer = window.setTimeout(() => {
      void loadShopPage(1, shopSearch, "reset");
    }, SHOP_SEARCH_DEBOUNCE_MS);
    return () => window.clearTimeout(timer);
  }, [capabilities.mode, adminShopMode, shopSearch, loadShopPage]);

  useEffect(() => {
    if (capabilities.mode !== "seller") {
      setSellerShopLabel("");
      return;
    }
    let cancelled = false;
    void loadSellerDevContexts()
      .then((rows) => {
        if (cancelled) return;
        const match = rows.find((row) => row.sellerPartyId === sellerShopId);
        setSellerShopLabel(match?.sellerLabel?.trim() ?? "");
      })
      .catch(() => {
        if (!cancelled) setSellerShopLabel("");
      });
    return () => {
      cancelled = true;
    };
  }, [capabilities.mode, sellerShopId, scopeTick]);

  useEffect(() => {
    void loadStoryLocaleOptions({
      mode: capabilities.mode,
      shopId: sellerShopId,
      actorUserId: actorId,
    }).then((rows) => {
      setLocaleOptions(rows);
      const def = rows.find((row) => row.isDefault) ?? rows[0];
      if (def) setEditLocale((prev) => prev || def.code);
    });
  }, [capabilities.mode, sellerShopId, actorId, scopeTick]);

  useEffect(() => {
    if (!slideDialogOpen || slideCtaType !== "product") {
      setProductLoading(false);
      return;
    }
    const shop =
      capabilities.mode === "seller"
        ? sellerShopId
        : detail?.sellerPartyId ?? (adminShopMode === "shop" ? adminShopId : null);
    const epoch = ++productEpochRef.current;
    setProductPage(1);
    setProductLoading(true);
    setProductError(null);
    // Debounce keystrokes so a fast typist issues one owning query, not one per character.
    const timer = setTimeout(() => {
      void loadStoryProductOptions({
        mode: capabilities.mode,
        shopId: shop,
        actorUserId: actorId,
        search: productSearch,
        page: 1,
        pageSize: 20,
      })
        .then((page) => {
          if (epoch !== productEpochRef.current) return;
          if (page.error) {
            setProductError(copy.productLoadFailed);
            setProductOptions([]);
            setProductTotalCount(0);
            return;
          }
          setProductOptions(page.items);
          setProductTotalCount(page.totalCount);
        })
        .catch(() => {
          if (epoch !== productEpochRef.current) return;
          setProductError(copy.productLoadFailed);
          setProductOptions([]);
          setProductTotalCount(0);
        })
        .finally(() => {
          if (epoch !== productEpochRef.current) return;
          setProductLoading(false);
        });
    }, PRODUCT_SEARCH_DEBOUNCE_MS);
    return () => {
      clearTimeout(timer);
      productEpochRef.current += 1;
    };
  }, [
    capabilities.mode,
    sellerShopId,
    detail?.sellerPartyId,
    adminShopId,
    adminShopMode,
    actorId,
    scopeTick,
    productSearch,
    slideDialogOpen,
    slideCtaType,
    productReloadToken,
    copy.productLoadFailed,
  ]);

  useEffect(() => {
    const onBeforeUnload = (event: BeforeUnloadEvent) => {
      if (!hasUnsavedChanges) return;
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", onBeforeUnload);
    return () => window.removeEventListener("beforeunload", onBeforeUnload);
  }, [hasUnsavedChanges]);

  // Unsaved edits also protect soft navigation: sidebar links and browser Back go through the
  // existing DS leave dialog instead of silently discarding the editor state.
  const requestLeaveRef = useRef(requestLeave);
  requestLeaveRef.current = requestLeave;
  useEffect(() => {
    if (!hasUnsavedChanges) {
      if (window.history.state?.storyDirtyGuard) {
        window.history.replaceState({}, "", window.location.href);
      }
      guardUrlRef.current = null;
      return;
    }
    // Keep the editor URL as the guard entry so Cancel restores it exactly.
    guardUrlRef.current = window.location.href;
    window.history.pushState({ storyDirtyGuard: true }, "", guardUrlRef.current);
    const onDocumentClick = (event: MouseEvent) => {
      if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
        return;
      }
      const anchor = (event.target as Element | null)?.closest?.("a[href]") as HTMLAnchorElement | null;
      if (!anchor) return;
      if (anchor.target && anchor.target !== "_self") return;
      if (anchor.hasAttribute("download")) return;
      const href = anchor.getAttribute("href");
      if (!href || href.startsWith("#") || href.startsWith("javascript:")) return;
      const target = new URL(href, window.location.href);
      if (target.origin !== window.location.origin) return;
      if (target.pathname === window.location.pathname && target.search === window.location.search) return;
      event.preventDefault();
      event.stopPropagation();
      requestLeaveRef.current(`${target.pathname}${target.search}`);
    };
    const onPopState = () => {
      const editorUrl = guardUrlRef.current;
      if (editorUrl) {
        window.history.pushState({ storyDirtyGuard: true }, "", editorUrl);
      }
      requestLeaveRef.current(basePath);
    };
    document.addEventListener("click", onDocumentClick, true);
    window.addEventListener("popstate", onPopState);
    return () => {
      document.removeEventListener("click", onDocumentClick, true);
      window.removeEventListener("popstate", onPopState);
    };
  }, [hasUnsavedChanges, basePath]);

  useEffect(() => {
    if (!slideFile) {
      setSlidePreviewUrl((prev) => {
        if (prev) URL.revokeObjectURL(prev);
        return null;
      });
      return;
    }
    const url = URL.createObjectURL(slideFile);
    setSlidePreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev);
      return url;
    });
    return () => {
      URL.revokeObjectURL(url);
    };
  }, [slideFile]);

  useEffect(() => {
    if (!slideDialogOpen) {
      setSlidePreviewUrl((prev) => {
        if (prev) URL.revokeObjectURL(prev);
        return null;
      });
      setSlideFile(null);
    }
  }, [slideDialogOpen]);

  useEffect(() => {
    if (!selectedId) {
      setDetail(null);
      setDetailError(null);
      setDetailLoading(false);
      return;
    }
    const started = epochRef.current;
    setDetailLoading(true);
    setDetailError(null);
    const load = async () => {
      const result =
        capabilities.mode === "seller" ? await getSellerStory(selectedId) : await getAdminStory(selectedId);
      if (started !== epochRef.current) return;
      setDetailLoading(false);
      if (result.state === "ok" && result.data) {
        setDetailError(null);
        setDetail(result.data);
        ownedDraftIdRef.current = result.data.storyId;
        setEditTitle(result.data.title);
        const nextLocale = result.data.locale;
        setEditLocale((prev) => nextLocale || prev);
        const start = isoToScheduleDraft(result.data.startAt);
        const end = isoToScheduleDraft(result.data.endAt);
        setDraftWindow({
          startDayIso: start.dayIso,
          startTime: start.time || "10:00",
          endDayIso: end.dayIso,
          endTime: end.time,
        });
        setNoPublicationEnd(!result.data.endAt);
        setDraftWindowError(null);
        setDirty(false);
        // Hydrate per-target labels from owning DTO only (scoped to this story remount).
        const nextLabels: Record<string, string> = {};
        for (const item of result.data.items) {
          if (item.ctaType === "product" && item.ctaTarget && item.ctaProductTitle?.trim()) {
            nextLabels[item.ctaTarget] = item.ctaProductTitle.trim();
          }
        }
        setProductLabelById(nextLabels);
        setSelectedProductLabel("");
        try {
          const pendingOpen = sessionStorage.getItem(OPEN_SLIDE_AFTER_LOAD_KEY);
          if (pendingOpen && pendingOpen === result.data.storyId) {
            sessionStorage.removeItem(OPEN_SLIDE_AFTER_LOAD_KEY);
            openNewSlideDialog();
          }
        } catch {
          /* sessionStorage unavailable */
        }
      } else if (result.state === "denied") setState("denied");
      else {
        setDetail(null);
        setDetailError(result.message ?? copy.detailLoadFailed);
      }
    };
    void load();
  }, [selectedId, capabilities.mode, reloadToken, copy.detailLoadFailed]);

  const openDetail = useCallback(
    (id: string) => {
      setSelectedId(id);
      router.push(`${basePath}/${encodeURIComponent(id)}`);
    },
    [basePath, router],
  );

  const openAdminOp = useCallback((kind: StoryAdminOpKind, rows: AdminStorySnapshot[]) => {
    if (capabilities.mode !== "admin" || rows.length === 0) return;
    const plan = classifyAdminOp(kind, rows);
    if (plan.eligible.length === 0) {
      // Nothing can run: this is a refusal, not a blue informational note.
      storyToast.error(explainZeroEligibleToast(plan));
      return;
    }
    setAdminOpPlan(plan);
    setAdminOpReason("");
  }, [capabilities.mode]);

  /** Stable grid status callbacks — dialog typing must not rebuild queryAdapter. */
  const onGridDenied = useCallback(() => setState("denied"), []);
  const onGridError = useCallback((detailMsg?: string) => {
    setMessage(detailMsg);
    setState("error");
  }, []);
  const onGridOk = useCallback(() => setState("ok"), []);
  const onGridRequestDelete = useCallback(
    (row: AdminStorySnapshot) => {
      if (capabilities.mode === "admin") {
        openAdminOp("delete", [row]);
        return;
      }
      setDeleteTarget(row);
      setDeleteOpen(true);
    },
    [capabilities.mode, openAdminOp],
  );
  const onGridSubmit = useCallback((row: AdminStorySnapshot) => {
    runSubmitRef.current(row);
  }, []);

  async function confirmAdminOp() {
    if (!adminOpPlan || !actorId || adminOpBusy) return;
    if (adminOpPlan.kind === "reject" && !adminOpReason.trim()) {
      storyToast.error(copy.rejectReasonRequired);
      return;
    }
    if (adminOpPlan.eligible.length === 0) {
      storyToast.error(explainZeroEligibleToast(adminOpPlan));
      setAdminOpPlan(null);
      setAdminOpReason("");
      return;
    }
    const planKind = adminOpPlan.kind;
    adminOpCancelledRef.current = false;
    setAdminOpBusy(true);
    try {
      const result = await executeAdminOpBatch({
        plan: adminOpPlan,
        gate,
        scope: { role: "admin", shopId: null, actorUserId: actorId },
        actorUserId: actorId,
        reason: adminOpReason.trim() || undefined,
        readLiveScope: () => {
          const live = readStoryScope("admin");
          return { role: "admin", shopId: null, actorUserId: live.actorUserId || actorId };
        },
        isCancelled: () => adminOpCancelledRef.current,
      });
      const announcement = formatAdminBatchToast(result, planKind);
      if (announcement.severity === "success") storyToast.success(announcement.message);
      else if (announcement.severity === "error") storyToast.error(announcement.message);
      else if (announcement.severity === "warning") storyToast.warning(announcement.message);
      else storyToast.info(announcement.message);

      setAdminOpPlan(null);
      setAdminOpReason("");
      refreshGrid();
      setReloadToken((v) => v + 1);

      // Genuine delete of the open detail → return to list; unresolved delete must not navigate.
      if (planKind === "delete") {
        const deletedIds = new Set(
          result.items.filter((i) => i.outcome === "succeeded").map((i) => i.storyId),
        );
        if (
          deletedIds.size > 0 &&
          ((selectedId && deletedIds.has(selectedId)) || (detail && deletedIds.has(detail.storyId)))
        ) {
          setSelectedId(null);
          setDetail(null);
          ownedDraftIdRef.current = null;
          router.push(basePath);
        }
      }
    } finally {
      setAdminOpBusy(false);
    }
  }

  async function runSubmit(row: AdminStorySnapshot) {
    if (inFlightRef.current) return;
    if (!actorId) {
      storyToast.error(copy.actorMissing);
      return;
    }
    const occurrenceKey = mintOccurrence();
    const body = { expectedVersion: row.versionToken, occurrenceKey };
    const isAdmin = capabilities.mode === "admin";
    const envelope: StoryIntentEnvelope = {
      version: 2,
      action: "submit",
      occurrenceKey,
      method: "POST",
      path: isAdmin ? `/v1/admin/stories/${row.storyId}/submit` : `/v1/seller/stories/${row.storyId}/submit`,
      routeTargetId: row.storyId,
      expectedVersion: row.versionToken,
      bodyText: JSON.stringify(body),
      uploadMeta: null,
      addBodyText: null,
      uploadedAssetId: null,
      children: [],
      role: isAdmin ? "admin" : "seller",
      shopId: row.sellerPartyId,
      actorUserId: actorId,
      recordedAt: new Date().toISOString(),
    };
    const outcome = await runDurableStoryMutation({
      gate,
      envelope,
      scope: isAdmin
        ? { role: "admin", shopId: null, actorUserId: actorId }
        : { role: "seller", shopId: row.sellerPartyId, actorUserId: actorId },
    });
    if (outcome.state === "ok") {
      storyToast.success(copy.submittedToast);
      refreshGrid();
      if (selectedId === row.storyId) setReloadToken((v) => v + 1);
    } else if (outcome.state === "failed") {
      if (outcome.code === "story.media.forbidden" || outcome.code === "story.content.notReady") {
        storyToast.error(copy.mediaRequiredHint);
      } else {
        storyToast.error(outcome.message);
      }
    } else storyToast.error(copy.sendFailed);
  }
  runSubmitRef.current = (row) => {
    void runSubmit(row);
  };

  function extractCreatedStoryId(data: unknown): string | null {
    if (!data || typeof data !== "object") return null;
    const root = data as { storyId?: unknown; data?: { storyId?: unknown } };
    const id = root.storyId ?? root.data?.storyId;
    if (typeof id === "string" && id.trim()) return id.trim();
    return null;
  }

  /** Shared create completion: normal success and exact recovery replay both adopt one owning draft. */
  function adoptBoundCreatedDraft(storyId: string, options?: { openSlideAfter?: boolean }): void {
    ownedDraftIdRef.current = storyId;
    setShowCreate(false);
    setDirty(false);
    if (options?.openSlideAfter) {
      try {
        sessionStorage.setItem(OPEN_SLIDE_AFTER_LOAD_KEY, storyId);
      } catch {
        /* ignore */
      }
    }
    openDetail(storyId);
  }

  async function createDraft(options?: { openSlideAfter?: boolean }): Promise<string | null> {
    const existingId = ownedDraftIdRef.current || selectedId;
    if (existingId) {
      if (options?.openSlideAfter) {
        try {
          sessionStorage.setItem(OPEN_SLIDE_AFTER_LOAD_KEY, existingId);
        } catch {
          /* ignore */
        }
        if (selectedId === existingId && detail) {
          openNewSlideDialog();
          try {
            sessionStorage.removeItem(OPEN_SLIDE_AFTER_LOAD_KEY);
          } catch {
            /* ignore */
          }
        } else {
          openDetail(existingId);
        }
      }
      return existingId;
    }
    if (inFlightRef.current) return null;
    if (!actorId) {
      storyToast.error(copy.actorMissing);
      return null;
    }
    const title = draftTitle.trim();
    if (!title) {
      storyToast.error(copy.titleRequired);
      return null;
    }
    if (capabilities.mode === "admin" && adminShopMode === "shop" && !adminShopId.trim()) {
      storyToast.error(copy.shopPickRequired);
      return null;
    }
    const windowParse = parseOptionalPublicationWindow(
      {
        ...draftWindow,
        endDayIso: noPublicationEnd ? "" : draftWindow.endDayIso,
        endTime: noPublicationEnd ? "" : draftWindow.endTime,
      },
      { requireEnd: !noPublicationEnd },
    );
    if (!windowParse.ok) {
      setDraftWindowError(windowParse.message);
      storyToast.error(windowParse.message);
      return null;
    }
    setDraftWindowError(null);
    const occurrenceKey = mintOccurrence();
    const shopId =
      capabilities.mode === "admin" && adminShopMode === "shop" ? adminShopId.trim() : sellerShopId;
    const body = {
      title,
      locale: editLocale || null,
      occurrenceKey,
      sellerPartyId: capabilities.mode === "admin" && adminShopMode === "shop" ? adminShopId.trim() : null,
      startAt: windowParse.startAt,
      endAt: windowParse.endAt,
    };
    const envelope: StoryIntentEnvelope = {
      version: 2,
      action: "create",
      occurrenceKey,
      method: "POST",
      path: capabilities.mode === "seller" ? "/v1/seller/stories" : "/v1/admin/stories",
      routeTargetId: null,
      expectedVersion: null,
      bodyText: JSON.stringify(body),
      uploadMeta: null,
      addBodyText: null,
      uploadedAssetId: null,
      children: [],
      role: capabilities.mode,
      shopId,
      actorUserId: actorId,
      recordedAt: new Date().toISOString(),
    };
    const outcome = await runDurableStoryMutation({
      gate,
      envelope,
      scope: { role: capabilities.mode, shopId, actorUserId: actorId },
    });
    if (outcome.state === "ok") {
      const id = extractCreatedStoryId(outcome.data);
      storyToast.success(copy.draftSaved);
      if (id) {
        adoptBoundCreatedDraft(id, options);
        return id;
      }
      // Bound create without extractable id must keep evidence visible for recovery.
      refreshGrid();
      return null;
    }
    if (outcome.state === "failed") storyToast.error(outcome.message);
    else if (outcome.state === "blocked") storyToast.error(copy.pendingContinueFailed);
    else storyToast.error(copy.sendFailed);
    // Unknown/failed/blocked create retains storage; remount recovery card without a second create.
    refreshGrid();
    return null;
  }

  if (state === "denied") return <Denied retry={refreshGrid} copy={copy} />;

  // Seller without an explicit shop: no silent demo-shop fallback, ask for a lawful context.
  if (capabilities.mode === "seller" && !sellerShopId) {
    return (
      <main data-testid="seller-stories">
        <div data-testid="story-choose-context" className="mx-auto max-w-xl p-6">
          <ErrorState
            title={copy.chooseShopTitle}
            detail={copy.chooseShopDetail}
            onRetry={() => router.push(`${storyLocalePrefix()}/vendor-panel`)}
            retryLabel={copy.chooseShopAction}
          />
        </div>
      </main>
    );
  }

  const title = capabilities.mode === "seller" ? copy.sellerTitle : copy.adminTitle;
  const description = capabilities.mode === "seller" ? copy.sellerDescription : copy.adminDescription;
  const testId = capabilities.mode === "seller" ? "seller-stories" : "admin-stories";
  const showCreateWorkspace = createMode && !selectedId;
  const isListSurface = !createMode && !selectedId;
  const sellerEditable = detail ? hasAction(detail, "update") || hasAction(detail, "add_item") : false;
  const loadMoreShops = () => {
    if (shopLoadingRef.current || !shopHasMoreRef.current) return;
    void loadShopPage(shopPageRef.current + 1, shopSearchRef.current, "append");
  };

  return (
    <main data-testid={testId}>
      <StoryPageHeader
        breadcrumb={copy.breadcrumbHome}
        title={title}
        description={description}
        actions={
          <>
            {pendingActs.length > 0 ? (
              <button
                type="button"
                className={storyButtonClass("secondary")}
                title={`${copy.pendingHeading} (${pendingActs.length.toLocaleString(numberLocale)})`}
                aria-label={`${copy.pendingHeading} (${pendingActs.length.toLocaleString(numberLocale)})`}
                data-testid="story-pending-recovery-open"
                onClick={() => setPendingRecoveryOpen(true)}
              >
                <AlertTriangle className="h-4 w-4" aria-hidden="true" />
                <span className="sr-only">{copy.pendingHeading}</span>
              </button>
            ) : null}
            {isListSurface && capabilities.canReview ? (
              <label className="flex min-h-11 items-center gap-2 rounded-xl border border-border bg-surface px-3 text-xs font-medium text-muted">
                {copy.filterReviewLabel}
                <select
                  className="min-h-9 min-w-28 border-0 bg-transparent py-1 text-sm font-normal text-foreground outline-none"
                  value={reviewFilter}
                  onChange={(e) => {
                    setReviewFilter(e.target.value);
                    refreshGrid();
                  }}
                  data-testid="admin-story-review-filter"
                >
                  <option value="">{copy.filterAll}</option>
                  <option value="Submitted">{copy.filterPending}</option>
                  <option value="Approved">{copy.filterApproved}</option>
                  <option value="Rejected">{copy.filterRejected}</option>
                </select>
              </label>
            ) : null}
            {isListSurface && capabilities.canCreate ? (
              <button
                type="button"
                className={storyButtonClass("primary")}
                onClick={() => router.push(`${basePath}/new`)}
                data-testid="story-create-open"
              >
                <Plus className="h-4 w-4" aria-hidden="true" />
                {copy.createButton}
              </button>
            ) : null}
          </>
        }
      />


      {isListSurface ? (
        <StoryLiveGrid
          capabilities={capabilities}
          reviewFilter={reviewFilter}
          reloadToken={reloadToken}
          copy={copy}
          numberLocale={numberLocale}
          onDenied={onGridDenied}
          onError={onGridError}
          onOk={onGridOk}
          onOpenDetail={openDetail}
          onSubmit={onGridSubmit}
          onRequestDelete={onGridRequestDelete}
          onRequestAdminOp={openAdminOp}
          errorMessage={message}
          onRetry={refreshGrid}
          showError={state === "error"}
        />
      ) : null}

      {showCreateWorkspace ? (
        <StorySectionCard
          testId="story-create-workspace"
          className="mt-5"
          title={copy.storyFormHeading}
          description={copy.storyFormHint}
          actions={
            <button
              type="button"
              className={storyButtonClass("secondary")}
              data-testid="story-create-back"
              onClick={() => requestLeave(basePath)}
            >
              {copy.backToList}
            </button>
          }
        >
          <div className="grid gap-5 lg:grid-cols-[minmax(0,1.5fr)_minmax(280px,0.9fr)]">
            <div className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <label className="block text-sm">
                    <span className="mb-1 block font-medium text-muted">{copy.titleLabel}</span>
                    <input
                      className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                      value={draftTitle}
                      onChange={(e) => {
                        setDraftTitle(e.target.value);
                        setDirty(true);
                      }}
                      data-testid="story-create-title"
                    />
                  </label>
                  <label className="block text-sm">
                    <span className="mb-1 block font-medium text-muted">{copy.languageLabel}</span>
                    <select
                      className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                      value={editLocale}
                      onChange={(e) => {
                        setEditLocale(e.target.value);
                        setDirty(true);
                      }}
                      data-testid="story-create-locale"
                    >
                      {localeOptions.map((row) => (
                        <option key={row.code} value={row.code}>
                          {row.name}
                          {row.isDefault ? copy.localeDefaultSuffix : ""}
                        </option>
                      ))}
                    </select>
                  </label>
                </div>
                {capabilities.mode === "seller" ? (
                  <p className="rounded-xl border border-border bg-secondary/60 px-4 py-3 text-sm" data-testid="story-seller-shop-fixed">
                    <Store className="mb-0.5 me-1 inline h-4 w-4 text-muted" aria-hidden="true" />
                    {copy.sellerShopFixedPrefix} {sellerShopLabel.trim() || copy.sellerShopFallback}
                    <span className="mt-1 block text-xs text-muted">{copy.sellerShopFixedHint}</span>
                  </p>
                ) : null}
                {capabilities.mode === "admin" ? (
                  <fieldset className="space-y-2 rounded-xl border border-border p-3 text-sm">
                    <legend className="px-1 text-xs font-medium text-muted">{copy.placementLegend}</legend>
                    <div className="grid gap-2 sm:grid-cols-2">
                      <label className="flex min-h-11 cursor-pointer items-center gap-2 rounded-lg px-2 hover:bg-secondary">
                        <input
                          type="radio"
                          checked={adminShopMode === "platform"}
                          data-testid="story-admin-platform-mode"
                          onChange={() => {
                            setAdminShopMode("platform");
                            setAdminShopId("");
                            setAdminShopName("");
                          }}
                        />
                        {copy.mainStorefront}
                      </label>
                      <label className="flex min-h-11 cursor-pointer items-center gap-2 rounded-lg px-2 hover:bg-secondary">
                        <input
                          type="radio"
                          checked={adminShopMode === "shop"}
                          data-testid="story-admin-shop-mode"
                          onChange={() => setAdminShopMode("shop")}
                        />
                        {copy.forOneShop}
                      </label>
                    </div>
                    {adminShopMode === "shop" ? (
                      <div className="space-y-2 border-t border-border pt-3" data-testid="story-admin-shop-picker">
                        <input
                          className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                          placeholder={copy.shopSearchPlaceholder}
                          aria-label={copy.shopSearchPlaceholder}
                          value={shopSearch}
                          onChange={(e) => setShopSearch(e.target.value)}
                          data-testid="story-admin-shop-search"
                        />
                        {adminShopName ? (
                          <p className="text-sm text-muted">
                            {copy.selectedShopPrefix} <span className="font-bold text-foreground">{adminShopName}</span>
                          </p>
                        ) : (
                          <p className="text-sm text-danger">{copy.shopPickRequired}</p>
                        )}
                        <ul
                          className="max-h-48 overflow-auto rounded-xl border border-border bg-surface"
                          data-testid="story-admin-shop-list"
                          aria-busy={shopLoading || shopLoadingMore}
                          onScroll={(event) => {
                            const el = event.currentTarget;
                            if (el.scrollHeight - el.scrollTop - el.clientHeight <= 48) loadMoreShops();
                          }}
                        >
                          {shopOptions.map((row) => (
                            <li key={row.sellerPartyId}>
                              <button
                                type="button"
                                className="flex min-h-11 w-full items-center px-3 py-2 text-right text-sm transition-colors hover:bg-secondary"
                                onClick={() => {
                                  setAdminShopId(row.sellerPartyId);
                                  setAdminShopName(row.displayName);
                                }}
                                data-testid="story-admin-shop-option"
                              >
                                {row.displayName}
                              </button>
                            </li>
                          ))}
                        </ul>
                        {shopLoading || shopLoadingMore ? (
                          <p className="text-xs text-muted" data-testid="story-admin-shop-loading">
                            {copy.loadingShops}
                          </p>
                        ) : null}
                        {shopError ? (
                          <div className="flex flex-wrap items-center gap-2" data-testid="story-admin-shop-error">
                            <p className="text-xs text-danger">{shopError}</p>
                            <button
                              type="button"
                              className={storyButtonClass("secondary")}
                              onClick={() => void loadShopPage(1, shopSearch, "reset")}
                            >
                              {faWorkspaceMessages.retry}
                            </button>
                          </div>
                        ) : null}
                        {!shopLoading && !shopLoadingMore && !shopError && shopOptions.length === 0 ? (
                          <p className="text-xs text-muted" data-testid="story-admin-shop-empty">
                            {copy.noShopsFound}
                          </p>
                        ) : null}
                        {!shopLoading && !shopLoadingMore && !shopError && shopHasMore ? (
                          <p className="text-xs text-muted" data-testid="story-admin-shop-more">
                            {copy.shopScrollHint}
                          </p>
                        ) : null}
                      </div>
                    ) : null}
                  </fieldset>
                ) : null}
                <StoryPublicationWindowFields
                  testIdPrefix="story-create-window"
                  copy={copy}
                  draftWindow={draftWindow}
                  noPublicationEnd={noPublicationEnd}
                  error={draftWindowError}
                  onDraftChange={(next) => {
                    setDraftWindow(next);
                    setDirty(true);
                  }}
                  onNoEndChange={(noEnd) => {
                    setNoPublicationEnd(noEnd);
                    setDirty(true);
                  }}
                />
                <div className="flex justify-end">
                  <button
                    type="button"
                    className={storyButtonClass("primary")}
                    onClick={() => void createDraft()}
                    data-testid="story-create-save"
                  >
                    {copy.saveForm}
                  </button>
                </div>
            </div>
            <StoryPreviewPanel
              items={[]}
              copy={copy}
              numberLocale={numberLocale}
              testId="story-create-preview"
            />
          </div>
        </StorySectionCard>
      ) : null}
      {showCreateWorkspace ? (
        <StorySectionCard
          testId="story-create-slides"
          className="mt-4"
          title={copy.itemsHeading}
          actions={
            capabilities.canCreate ? (
              <button
                type="button"
                className={storyButtonClass("primary")}
                data-testid="story-slide-open-new"
                onClick={() => {
                  void createDraft({ openSlideAfter: true });
                }}
              >
                <Plus className="h-4 w-4" aria-hidden="true" />
                {copy.addItem}
              </button>
            ) : null
          }
        >
          <StoryEmptyState
            compact
            testId="story-create-slides-empty"
            title={copy.previewEmpty}
            description={copy.slidesEmptyHint}
            icon={<Layers className="h-5 w-5" aria-hidden="true" />}
          />
        </StorySectionCard>
      ) : null}
      {selectedId && !detail && detailError ? (
        <section
          className="mt-5 rounded-2xl border border-danger/40 bg-danger-soft/40 px-5 py-6"
          data-testid="story-detail-error"
          role="alert"
        >
          <p className="text-sm font-bold text-danger">{copy.detailLoadFailed}</p>
          <p className="mt-1 text-xs text-muted">{detailError}</p>
          <div className="mt-4 flex flex-wrap gap-2">
            <button
              type="button"
              className={storyButtonClass("primary")}
              data-testid="story-detail-error-retry"
              disabled={detailLoading}
              onClick={() => setReloadToken((v) => v + 1)}
            >
              {detailLoading ? copy.detailLoading : copy.detailRetry}
            </button>
            <button
              type="button"
              className={storyButtonClass("secondary")}
              data-testid="story-detail-error-back"
              onClick={() => {
                setSelectedId(null);
                setDetail(null);
                setDetailError(null);
                router.push(basePath);
              }}
            >
              {copy.detailBack}
            </button>
          </div>
        </section>
      ) : selectedId && !detail && detailLoading ? (
        <section
          className="mt-5 rounded-2xl border border-border bg-surface-elevated px-5 py-6 text-sm text-muted"
          data-testid="story-detail-loading"
          aria-busy="true"
        >
          {copy.detailLoading}
        </section>
      ) : null}
      {selectedId && detail ? (
        <section className="mt-5 overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm" data-testid="story-detail-workspace">
          <div className="flex flex-col gap-3 border-b border-border bg-background/30 px-5 py-4 lg:flex-row lg:items-start lg:justify-between">
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="min-w-0 text-lg font-bold text-foreground">{detail.title}</h2>
                <StoryStatusPill label={displayState(detail, copy)} tone={displayStateTone(detail)} testId="story-detail-status" />
              </div>
              <div className="mt-2 space-y-1">
                <p className="text-sm text-muted" data-testid="story-detail-location">
                  {copy.locationPrefix} {shopLabel(detail, copy)}
                </p>
                {capabilities.showOrigin ? (
                  <span className="block text-xs text-muted" data-testid="story-detail-origin">
                    {detail.origin === "Admin"
                      ? copy.originCreatorAdmin
                      : detail.origin === "Seller"
                        ? copy.originCreatorSeller
                        : originLabels[detail.origin] ?? detail.origin}
                  </span>
                ) : null}
                <p className="text-xs text-muted">{copy.detailWorkspaceHint}</p>
              </div>
            </div>
            <button
              type="button"
              className={storyButtonClass("secondary")}
              data-testid="story-detail-back"
              onClick={() => requestLeave(basePath)}
            >
              {copy.backToList}
            </button>
          </div>

          <div className="space-y-5 p-5">
          {detail.rejectionReason ? (
            <div className="rounded-xl border border-danger/20 bg-danger/10 px-4 py-3 text-sm text-danger" data-testid="story-rejection-reason">
              <span className="font-bold">دلیل رد: </span>
              {detail.rejectionReason}
            </div>
          ) : null}

          <StoryActionBar>
            {hasAction(detail, "approve") ? (
              <button
                type="button"
                className={storyButtonClass("primary")}
                data-testid="story-detail-approve"
                onClick={() => openAdminOp("approve", [detail])}
              >
                {copy.approve}
              </button>
            ) : null}
            {hasAction(detail, "approve_and_publish") ? (
              <button
                type="button"
                className={storyButtonClass("primary")}
                onClick={() => setPreviewConfirm("publish")}
              >
                {copy.approveAndPublish}
              </button>
            ) : null}
            {hasAction(detail, "approve_and_schedule") ? (
              <button
                type="button"
                className={storyButtonClass("secondary")}
                onClick={() => {
                  setScheduleMode("approve_schedule");
                  const start = isoToScheduleDraft(detail.startAt);
                  const end = isoToScheduleDraft(detail.endAt);
                  setScheduleFieldError(null);
                  setScheduleDraft({
                    startDayIso: start.dayIso,
                    startTime: start.time || "10:00",
                    endDayIso: end.dayIso,
                    endTime: end.time,
                  });
                  setScheduleOpen(true);
                }}
              >
                {copy.approveAndSchedule}
              </button>
            ) : null}
            {hasAction(detail, "reject") ? (
              <button
                type="button"
                className={storyButtonClass("danger")}
                data-testid="story-detail-reject"
                onClick={() => openAdminOp("reject", [detail])}
              >
                {copy.reject}
              </button>
            ) : null}
            {hasAction(detail, "publish_now") ? (
              <button
                type="button"
                className={storyButtonClass("primary")}
                data-testid="story-detail-publish-now"
                onClick={() => {
                  if (!actorId) return;
                  const key = mintOccurrence();
                  const body = { expectedVersion: detail.versionToken, occurrenceKey: key };
                  const envelope: StoryIntentEnvelope = {
                    version: 2,
                    action: "publish_now",
                    occurrenceKey: key,
                    method: "POST",
                    path: `/v1/admin/stories/${detail.storyId}/enable`,
                    routeTargetId: detail.storyId,
                    expectedVersion: detail.versionToken,
                    bodyText: JSON.stringify(body),
                    uploadMeta: null,
                    addBodyText: null,
                    uploadedAssetId: null,
                    children: [],
                    role: "admin",
                    shopId: detail.sellerPartyId,
                    actorUserId: actorId,
                    recordedAt: new Date().toISOString(),
                  };
                  void runDurableStoryMutation({
                    gate,
                    envelope,
                    scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                  }).then((outcome) => {
                    if (outcome.state === "ok") {
                      storyToast.success(copy.publishedToast);
                      refreshGrid();
                      setReloadToken((v) => v + 1);
                    } else if (outcome.state === "failed") storyToast.error(outcome.message);
                    else storyToast.error(copy.sendFailed);
                  });
                }}
              >
                {copy.publishNow}
              </button>
            ) : null}
            {hasAction(detail, "schedule") ? (
              <button
                type="button"
                className={storyButtonClass("secondary")}
                onClick={() => {
                  setScheduleMode("schedule");
                  const start = isoToScheduleDraft(detail.startAt);
                  const end = isoToScheduleDraft(detail.endAt);
                  setScheduleFieldError(null);
                  setScheduleDraft({
                    startDayIso: start.dayIso,
                    startTime: start.time || "10:00",
                    endDayIso: end.dayIso,
                    endTime: end.time,
                  });
                  setScheduleOpen(true);
                }}
              >
                {copy.scheduleDisplay}
              </button>
            ) : null}
            {hasAction(detail, "stop") ? (
              <button
                type="button"
                className={storyButtonClass("danger")}
                onClick={() => {
                  if (!actorId) return;
                  const key = mintOccurrence();
                  const body = { expectedVersion: detail.versionToken, occurrenceKey: key };
                  const envelope: StoryIntentEnvelope = {
                    version: 2,
                    action: "stop",
                    occurrenceKey: key,
                    method: "POST",
                    path:
                      capabilities.mode === "seller"
                        ? `/v1/seller/stories/${detail.storyId}/stop`
                        : `/v1/admin/stories/${detail.storyId}/disable`,
                    routeTargetId: detail.storyId,
                    expectedVersion: detail.versionToken,
                    bodyText: JSON.stringify(body),
                    uploadMeta: null,
                    addBodyText: null,
                    uploadedAssetId: null,
                    children: [],
                    role: capabilities.mode,
                    shopId: detail.sellerPartyId ?? sellerShopId,
                    actorUserId: actorId,
                    recordedAt: new Date().toISOString(),
                  };
                  void runDurableStoryMutation({
                    gate,
                    envelope,
                    scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                  }).then((outcome) => {
                    if (outcome.state === "ok") {
                      storyToast.success(copy.stoppedToast);
                      refreshGrid();
                      setReloadToken((v) => v + 1);
                    } else if (outcome.state === "failed") storyToast.error(outcome.message);
                    else storyToast.error(copy.stopFailed);
                  });
                }}
              >
                {copy.stopDisplay}
              </button>
            ) : null}
            {hasAction(detail, "return_to_draft") ? (
              <button
                type="button"
                className={storyButtonClass("secondary")}
                onClick={() => {
                  if (!actorId) return;
                  const key = mintOccurrence();
                  const body = { expectedVersion: detail.versionToken, occurrenceKey: key };
                  const envelope: StoryIntentEnvelope = {
                    version: 2,
                    action: "return_to_draft",
                    occurrenceKey: key,
                    method: "POST",
                    path: `/v1/seller/stories/${detail.storyId}/return-to-draft`,
                    routeTargetId: detail.storyId,
                    expectedVersion: detail.versionToken,
                    bodyText: JSON.stringify(body),
                    uploadMeta: null,
                    addBodyText: null,
                    uploadedAssetId: null,
                    children: [],
                    role: "seller",
                    shopId: detail.sellerPartyId ?? sellerShopId,
                    actorUserId: actorId,
                    recordedAt: new Date().toISOString(),
                  };
                  void runDurableStoryMutation({
                    gate,
                    envelope,
                    scope: { role: "seller", shopId: sellerShopId, actorUserId: actorId },
                  }).then((outcome) => {
                    if (outcome.state === "ok") {
                      storyToast.success(copy.returnedToast);
                      refreshGrid();
                      setReloadToken((v) => v + 1);
                    } else if (outcome.state === "failed") storyToast.error(outcome.message);
                    else storyToast.error(copy.returnFailed);
                  });
                }}
              >
                {copy.returnToDraft}
              </button>
            ) : null}
            {hasAction(detail, "submit") ? (
              <button
                type="button"
                className={storyButtonClass("primary")}
                data-testid="story-detail-submit"
                onClick={() => void runSubmit(detail)}
              >
                {copy.rowSubmit}
              </button>
            ) : null}
          </StoryActionBar>

          {capabilities.canEdit && sellerEditable ? (
            <StorySectionCard
              title={copy.storyFormHeading}
              description={copy.storyFormHint}
            >
              <div className="grid gap-5 lg:grid-cols-[minmax(0,1.5fr)_minmax(280px,0.9fr)]">
                <div className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <label className="block text-sm">
                  <span className="mb-1 block text-muted">{copy.titleLabel}</span>
                  <input
                    className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                    value={editTitle}
                    onChange={(e) => {
                      setEditTitle(e.target.value);
                      setDirty(true);
                    }}
                    data-testid="story-edit-title"
                  />
                </label>
                <label className="block text-sm">
                  <span className="mb-1 block text-muted">{copy.languageLabel}</span>
                  <select
                    className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                    value={editLocale}
                    onChange={(e) => {
                      setEditLocale(e.target.value);
                      setDirty(true);
                    }}
                    data-testid="story-edit-locale"
                  >
                    {localeOptions.map((row) => (
                      <option key={row.code} value={row.code}>
                        {row.name}
                        {row.isDefault ? copy.localeDefaultSuffix : ""}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
              <StoryPublicationWindowFields
                testIdPrefix="story-edit-window"
                copy={copy}
                draftWindow={draftWindow}
                noPublicationEnd={noPublicationEnd}
                error={draftWindowError}
                onDraftChange={(next) => {
                  setDraftWindow(next);
                  setDirty(true);
                }}
                onNoEndChange={(noEnd) => {
                  setNoPublicationEnd(noEnd);
                  setDirty(true);
                }}
              />
              {detail.items.length > 0 && !detail.contentReady ? (
                <p className="text-sm text-warning" data-testid="story-media-required-hint">
                  {copy.mediaRequiredHint}
                </p>
              ) : null}
              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  className={storyButtonClass("primary")}
                  data-testid="story-save-title-locale"
                  onClick={() => {
                    if (!detail || !actorId) return;
                    const windowParse = parseOptionalPublicationWindow(
                      {
                        ...draftWindow,
                        endDayIso: noPublicationEnd ? "" : draftWindow.endDayIso,
                        endTime: noPublicationEnd ? "" : draftWindow.endTime,
                      },
                      { requireEnd: !noPublicationEnd },
                    );
                    if (!windowParse.ok) {
                      setDraftWindowError(windowParse.message);
                      storyToast.error(windowParse.message);
                      return;
                    }
                    setDraftWindowError(null);
                    const key = mintOccurrence();
                    const body = {
                      title: editTitle.trim(),
                      locale: editLocale || null,
                      market: detail.market ?? null,
                      coverMediaAssetId: detail.coverMediaAssetId ?? null,
                      coverMediaUrl: detail.coverMediaUrl ?? null,
                      ctaType: detail.ctaType ?? "none",
                      ctaTarget: detail.ctaTarget ?? null,
                      startAt: windowParse.startAt,
                      endAt: windowParse.endAt,
                      expectedVersion: detail.versionToken,
                      occurrenceKey: key,
                    };
                    const envelope: StoryIntentEnvelope = {
                      version: 2,
                      action: "update",
                      occurrenceKey: key,
                      method: "PUT",
                      path:
                        capabilities.mode === "seller"
                          ? `/v1/seller/stories/${detail.storyId}`
                          : `/v1/admin/stories/${detail.storyId}`,
                      routeTargetId: detail.storyId,
                      expectedVersion: detail.versionToken,
                      bodyText: JSON.stringify(body),
                      uploadMeta: null,
                      addBodyText: null,
                      uploadedAssetId: null,
                      children: [],
                      role: capabilities.mode,
                      shopId: detail.sellerPartyId ?? sellerShopId,
                      actorUserId: actorId,
                      recordedAt: new Date().toISOString(),
                    };
                    void runDurableStoryMutation({
                      gate,
                      envelope,
                      scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                    }).then((outcome) => {
                      if (outcome.state === "ok") {
                        storyToast.success(copy.savedToast);
                        setDirty(false);
                        setReloadToken((v) => v + 1);
                        refreshGrid();
                      } else if (outcome.state === "failed") storyToast.error(outcome.message);
                      else storyToast.error(copy.sendFailed);
                    });
                  }}
                >
                  {copy.saveTitleLocale}
                </button>
                {hasAction(detail, "delete") ? (
                  <button
                    type="button"
                    className={storyButtonClass("danger")}
                    data-testid="story-delete-open"
                    onClick={() => {
                      if (capabilities.mode === "admin") {
                        openAdminOp("delete", [detail]);
                        return;
                      }
                      setDeleteTarget(detail);
                      setDeleteOpen(true);
                    }}
                  >
                    {copy.deleteStory}
                  </button>
                ) : null}
              </div>
                </div>
                <StoryPreviewPanel
                  items={detail.items}
                  copy={copy}
                  numberLocale={numberLocale}
                  testId="story-edit-preview"
                />
              </div>
            </StorySectionCard>
          ) : null}

          <StorySectionCard
            className="mt-4"
            title={`${copy.itemsHeading} (${detail.items.length.toLocaleString(numberLocale)})`}
            actions={
              capabilities.canEdit && sellerEditable ? (
                <button
                  type="button"
                  className={storyButtonClass("primary")}
                  data-testid="story-slide-open-new"
                  onClick={() => openNewSlideDialog()}
                >
                  <Plus className="h-4 w-4" aria-hidden="true" />
                  {copy.addItem}
                </button>
              ) : null
            }
          >
          {detail.items.length === 0 ? (
            <StoryEmptyState
              compact
              testId="story-slides-empty"
              title={copy.previewEmpty}
              description={copy.slidesEmptyHint}
              icon={<ImagePlus className="h-5 w-5" aria-hidden="true" />}
            />
          ) : null}
          <ul className="space-y-3 text-sm" data-testid="story-detail-preview">
            {detail.items.map((item, index) => (
              <li key={item.storyItemId} className="rounded-xl border border-border bg-surface p-3" data-testid="story-slide-row">
                <div className="flex flex-wrap items-center gap-3">
                  <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary text-xs font-bold text-muted" aria-hidden="true">
                    {(index + 1).toLocaleString(numberLocale)}
                  </span>
                  {item.mediaType === "video" && item.mediaUrl ? (
                    // eslint-disable-next-line jsx-a11y/media-has-caption
                    <video src={item.mediaUrl} className="h-16 w-16 shrink-0 rounded-lg border border-border object-cover" muted playsInline controls />
                  ) : item.mediaUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={item.mediaUrl} alt="" className="h-16 w-16 shrink-0 rounded-lg border border-border object-cover" />
                  ) : null}
                  <div className="min-w-0 flex-1">
                    <p className="font-medium text-foreground">
                      {item.mediaType === "video" ? copy.videoLabel : copy.photoLabel}
                      {item.durationMs
                        ? ` · ${Math.round(item.durationMs / 1000).toLocaleString(numberLocale)} ${copy.secondsSuffix}`
                        : ""}
                    </p>
                    {item.caption ? <p className="text-muted">{item.caption}</p> : null}
                    <p className="text-xs text-muted" data-testid="story-slide-destination">
                      {copy.destinationPrefix} {destinationLabelForItem(item)}
                    </p>
                    {capabilities.mode === "seller" && !item.mediaAssetId ? (
                      <p className="mt-1 text-xs text-warning" data-testid="story-slide-media-recovery">
                        {copy.slideNeedsOwnedMedia}
                      </p>
                    ) : null}
                  </div>
                  {capabilities.canEdit && sellerEditable ? (
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        className={storyButtonClass("secondary")}
                        data-testid="story-slide-edit"
                        onClick={() => openEditSlideDialog(item)}
                      >
                        {copy.editItem}
                      </button>
                      <button
                        type="button"
                        className={storyButtonClass("secondary")}
                        disabled={index === 0}
                        onClick={() => {
                          if (!actorId) return;
                          const ids = detail.items.map((row) => row.storyItemId);
                          const next = [...ids];
                          const tmp = next[index - 1]!;
                          next[index - 1] = next[index]!;
                          next[index] = tmp;
                          const key = mintOccurrence();
                          const body = { itemIds: next, expectedVersion: detail.versionToken, occurrenceKey: key };
                          const envelope: StoryIntentEnvelope = {
                            version: 2,
                            action: "reorder_items",
                            occurrenceKey: key,
                            method: "PUT",
                            path:
                              capabilities.mode === "seller"
                                ? `/v1/seller/stories/${detail.storyId}/items/reorder`
                                : `/v1/admin/stories/${detail.storyId}/items/reorder`,
                            routeTargetId: detail.storyId,
                            expectedVersion: detail.versionToken,
                            bodyText: JSON.stringify(body),
                            uploadMeta: null,
                            addBodyText: null,
                            uploadedAssetId: null,
                            children: [],
                            role: capabilities.mode,
                            shopId: detail.sellerPartyId ?? sellerShopId,
                            actorUserId: actorId,
                            recordedAt: new Date().toISOString(),
                          };
                          void runDurableStoryMutation({
                            gate,
                            envelope,
                            scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                          }).then((outcome) => {
                            if (outcome.state === "ok") {
                              storyToast.success(copy.reorderToast);
                              setReloadToken((v) => v + 1);
                            } else if (outcome.state === "failed") storyToast.error(outcome.message);
                            else storyToast.error(copy.reorderFailed);
                          });
                        }}
                      >
                        {copy.moveUp}
                      </button>
                      <button
                        type="button"
                        className={storyButtonClass("danger")}
                        onClick={() => {
                          if (!actorId) return;
                          const key = mintOccurrence();
                          const body = { expectedVersion: detail.versionToken, occurrenceKey: key };
                          const envelope: StoryIntentEnvelope = {
                            version: 2,
                            action: "remove_item",
                            occurrenceKey: key,
                            method: "DELETE",
                            path:
                              capabilities.mode === "seller"
                                ? `/v1/seller/stories/${detail.storyId}/items/${item.storyItemId}`
                                : `/v1/admin/stories/${detail.storyId}/items/${item.storyItemId}`,
                            routeTargetId: detail.storyId,
                            expectedVersion: detail.versionToken,
                            bodyText: JSON.stringify(body),
                            uploadMeta: null,
                            addBodyText: null,
                            uploadedAssetId: null,
                            children: [],
                            role: capabilities.mode,
                            shopId: detail.sellerPartyId ?? sellerShopId,
                            actorUserId: actorId,
                            recordedAt: new Date().toISOString(),
                          };
                          void runDurableStoryMutation({
                            gate,
                            envelope,
                            scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                          }).then((outcome) => {
                            if (outcome.state === "ok") {
                              storyToast.success(copy.slideUpdated);
                              setReloadToken((v) => v + 1);
                              refreshGrid();
                            } else if (outcome.state === "failed") storyToast.error(outcome.message);
                            else storyToast.error(copy.sendFailed);
                          });
                        }}
                      >
                        {copy.removeSlide}
                      </button>
                    </div>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
          </StorySectionCard>
          </div>
        </section>
      ) : null}

      <Dialog
        open={slideDialogOpen}
        onClose={closeSlideEditor}
        title={editingItemId ? copy.editSlideTitle : copy.newSlideTitle}
      >
        <div className="space-y-3 p-1" data-testid="story-slide-editor">
          {!editingItemId ? (
            <div className="space-y-2 text-sm">
              <span className="mb-1 block text-muted">{copy.mediaLabel}</span>
              <input
                ref={slideFileInputRef}
                type="file"
                accept="image/*,video/*"
                className="sr-only"
                onChange={(e) => setSlideFile(e.target.files?.[0] ?? null)}
                data-testid="story-slide-file"
              />
              <button
                type="button"
                className={storyButtonClass("secondary")}
                onClick={() => slideFileInputRef.current?.click()}
                data-testid="story-slide-file-pick"
              >
                {slideFile ? `${copy.pickedFilePrefix} ${slideFile.name}` : copy.pickFile}
              </button>
              {slidePreviewUrl ? (
                <div className="space-y-1" data-testid="story-slide-local-preview">
                  <p className="text-xs text-muted">{copy.localPreviewLabel}</p>
                  {isVideoFile(slideFile) ? (
                    // eslint-disable-next-line jsx-a11y/media-has-caption
                    <video
                      src={slidePreviewUrl}
                      className="max-h-48 w-full rounded-xl object-contain"
                      controls
                      playsInline
                    />
                  ) : (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={slidePreviewUrl} alt="" className="max-h-48 w-full rounded-xl object-contain" />
                  )}
                </div>
              ) : null}
            </div>
          ) : (
            <div className="space-y-2">
              <p className="text-sm text-muted">{copy.keepMediaHint}</p>
              {detail?.items.find((row) => row.storyItemId === editingItemId)?.mediaUrl ? (
                <div data-testid="story-slide-existing-preview">
                  {detail.items.find((row) => row.storyItemId === editingItemId)?.mediaType === "video" ? (
                    // eslint-disable-next-line jsx-a11y/media-has-caption
                    <video
                      src={detail.items.find((row) => row.storyItemId === editingItemId)!.mediaUrl!}
                      className="max-h-48 w-full rounded-xl object-contain"
                      controls
                      playsInline
                    />
                  ) : (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                      src={detail.items.find((row) => row.storyItemId === editingItemId)!.mediaUrl!}
                      alt=""
                      className="max-h-48 w-full rounded-xl object-contain"
                    />
                  )}
                </div>
              ) : null}
              {/* Explicit replacement: the old slide stays until the new file uploads AND the owning
                  update command completes; a caption/CTA-only edit never uploads anything. */}
              <input
                ref={slideFileInputRef}
                type="file"
                accept="image/*,video/*"
                className="sr-only"
                onChange={(e) => setSlideFile(e.target.files?.[0] ?? null)}
                data-testid="story-replace-file"
              />
              <button
                type="button"
                className={storyButtonClass("secondary")}
                onClick={() => slideFileInputRef.current?.click()}
                data-testid="story-replace-file-pick"
              >
                {slideFile ? `${copy.pickedFilePrefix} ${slideFile.name}` : copy.replaceMediaPick}
              </button>
              <p className="text-xs text-muted">{copy.replaceMediaHint}</p>
            </div>
          )}
          <label className="block text-sm">
            <span className="mb-1 block text-muted">{copy.captionLabel}</span>
            <input
              className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
              value={slideCaption}
              onChange={(e) => setSlideCaption(e.target.value)}
              data-testid="story-slide-caption"
            />
          </label>
          {(!editingItemId && isVideoFile(slideFile)) ||
          (editingItemId &&
            (slideFile
              ? isVideoFile(slideFile)
              : detail?.items.find((row) => row.storyItemId === editingItemId)?.mediaType === "video")) ? null : (
            <label className="block text-sm">
              <span className="mb-1 block text-muted">{copy.durationLabel}</span>
              <input
                type="number"
                min={1}
                className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                value={slideDurationSec}
                onChange={(e) => setSlideDurationSec(e.target.value)}
                data-testid="story-slide-duration"
              />
            </label>
          )}
          <label className="block text-sm">
            <span className="mb-1 block text-muted">{copy.ctaLabel}</span>
            <select
              className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
              value={slideCtaType}
              onChange={(e) => {
                setSlideCtaType(e.target.value);
                setSlideCtaTarget("");
                setSelectedProductLabel("");
                setSlideCtaError(null);
              }}
              data-testid="story-slide-cta-type"
            >
              <option value="none">{copy.destinationNone}</option>
              <option value="product">{copy.destinationProduct}</option>
              <option value="external">{copy.destinationExternal}</option>
              <option value="internal">{copy.destinationInternal}</option>
            </select>
          </label>
          {slideCtaType === "product" ? (
            <div className="space-y-2" data-testid="story-product-picker">
              <input
                className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                placeholder={copy.productSearchPlaceholder}
                aria-label={copy.productSearchPlaceholder}
                value={productSearch}
                onChange={(e) => setProductSearch(e.target.value)}
                data-testid="story-product-search"
              />
              {productLoading ? <p className="text-xs text-muted">{copy.loadingProducts}</p> : null}
              {productError ? (
                <div className="flex flex-wrap items-center gap-2" data-testid="story-product-error">
                  <p className="text-xs text-danger">{copy.productLoadFailed}</p>
                  <button
                    type="button"
                    className="min-h-9 rounded-lg border border-border px-2 text-xs"
                    onClick={() => setProductReloadToken((value) => value + 1)}
                  >
                    {copy.productRetry}
                  </button>
                </div>
              ) : null}
              <ul className="max-h-40 overflow-auto rounded-xl border border-border">
                {productOptions.map((row) => (
                  <li key={`${row.productId}:${row.offerId}`}>
                    <button
                      type="button"
                      className="flex min-h-11 w-full items-center px-3 py-2 text-right text-sm hover:bg-secondary"
                      data-testid="story-product-option"
                      data-product-id={row.productId}
                      onClick={() => {
                        if (!row.productId) {
                          storyToast.error(copy.productIdInvalid);
                          return;
                        }
                        setSlideCtaTarget(row.productId);
                        setSelectedProductLabel(row.productTitle);
                        setProductLabelById((prev) => ({ ...prev, [row.productId]: row.productTitle }));
                      }}
                    >
                      {row.productTitle}
                    </button>
                  </li>
                ))}
              </ul>
              {!productLoading && !productError && productOptions.length === 0 ? (
                <p className="text-xs text-muted" data-testid="story-product-empty">
                  {copy.noProductsFound}
                </p>
              ) : null}
              {productOptions.length < productTotalCount ? (
                <button
                  type="button"
                  className="min-h-11 w-full rounded-xl border border-border text-sm"
                  data-testid="story-product-load-more"
                  onClick={() => {
                    const shop =
                      capabilities.mode === "seller"
                        ? sellerShopId
                        : detail?.sellerPartyId ?? (adminShopMode === "shop" ? adminShopId : null);
                    const next = productPage + 1;
                    const epoch = productEpochRef.current;
                    setProductLoading(true);
                    setProductError(null);
                    void loadStoryProductOptions({
                      mode: capabilities.mode,
                      shopId: shop,
                      actorUserId: actorId,
                      search: productSearch,
                      page: next,
                      pageSize: 20,
                    })
                      .then((page) => {
                        if (epoch !== productEpochRef.current) return;
                        if (page.error) {
                          setProductError(copy.productLoadFailed);
                          return;
                        }
                        const seen = new Set(productOptions.map((r) => r.productId));
                        const appended = page.items.filter((r) => !seen.has(r.productId));
                        setProductOptions((prev) => [...prev, ...appended]);
                        setProductTotalCount(page.totalCount);
                        setProductPage(next);
                      })
                      .catch(() => {
                        if (epoch !== productEpochRef.current) return;
                        setProductError(copy.productLoadFailed);
                      })
                      .finally(() => {
                        if (epoch !== productEpochRef.current) return;
                        setProductLoading(false);
                      });
                  }}
                >
                  {copy.loadMoreProducts} ({productOptions.length} از {productTotalCount})
                </button>
              ) : null}
              {slideCtaTarget ? (
                <p className="text-xs text-muted" data-testid="story-product-selected">
                  {copy.productSelectedPrefix} {selectedProductLabel || copy.productSelectedFallback}
                </p>
              ) : null}
            </div>
          ) : null}
          {slideCtaType === "external" ? (
            <label className="block text-sm">
              <span className="mb-1 block text-muted">{copy.externalLinkLabel}</span>
              <input
                className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                dir="ltr"
                placeholder="https://..."
                value={slideCtaTarget}
                aria-label={copy.externalLinkLabel}
                aria-invalid={Boolean(slideCtaError)}
                onChange={(e) => {
                  setSlideCtaTarget(e.target.value);
                  setSlideCtaError(null);
                }}
                data-testid="story-slide-cta-external"
              />
            </label>
          ) : null}
          {slideCtaType === "internal" ? (
            <label className="block text-sm">
              <span className="mb-1 block text-muted">{copy.internalLinkLabel}</span>
              <input
                className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                dir="ltr"
                placeholder="/products"
                value={slideCtaTarget}
                aria-label={copy.internalLinkLabel}
                aria-invalid={Boolean(slideCtaError)}
                onChange={(e) => {
                  setSlideCtaTarget(e.target.value);
                  setSlideCtaError(null);
                }}
                data-testid="story-slide-cta-internal"
              />
            </label>
          ) : null}
          {slideCtaError ? (
            <p className="text-sm text-danger" role="alert" data-testid="story-slide-cta-error">
              {slideCtaError}
            </p>
          ) : null}
          <div className="flex flex-wrap justify-end gap-2 pt-2">
            <button
              type="button"
              className={storyButtonClass("secondary")}
              onClick={() => {
                setSlideDialogOpen(false);
                setEditingItemId(null);
              }}
            >
              {copy.cancel}
            </button>
            <button
              type="button"
              className={storyButtonClass("primary")}
              disabled={(!editingItemId && !slideFile) || inFlightRef.current}
              data-testid="story-slide-save"
              onClick={() => {
                if (!detail || !actorId) return;
                void (async () => {
                  if (inFlightRef.current) return;
                  const existingForCta = editingItemId
                    ? detail.items.find((row) => row.storyItemId === editingItemId)
                    : null;
                  const ctaUnchanged = Boolean(
                    existingForCta &&
                    existingForCta.ctaType === slideCtaType &&
                    (existingForCta.ctaTarget ?? "") === slideCtaTarget,
                  );
                  const ctaError = ctaUnchanged ? null : validateCtaInput(slideCtaType, slideCtaTarget, copy);
                  if (ctaError) {
                    setSlideCtaError(ctaError);
                    storyToast.error(ctaError);
                    return;
                  }
                  if (editingItemId) {
                    const existing = detail.items.find((row) => row.storyItemId === editingItemId);
                    if (!existing) {
                      storyToast.error(copy.slideMissing);
                      return;
                    }
                    // Explicit media replacement: ONE staged journal freezes the file digest, the
                    // exact upload and the owning update bytes BEFORE dispatch. The old slide stays
                    // authoritative until both steps succeed; a lost upload resumes that same file
                    // and a lost update retries only the frozen update (no second asset, no orphan).
                    if (slideFile) {
                      const uploadKey = mintOccurrence();
                      const itemKey = mintOccurrence();
                      const uploadPath =
                        capabilities.mode === "seller"
                          ? "/v1/seller/media"
                          : detail.sellerPartyId
                            ? "/v1/admin/media/upload-for-shop"
                            : "/v1/admin/media/upload-story-platform";
                      const digestHex = await digestFile(slideFile);
                      if (inFlightRef.current) return;
                      const plannedReplacement = {
                        caption: slideCaption.trim() || null,
                        durationSec: Math.max(1, Number(slideDurationSec) || 5),
                        ctaType: slideCtaType,
                        ctaTarget: slideCtaType === "none" ? null : slideCtaTarget || null,
                        expectedVersion: detail.versionToken,
                        itemOccurrenceKey: itemKey,
                      };
                      const replaceEnvelope: StoryIntentEnvelope = {
                        version: 2,
                        action: "replace_media",
                        occurrenceKey: uploadKey,
                        method: "POST",
                        path: uploadPath,
                        routeTargetId: detail.storyId,
                        targetItemId: editingItemId,
                        expectedVersion: detail.versionToken,
                        bodyText: JSON.stringify(plannedReplacement),
                        uploadMeta: {
                          fileName: slideFile.name,
                          fileType: slideFile.type,
                          fileSize: slideFile.size,
                          digestHex,
                        },
                        addBodyText: null,
                        uploadedAssetId: null,
                        children: [
                          { step: "upload", occurrenceKey: uploadKey, done: false },
                          { step: "update_item", occurrenceKey: itemKey, done: false },
                        ],
                        role: capabilities.mode,
                        shopId: detail.sellerPartyId ?? sellerShopId,
                        actorUserId: actorId,
                        recordedAt: new Date().toISOString(),
                      };
                      const replacement = await runDurableUploadAdd({
                        gate,
                        envelope: replaceEnvelope,
                        scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                        uploadFile: slideFile,
                      });
                      if (replacement.state === "ok") {
                        storyToast.success(copy.slideUpdated);
                        if (slideCtaType === "product" && slideCtaTarget && selectedProductLabel) {
                          setProductLabelById((prev) => ({ ...prev, [slideCtaTarget]: selectedProductLabel }));
                        }
                        setSlideFile(null);
                        setSlideDialogOpen(false);
                        setEditingItemId(null);
                        setReloadToken((v) => v + 1);
                        refreshGrid();
                      } else if (replacement.state === "failed") {
                        storyToast.error(replacement.message);
                      } else {
                        storyToast.error(copy.sendFailed);
                      }
                      return;
                    }
                    const key = mintOccurrence();
                    const isVideo = existing.mediaType === "video";
                    const body = {
                      mediaType: existing.mediaType,
                      mediaAssetId: existing.mediaAssetId,
                      mediaUrl: existing.mediaUrl,
                      caption: slideCaption.trim() || null,
                      durationMs: isVideo ? null : Math.max(1, Number(slideDurationSec) || 5) * 1000,
                      ctaType: slideCtaType,
                      ctaTarget: slideCtaType === "none" ? null : slideCtaTarget || null,
                      expectedVersion: detail.versionToken,
                      occurrenceKey: key,
                    };
                    const envelope: StoryIntentEnvelope = {
                      version: 2,
                      action: "update_item",
                      occurrenceKey: key,
                      method: "PUT",
                      path:
                        capabilities.mode === "seller"
                          ? `/v1/seller/stories/${detail.storyId}/items/${editingItemId}`
                          : `/v1/admin/stories/${detail.storyId}/items/${editingItemId}`,
                      routeTargetId: detail.storyId,
                      targetItemId: editingItemId,
                      expectedVersion: detail.versionToken,
                      bodyText: JSON.stringify(body),
                      uploadMeta: null,
                      addBodyText: null,
                      uploadedAssetId: null,
                      children: [],
                      role: capabilities.mode,
                      shopId: detail.sellerPartyId ?? sellerShopId,
                      actorUserId: actorId,
                      recordedAt: new Date().toISOString(),
                    };
                    const outcome = await runDurableStoryMutation({
                      gate,
                      envelope,
                      scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                    });
                    if (outcome.state === "ok") {
                      storyToast.success(copy.slideUpdated);
                      if (slideCtaType === "product" && slideCtaTarget && selectedProductLabel) {
                        setProductLabelById((prev) => ({ ...prev, [slideCtaTarget]: selectedProductLabel }));
                      }
                      setSlideDialogOpen(false);
                      setEditingItemId(null);
                      setReloadToken((v) => v + 1);
                      refreshGrid();
                    } else if (outcome.state === "failed") storyToast.error(outcome.message);
                    else storyToast.error(copy.sendFailed);
                    return;
                  }
                  if (!slideFile) return;
                  const uploadKey = mintOccurrence();
                  const itemKey = mintOccurrence();
                  // Freeze the cryptographic digest before the journal exists (never a literal marker).
                  const uploadDigest = await digestFile(slideFile);
                  if (inFlightRef.current) return;
                  const plannedAdd = {
                    caption: slideCaption.trim() || null,
                    durationSec: isVideoFile(slideFile) ? null : Math.max(1, Number(slideDurationSec) || 5),
                    ctaType: slideCtaType,
                    ctaTarget: slideCtaType === "none" ? null : slideCtaTarget || null,
                    expectedVersion: detail.versionToken,
                    itemOccurrenceKey: itemKey,
                  };
                  const uploadEnvelope: StoryIntentEnvelope = {
                    version: 2,
                    action: "upload_add",
                    occurrenceKey: uploadKey,
                    method: "POST",
                    path:
                      capabilities.mode === "seller"
                        ? "/v1/seller/media"
                        : detail.sellerPartyId
                          ? "/v1/admin/media/upload-for-shop"
                          : "/v1/admin/media/upload-story-platform",
                    routeTargetId: detail.storyId,
                    expectedVersion: detail.versionToken,
                    bodyText: JSON.stringify(plannedAdd),
                    uploadMeta: {
                      fileName: slideFile.name,
                      fileType: slideFile.type,
                      fileSize: slideFile.size,
                      digestHex: uploadDigest,
                    },
                    addBodyText: null,
                    uploadedAssetId: null,
                    children: [
                      { step: "upload", occurrenceKey: uploadKey, done: false },
                      { step: "add", occurrenceKey: itemKey, done: false },
                    ],
                    role: capabilities.mode,
                    shopId: detail.sellerPartyId ?? sellerShopId,
                    actorUserId: actorId,
                    recordedAt: new Date().toISOString(),
                  };
                  const outcome = await runDurableUploadAdd({
                    gate,
                    envelope: uploadEnvelope,
                    scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                    uploadFile: slideFile,
                  });
                  if (outcome.state === "ok") {
                    storyToast.success(copy.slideAdded);
                    if (slideCtaType === "product" && slideCtaTarget && selectedProductLabel) {
                      setProductLabelById((prev) => ({ ...prev, [slideCtaTarget]: selectedProductLabel }));
                    }
                    setSlideDialogOpen(false);
                    setSlideFile(null);
                    setSlideCaption("");
                    setSlideCtaType("none");
                    setSlideCtaTarget("");
                    refreshGrid();
                    setReloadToken((v) => v + 1);
                  } else if (outcome.state === "failed") storyToast.error(outcome.message);
                  else storyToast.error(copy.sendFailed);
                })();
              }}
            >
              {editingItemId ? copy.saveSlideChanges : copy.addItem}
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog
        open={pendingRecoveryOpen}
        onClose={() => setPendingRecoveryOpen(false)}
        title={copy.pendingHeading}
        showCloseButton={false}
      >
        <div className="space-y-3 p-1" data-testid="story-pending-recovery-dialog">
          <p className="text-sm text-muted">{copy.pendingExpandHint}</p>
          <ul className="space-y-2 rounded-xl border border-border px-4 py-3 text-sm text-muted">
            {pendingActs.map((act) => (
              <li key={`${act.action}:${act.occurrenceKey}`} className="flex flex-wrap items-center justify-between gap-2">
                <span>{pendingStoryLabel(act)}</span>
                <div className="flex flex-wrap items-center gap-2">
                  {(act.action === "upload" ||
                    act.action === "upload_add" ||
                    act.action === "replace_media") &&
                  !act.children[0]?.done ? (
                    <input
                      type="file"
                      accept="image/*,video/*"
                      className="min-h-11 text-sm"
                      data-testid="story-recovery-file"
                      onChange={(e) => setRecoveryFile(e.target.files?.[0] ?? null)}
                    />
                  ) : null}
                  <button
                    type="button"
                    className={storyButtonClass("secondary")}
                    data-testid="story-recovery-retry"
                    onClick={() => {
                      const scope = { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId };
                      const shopMismatch =
                        act.role === "seller" && (act.shopId ?? null) !== (scope.shopId ?? null);
                      if (act.role !== scope.role || act.actorUserId !== scope.actorUserId || shopMismatch) {
                        storyToast.error(copy.pendingContinueFailed);
                        return;
                      }
                      void replayPendingStoryIntent({
                        gate,
                        envelope: act,
                        scope: {
                          role: act.role,
                          shopId: act.role === "admin" ? act.shopId ?? null : scope.shopId,
                          actorUserId: scope.actorUserId,
                        },
                        uploadFile: recoveryFile,
                      }).then((outcome) => {
                        if (outcome.state === "ok") {
                          storyToast.success(copy.pendingContinued);
                          setRecoveryFile(null);
                          setPendingRecoveryOpen(false);
                          if (act.action === "create") {
                            const id = extractCreatedStoryId(outcome.data);
                            if (id) {
                              adoptBoundCreatedDraft(id, {
                                openSlideAfter: Boolean(createMode && !selectedId),
                              });
                            }
                          }
                          if (act.action === "delete") {
                            setDeleteOpen(false);
                            setDeleteTarget(null);
                            const closedId = act.routeTargetId;
                            if (closedId && (selectedId === closedId || detail?.storyId === closedId)) {
                              setSelectedId(null);
                              setDetail(null);
                              ownedDraftIdRef.current = null;
                              router.push(basePath);
                            }
                          }
                          refreshGrid();
                          setReloadToken((v) => v + 1);
                        } else if (outcome.state === "failed") storyToast.error(outcome.message);
                        else storyToast.error(copy.pendingContinueFailed);
                      });
                    }}
                  >
                    {copy.pendingRetry}
                  </button>
                </div>
              </li>
            ))}
          </ul>
          <div className="flex justify-end">
            <button
              type="button"
              className={storyButtonClass("secondary")}
              onClick={() => setPendingRecoveryOpen(false)}
            >
              {copy.cancel}
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog
        open={deleteOpen}
        onClose={() => {
          setDeleteOpen(false);
          setDeleteTarget(null);
        }}
        title={copy.deleteStory}
      >
        <div className="space-y-3 p-1" data-testid="story-delete-dialog">
          <p className="text-sm text-muted">
            {copy.deleteStoryConfirm}
            {deleteTarget?.title ? ` «${deleteTarget.title}»` : ""}
          </p>
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              className={storyButtonClass("secondary")}
              data-testid="story-delete-cancel"
              onClick={() => {
                setDeleteOpen(false);
                setDeleteTarget(null);
              }}
            >
              {copy.cancel}
            </button>
            <button
              type="button"
              className={storyButtonClass("danger")}
              data-testid="story-delete-confirm"
              onClick={() => {
                const target = deleteTarget;
                if (!target || !actorId) return;
                const key = mintOccurrence();
                const body = { expectedVersion: target.versionToken, occurrenceKey: key };
                const envelope: StoryIntentEnvelope = {
                  version: 2,
                  action: "delete",
                  occurrenceKey: key,
                  method: "DELETE",
                  path:
                    capabilities.mode === "seller"
                      ? `/v1/seller/stories/${target.storyId}`
                      : `/v1/admin/stories/${target.storyId}`,
                  routeTargetId: target.storyId,
                  expectedVersion: target.versionToken,
                  bodyText: JSON.stringify(body),
                  uploadMeta: null,
                  addBodyText: null,
                  uploadedAssetId: null,
                  children: [],
                  role: capabilities.mode,
                  shopId: target.sellerPartyId ?? sellerShopId,
                  actorUserId: actorId,
                  recordedAt: new Date().toISOString(),
                };
                void runDurableStoryMutation({
                  gate,
                  envelope,
                  scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                }).then((outcome) => {
                  if (outcome.state === "ok") {
                    storyToast.success(copy.deletedToast);
                    setDeleteOpen(false);
                    setDeleteTarget(null);
                    const closedId = target.storyId;
                    if (selectedId === closedId || detail?.storyId === closedId) {
                      setSelectedId(null);
                      setDetail(null);
                      ownedDraftIdRef.current = null;
                      router.push(basePath);
                    }
                    refreshGrid();
                  } else if (outcome.state === "failed") {
                    storyToast.error(outcome.message);
                    setReloadToken((v) => v + 1);
                  } else {
                    storyToast.error(copy.sendFailed);
                    setReloadToken((v) => v + 1);
                  }
                });
              }}
            >
              {copy.deleteStory}
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog
        open={slideDiscardOpen}
        onClose={() => setSlideDiscardOpen(false)}
        title={copy.unsavedLeaveTitle}
      >
        <div className="space-y-3 p-1" data-testid="story-slide-unsaved-dialog">
          <p className="text-sm text-muted">{copy.unsavedLeaveBody}</p>
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              className={storyButtonClass("secondary")}
              data-testid="story-slide-unsaved-stay"
              onClick={() => setSlideDiscardOpen(false)}
            >
              {copy.unsavedStay}
            </button>
            <button
              type="button"
              className={storyButtonClass("danger")}
              data-testid="story-slide-unsaved-discard"
              onClick={discardSlideEditor}
            >
              {copy.unsavedDiscard}
            </button>
          </div>
        </div>
      </Dialog>
      <Dialog
        open={leaveOpen}
        onClose={() => {
          setLeaveOpen(false);
          setPendingLeaveHref(null);
        }}
        title={copy.unsavedLeaveTitle}
      >
        <div className="space-y-3 p-1" data-testid="story-unsaved-leave-dialog">
          <p className="text-sm text-muted">{copy.unsavedLeaveBody}</p>
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              className={storyButtonClass("secondary")}
              data-testid="story-unsaved-stay"
              onClick={() => {
                setLeaveOpen(false);
                setPendingLeaveHref(null);
              }}
            >
              {copy.unsavedStay}
            </button>
            <button
              type="button"
              className={storyButtonClass("danger")}
              data-testid="story-unsaved-discard"
              onClick={() => {
                const href = pendingLeaveHref;
                setLeaveOpen(false);
                setPendingLeaveHref(null);
                if (href) finishLeave(href);
              }}
            >
              {copy.unsavedDiscard}
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog
        open={Boolean(adminOpPlan)}
        onClose={() => {
          if (adminOpBusy) {
            adminOpCancelledRef.current = true;
            return;
          }
          setAdminOpPlan(null);
          setAdminOpReason("");
        }}
        title={
          adminOpPlan?.kind === "approve"
            ? copy.approveConfirm
            : adminOpPlan?.kind === "reject"
              ? copy.reject
              : copy.deleteStory
        }
      >
        {adminOpPlan ? (
          <div className="space-y-3 p-1" data-testid="story-admin-op-dialog">
            <div className="grid grid-cols-2 gap-2">
              <div className="rounded-xl border border-border bg-background/40 px-3 py-2">
                <span className="block text-xs text-muted">{copy.bulkSelectedCount}</span>
                <span className="text-lg font-bold text-foreground" data-testid="story-admin-op-selected-count">{adminOpPlan.selected.length.toLocaleString(numberLocale)}</span>
              </div>
              <div className="rounded-xl border border-border bg-background/40 px-3 py-2">
                <span className="block text-xs text-muted">{copy.bulkProcessableCount}</span>
                <span className="text-lg font-bold text-foreground" data-testid="story-admin-op-processable-count">{adminOpPlan.eligible.length.toLocaleString(numberLocale)}</span>
              </div>
            </div>
            {adminOpPlan.kind === "approve" ? (
              <p className="text-sm text-muted">{copy.approveConfirmBody}</p>
            ) : null}
            {adminOpPlan.kind === "delete" ? (
              <p className="text-sm text-danger">{copy.deleteBatchConfirmBody}</p>
            ) : null}
            {adminOpPlan.kind === "reject" ? (
              <p className="text-sm text-muted">{copy.rejectConfirmBody}</p>
            ) : null}
            {adminOpPlan.skipped.length > 0 ? (
              <div data-testid="story-admin-op-skipped">
                <p className="mb-1 text-sm font-bold">
                  {copy.bulkSkippedHeading}: {adminOpPlan.skipped.length.toLocaleString(numberLocale)}
                </p>
                <ul className="max-h-40 space-y-1 overflow-auto text-sm text-muted" data-testid="story-admin-op-skipped-groups">
                  {groupSkippedReasons(adminOpPlan.skipped).map((group) => (
                    <li key={group.reason} data-testid="story-admin-op-skipped-group">
                      {group.reason}
                      {group.count > 1 ? ` (${group.count.toLocaleString(numberLocale)})` : ""}
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
            {adminOpPlan.kind === "reject" ? (
              <label className="block text-sm">
                <span className="mb-1 block text-muted">{copy.rejectionReason}</span>
                <input
                  className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
                  value={adminOpReason}
                  onChange={(e) => setAdminOpReason(e.target.value)}
                  data-testid="story-reject-reason-input"
                />
              </label>
            ) : null}
            <div className="flex flex-wrap justify-end gap-2">
              <button
                type="button"
                className={storyButtonClass("secondary")}
                data-testid="story-admin-op-cancel"
                disabled={adminOpBusy}
                onClick={() => {
                  setAdminOpPlan(null);
                  setAdminOpReason("");
                }}
              >
                {copy.cancel}
              </button>
              <button
                type="button"
                className={adminOpPlan.kind === "delete" || adminOpPlan.kind === "reject" ? storyButtonClass("danger") : storyButtonClass("primary")}
                data-testid="story-admin-op-confirm"
                disabled={adminOpBusy || adminOpPlan.eligible.length === 0}
                onClick={() => void confirmAdminOp()}
              >
                {adminOpPlan.kind === "approve"
                  ? copy.bulkConfirmApprove
                  : adminOpPlan.kind === "reject"
                    ? copy.bulkConfirmReject
                    : copy.bulkConfirmDelete}
              </button>
            </div>
          </div>
        ) : null}
      </Dialog>

      <Dialog
        open={scheduleOpen}
        onClose={() => setScheduleOpen(false)}
        title={scheduleMode === "approve_schedule" ? "تأیید و زمان‌بندی" : "زمان‌بندی نمایش"}
      >
        <div className="space-y-4 p-1">
          <p className="rounded-xl border border-info/20 bg-info/10 px-3 py-2 text-xs leading-5 text-info">زمان به وقت تهران. اگر پایان خالی بماند تا توقف دستی می‌ماند.</p>
          {scheduleFieldError ? (
            <p className="text-sm text-danger" role="alert" data-testid="story-schedule-error">{scheduleFieldError}</p>
          ) : null}
          <div className="grid gap-3 md:grid-cols-2">
          <label className="block rounded-xl border border-border bg-background/40 p-3 text-sm" data-testid="story-schedule-start-wrap">
            <span className="mb-2 block font-medium text-muted">روز و ساعت شروع</span>
            <JalaliDatePicker
              ariaLabel="روز شروع"
              value={scheduleDraft.startDayIso || undefined}
              onChange={(iso) => setScheduleDraft((c) => ({ ...c, startDayIso: iso ?? "" }))}
            />
            <input type="time" className="mt-2 min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm" value={scheduleDraft.startTime} onChange={(e) => setScheduleDraft((c) => ({ ...c, startTime: e.target.value }))} data-testid="story-schedule-start" />
          </label>
          <label className="block rounded-xl border border-border bg-background/40 p-3 text-sm" data-testid="story-schedule-end-wrap">
            <span className="mb-2 block font-medium text-muted">روز و ساعت پایان (اختیاری)</span>
            <JalaliDatePicker
              ariaLabel="روز پایان"
              value={scheduleDraft.endDayIso || undefined}
              onChange={(iso) => setScheduleDraft((c) => ({ ...c, endDayIso: iso ?? "" }))}
            />
            <input type="time" className="mt-2 min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm" value={scheduleDraft.endTime} onChange={(e) => setScheduleDraft((c) => ({ ...c, endTime: e.target.value }))} data-testid="story-schedule-end" />
          </label>
          </div>
          <div className="flex justify-end gap-2">
            <button type="button" className={storyButtonClass("secondary")} onClick={() => setScheduleOpen(false)}>
              انصراف
            </button>
            <button
              type="button"
              className={storyButtonClass("primary")}
              onClick={() => {
                if (!detail || !actorId) return;
                const key = mintOccurrence();
                const parsed = parseStoryScheduleDraft(scheduleDraft);
                if (!parsed.ok) {
                  setScheduleFieldError(parsed.message);
                  return;
                }
                setScheduleFieldError(null);
                const startAt = parsed.startAt;
                const endAt = parsed.endAt;
                const body =
                  scheduleMode === "approve_schedule"
                    ? { expectedVersion: detail.versionToken, startAt, endAt, occurrenceKey: key }
                    : { startAt, endAt, expectedVersion: detail.versionToken, occurrenceKey: key };
                const envelope: StoryIntentEnvelope = {
                  version: 2,
                  action: scheduleMode === "approve_schedule" ? "approve_schedule" : "schedule",
                  occurrenceKey: key,
                  method: "POST",
                  path:
                    scheduleMode === "approve_schedule"
                      ? `/v1/admin/stories/${detail.storyId}/approve-and-schedule`
                      : `/v1/admin/stories/${detail.storyId}/schedule`,
                  routeTargetId: detail.storyId,
                  expectedVersion: detail.versionToken,
                  bodyText: JSON.stringify(body),
                  uploadMeta: null,
                  addBodyText: null,
                  uploadedAssetId: null,
                  children: [],
                  role: "admin",
                  shopId: detail.sellerPartyId,
                  actorUserId: actorId,
                  recordedAt: new Date().toISOString(),
                };
                void runDurableStoryMutation({
                  gate,
                  envelope,
                  scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                }).then((outcome) => {
                  if (outcome.state === "blocked") {
                    storyToast.error(
                      outcome.reason === "storage"
                        ? copy.pendingStorageBlocked
                        : copy.sendFailed,
                    );
                    return;
                  }
                  if (outcome.state === "failed") {
                    storyToast.error(outcome.message);
                    return;
                  }
                  storyToast.success(scheduleMode === "approve_schedule" ? copy.approvedScheduled : copy.scheduleSaved);
                  setScheduleOpen(false);
                  refreshGrid();
                  setReloadToken((v) => v + 1);
                });
              }}
            >
              {scheduleMode === "approve_schedule" ? "تأیید و زمان‌بندی" : "ذخیره زمان‌بندی"}
            </button>
          </div>
        </div>
      </Dialog>

      {previewConfirm === "publish" ? (
      <Dialog open onClose={() => setPreviewConfirm(null)} title={copy.approveAndPublish}>
        <div className="space-y-3 p-1" data-testid="story-publish-preview">
          {detail ? (
            <>
              <p className="text-sm text-muted">پیش‌نمایش نسخهٔ بررسی‌شده قبل از انتشار:</p>
              <p className="font-bold">{detail.title}</p>
              <ul className="space-y-2 text-sm">
                {detail.items.map((item) => (
                  <li key={item.storyItemId} className="flex items-center gap-3 rounded-xl border border-border px-3 py-2">
                    {item.mediaType === "video" && item.mediaUrl ? (
                      // eslint-disable-next-line jsx-a11y/media-has-caption
                      <video src={item.mediaUrl} className="h-16 w-16 shrink-0 rounded-lg border border-border object-cover" muted playsInline />
                    ) : item.mediaUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={item.mediaUrl} alt="" className="h-16 w-16 shrink-0 rounded-lg border border-border object-cover" />
                    ) : null}
                    <span>
                      {item.mediaType === "video" ? copy.videoLabel : copy.photoLabel}
                      {item.caption ? ` · ${item.caption}` : ""}
                      {item.ctaType !== "none" ? ` · ${copy.destinationPrefix} ${destinationLabelForItem(item)}` : ""}
                    </span>
                  </li>
                ))}
              </ul>
              <div className="flex justify-end gap-2">
                <button type="button" className={storyButtonClass("secondary")} onClick={() => setPreviewConfirm(null)}>
                  انصراف
                </button>
                <button
                  type="button"
                  className={storyButtonClass("primary")}
                  onClick={() => {
                    if (!detail || !actorId) return;
                    const key = mintOccurrence();
                    const envelope: StoryIntentEnvelope = {
                      version: 2,
                      action: "approve_publish",
                      occurrenceKey: key,
                      method: "POST",
                      path: `/v1/admin/stories/${detail.storyId}/approve-and-publish`,
                      routeTargetId: detail.storyId,
                      expectedVersion: detail.versionToken,
                      bodyText: JSON.stringify({ expectedVersion: detail.versionToken, occurrenceKey: key }),
                      uploadMeta: null,
                      addBodyText: null,
                      uploadedAssetId: null,
                      children: [],
                      role: "admin",
                      shopId: detail.sellerPartyId,
                      actorUserId: actorId,
                      recordedAt: new Date().toISOString(),
                    };
                    void runDurableStoryMutation({
                      gate,
                      envelope,
                      scope: { role: capabilities.mode, shopId: sellerShopId, actorUserId: actorId },
                    }).then((outcome) => {
                      if (outcome.state === "ok") {
                        storyToast.success(copy.approvedPublished);
                        setPreviewConfirm(null);
                        refreshGrid();
                        setReloadToken((v) => v + 1);
                      } else if (outcome.state === "failed") storyToast.error(outcome.message);
                      else storyToast.error(copy.sendFailed);
                    });
                  }}
                >
                  تأیید انتشار
                </button>
              </div>
            </>
          ) : null}
        </div>
      </Dialog>
      ) : null}
    </main>
  );
}

function StoryLiveGrid({
  capabilities,
  reviewFilter,
  reloadToken,
  copy,
  numberLocale,
  onDenied,
  onError,
  onOk,
  onOpenDetail,
  onSubmit,
  onRequestDelete,
  onRequestAdminOp,
  errorMessage,
  onRetry,
  showError,
}: {
  capabilities: StoryCapabilities;
  reviewFilter: string;
  reloadToken: number;
  copy: StoryCopy;
  numberLocale: string;
  onDenied: () => void;
  onError: (message?: string) => void;
  onOk: () => void;
  onOpenDetail: (id: string) => void;
  onSubmit: (row: AdminStorySnapshot) => void;
  onRequestDelete: (row: AdminStorySnapshot) => void;
  onRequestAdminOp: (kind: StoryAdminOpKind, rows: AdminStorySnapshot[]) => void;
  errorMessage?: string;
  onRetry: () => void;
  showError: boolean;
}) {
  const actorId = resolveActorId(capabilities.mode);
  const sellerShopId = capabilities.mode === "seller" ? readSellerPartyId() : null;
  const isAdmin = capabilities.mode === "admin";
  const onDeniedRef = useRef(onDenied);
  const onErrorRef = useRef(onError);
  const onOkRef = useRef(onOk);
  const onOpenDetailRef = useRef(onOpenDetail);
  const onSubmitRef = useRef(onSubmit);
  const onRequestDeleteRef = useRef(onRequestDelete);
  const onRequestAdminOpRef = useRef(onRequestAdminOp);
  onDeniedRef.current = onDenied;
  onErrorRef.current = onError;
  onOkRef.current = onOk;
  onOpenDetailRef.current = onOpenDetail;
  onSubmitRef.current = onSubmit;
  onRequestDeleteRef.current = onRequestDelete;
  onRequestAdminOpRef.current = onRequestAdminOp;

  const columns = useMemo((): GridColumnDef<AdminStorySnapshot>[] => {
    return [
      {
        id: "title",
        header: copy.columnTitle,
        accessor: (row) => row.title,
        cell: (row) => (
          <button
            type="button"
            className="flex min-h-11 items-center gap-2 text-right font-bold line-clamp-2 hover:text-primary"
            onClick={() => onOpenDetailRef.current(row.storyId)}
            data-testid="story-row-title"
          >
            {row.coverMediaUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={row.coverMediaUrl} alt="" className="h-10 w-10 shrink-0 rounded-lg border border-border object-cover" />
            ) : (
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border border-border bg-secondary text-xs text-muted">—</span>
            )}
            <span>{row.title}</span>
          </button>
        ),
        width: 240,
        minWidth: 160,
        // Match Orders: first data column is not sticky; actions use bridge end-pin.
        filterKind: "text",
        sortable: true,
      },
      {
        id: "shopDisplayName",
        header: copy.columnShop,
        accessor: (row) => shopLabel(row, copy),
        cell: (row) => (
          <span className="inline-flex max-w-full items-center gap-1.5 text-sm text-muted">
            <Store className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
            <span className="truncate">{shopLabel(row, copy)}</span>
          </span>
        ),
        width: 140,
        minWidth: 110,
        filterKind: isAdmin ? "text" : undefined,
      },
      {
        id: "displayState",
        header: copy.columnStatus,
        accessor: (row) => displayState(row, copy),
        cell: (row) => (
          <StoryStatusPill label={displayState(row, copy)} tone={displayStateTone(row)} testId="story-row-status" />
        ),
        width: 160,
        minWidth: 120,
      },
      {
        id: "items",
        header: copy.columnSlides,
        accessor: (row) => row.items.length,
        cell: (row) => row.items.length.toLocaleString(numberLocale),
        width: 90,
        minWidth: 70,
      },
      {
        id: "startAt",
        header: copy.columnStart,
        accessor: (row) => row.startAt ?? "",
        cell: (row) =>
          row.startAt
            ? formatStoryScheduleClock(row.startAt, numberLocale.startsWith("en") ? "en" : "fa") ||
              formatAdminDate(row.startAt)
            : "—",
        width: 140,
        minWidth: 120,
        filterKind: "date",
      },
      {
        id: "endAt",
        header: copy.columnEnd,
        accessor: (row) => row.endAt ?? "",
        cell: (row) =>
          row.endAt
            ? formatStoryScheduleClock(row.endAt, numberLocale.startsWith("en") ? "en" : "fa") ||
              formatAdminDate(row.endAt)
            : copy.noEnd,
        width: 140,
        minWidth: 120,
        filterKind: "date",
      },
      {
        id: "updatedAt",
        header: copy.columnUpdated,
        accessor: (row) => row.updatedAt,
        cell: (row) => formatAdminDate(row.updatedAt),
        width: 120,
        minWidth: 100,
        filterKind: "date",
        sortable: true,
      },
      {
        id: "actions",
        header: copy.columnActions,
        accessor: () => "",
        cell: (row) => {
          const actions: AppGridRowAction<AdminStorySnapshot>[] = [
            { id: "view", label: copy.rowReview, icon: Eye, onClick: (r) => onOpenDetailRef.current(r.storyId) },
          ];
          if ((capabilities.canSubmit || isAdmin) && hasAction(row, "submit")) {
            actions.push({
              id: "submit",
              label: copy.rowSubmit,
              icon: Send,
              onClick: (r) => onSubmitRef.current(r),
            });
          }
          if (isAdmin && hasAction(row, "approve")) {
            actions.push({
              id: "approve",
              label: copy.approve,
              icon: Check,
              onClick: (r) => onRequestAdminOpRef.current("approve", [r]),
            });
          }
          if (isAdmin && hasAction(row, "reject")) {
            actions.push({
              id: "reject",
              label: copy.reject,
              icon: XCircle,
              onClick: (r) => onRequestAdminOpRef.current("reject", [r]),
            });
          }
          if (hasAction(row, "delete")) {
            actions.push({
              id: "delete",
              label: copy.deleteStory,
              icon: Trash2,
              onClick: (r) => onRequestDeleteRef.current(r),
            });
          }
          return <AppGridRowActionsCell row={row} actions={actions} compact />;
        },
        width: 140,
        minWidth: 110,
        exportable: false,
        sortable: false,
      },
    ];
  }, [capabilities.canSubmit, copy, isAdmin, numberLocale]);

  const bulkActions = useMemo((): GridBulkAction<AdminStorySnapshot>[] => {
    if (!isAdmin) return [];
    const open = (kind: StoryAdminOpKind) => async (rows: AdminStorySnapshot[]) => {
      onRequestAdminOpRef.current(kind, rows);
      return { ok: true, message: "opened" };
    };
    return [
      {
        id: "story-bulk-approve",
        label: copy.approve,
        requiresConfirmation: true,
        isAvailable: (rows) => rows.length > 0,
        execute: open("approve"),
      },
      {
        id: "story-bulk-reject",
        label: copy.reject,
        requiresConfirmation: true,
        isAvailable: (rows) => rows.length > 0,
        execute: open("reject"),
      },
      {
        id: "story-bulk-delete",
        label: copy.deleteStory,
        requiresConfirmation: true,
        isAvailable: (rows) => rows.length > 0,
        execute: open("delete"),
      },
    ];
  }, [copy.approve, copy.deleteStory, copy.reject, isAdmin]);

  const savedViewStore = useMemo(
    () =>
      createScopedStorySavedViewStore({
        role: capabilities.mode,
        actorUserId: actorId || "anon",
        shopId: sellerShopId,
        gridKey: capabilities.mode === "seller" ? SELLER_STORY_GRID_VIEW_KEY : ADMIN_STORY_GRID_VIEW_KEY,
      }),
    [capabilities.mode, actorId, sellerShopId],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      if (capabilities.mode === "seller") {
        const result = await querySellerStories(query);
        if (result.state === "denied") {
          onDeniedRef.current();
          return { rows: [], total: 0 };
        }
        if (result.state !== "ok" || !result.data) {
          onErrorRef.current(result.message);
          return { rows: [], total: 0 };
        }
        onOkRef.current();
        return { rows: result.data.items, total: result.data.totalCount };
      }
      return adminGridQueryAdapter(
        (serverQuery) =>
          queryAdminStoriesGrid(serverQuery, reviewFilter ? { reviewStatus: reviewFilter } : undefined),
        () => onDeniedRef.current(),
        (message) => onErrorRef.current(message),
      )(query);
    },
    [capabilities.mode, reviewFilter],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: capabilities.mode === "seller" ? SELLER_STORY_GRID_VIEW_KEY : ADMIN_STORY_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm" data-testid="story-live-grid">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border px-5 py-4">
        <div>
          <h2 className="text-base font-bold text-foreground">{copy.listHeading}</h2>
          <p className="mt-1 text-xs text-muted">{copy.listHint}</p>
        </div>
      </div>
      <div className="p-2 md:p-4">
        {showError ? (
          <ErrorState title={copy.loadErrorTitle} detail={errorMessage} onRetry={onRetry} retryLabel={faWorkspaceMessages.retry} />
        ) : (
          <AppDataGrid<AdminStorySnapshot>
            {...gridProps}
            advancedFilterColumns={isAdmin ? gridProps.advancedFilterColumns : []}
            refreshToken={reloadToken}
            capabilities={{ rowSelection: isAdmin }}
            bulkActions={bulkActions}
            pageSelectionOnly
          />
        )}
      </div>
    </section>
  );
}
