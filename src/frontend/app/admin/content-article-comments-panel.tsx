"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "react-toastify";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import { ContentHelpAffordance } from "./content-help-affordance.tsx";
import {
  approveArticleComment,
  articleCommentStatusLabel,
  createArticleComment,
  hideArticleComment,
  loadArticleComments,
  markArticleCommentPending,
  rejectArticleComment,
  type ArticleCommentPage,
  type ArticleCommentRow,
  type ArticleCommentStatus,
} from "./content-article-comments-api.ts";

const PAGE_SIZE = 20;

const STATUS_FILTERS: Array<{ id: "" | ArticleCommentStatus; label: string }> = [
  { id: "", label: "همه" },
  { id: "Pending", label: "در انتظار" },
  { id: "Approved", label: "تأییدشده" },
  { id: "Rejected", label: "ردشده" },
  { id: "Hidden", label: "پنهان" },
];

type ContentArticleCommentsPanelProps = {
  articleId: string;
  canModerate: boolean;
  onPendingCountChange?: (count: number) => void;
};

function formatWhen(iso: string): string {
  const t = Date.parse(iso);
  if (!Number.isFinite(t)) return iso;
  try {
    return new Intl.DateTimeFormat("fa-IR", {
      dateStyle: "medium",
      timeStyle: "short",
    }).format(new Date(t));
  } catch {
    return iso;
  }
}

/** تب نظرات مقاله — فهرست فشردهٔ تعدیل با فیلتر/جستجو/صفحه‌بندی (بدون AgGrid خام). */
export function ContentArticleCommentsPanel({
  articleId,
  canModerate,
  onPendingCountChange,
}: ContentArticleCommentsPanelProps) {
  const [status, setStatus] = useState<"" | ArticleCommentStatus>("");
  const [search, setSearch] = useState("");
  const [searchApplied, setSearchApplied] = useState("");
  const [skip, setSkip] = useState(0);
  const [page, setPage] = useState<ArticleCommentPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<{
    comment: ArticleCommentRow;
    action: "reject" | "hide";
  } | null>(null);
  const [seedOpen, setSeedOpen] = useState(false);
  const [seedName, setSeedName] = useState("خواننده آزمایشی");
  const [seedBody, setSeedBody] = useState("این یک نظر آزمایشی برای بررسی تعدیل است.");

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    const result = await loadArticleComments(articleId, {
      status: status || undefined,
      search: searchApplied,
      skip,
      take: PAGE_SIZE,
    });
    setLoading(false);
    if (result.state !== "ok" || !result.data) {
      setPage(null);
      setError(mapAdminErrorMessage(result.message ?? "admin.error.generic", "fa"));
      onPendingCountChange?.(0);
      return;
    }
    setPage(result.data);
    onPendingCountChange?.(result.data.pendingCount);
  }, [articleId, onPendingCountChange, searchApplied, skip, status]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const runModerate = async (
    comment: ArticleCommentRow,
    action: "approve" | "reject" | "hide" | "pending",
  ) => {
    if (!canModerate) return;
    setBusyId(comment.commentId);
    const runner =
      action === "approve"
        ? approveArticleComment
        : action === "reject"
          ? rejectArticleComment
          : action === "hide"
            ? hideArticleComment
            : markArticleCommentPending;
    const result = await runner(articleId, comment.commentId);
    setBusyId(null);
    setConfirm(null);
    if (result.state !== "ok") {
      toast.error(mapAdminErrorMessage(result.message ?? "admin.error.generic", "fa"));
      return;
    }
    toast.success(
      action === "approve"
        ? "نظر تأیید شد"
        : action === "reject"
          ? "نظر رد شد"
          : action === "hide"
            ? "نظر پنهان شد"
            : "نظر به حالت انتظار برگشت",
    );
    await refresh();
  };

  const seedComment = async () => {
    if (!canModerate) return;
    setBusyId("seed");
    const result = await createArticleComment(articleId, {
      displayName: seedName,
      body: seedBody,
    });
    setBusyId(null);
    if (result.state !== "ok") {
      toast.error(mapAdminErrorMessage(result.message ?? "admin.error.generic", "fa"));
      return;
    }
    toast.success("نظر آزمایشی افزوده شد");
    setSeedOpen(false);
    setSkip(0);
    await refresh();
  };

  const total = page?.totalCount ?? 0;
  const canPrev = skip > 0;
  const canNext = skip + PAGE_SIZE < total;

  return (
    <div className="space-y-4" data-testid="content-article-comments-panel">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <h3 className="text-sm font-semibold">نظرات مقاله</h3>
          <ContentHelpAffordance helpKey="comments" />
          {page && page.pendingCount > 0 ? (
            <span
              className="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-900"
              data-testid="content-article-comments-pending-badge"
            >
              {page.pendingCount} در انتظار
            </span>
          ) : null}
        </div>
        {canModerate ? (
          <button
            type="button"
            className="rounded-xl border px-3 py-1.5 text-xs"
            data-testid="content-article-comments-seed"
            onClick={() => setSeedOpen((v) => !v)}
          >
            افزودن نظر آزمایشی
          </button>
        ) : null}
      </div>

      {seedOpen ? (
        <div className="space-y-2 rounded-xl border bg-slate-50 p-3" data-testid="content-article-comments-seed-form">
          <p className="text-xs text-muted">فقط برای آزمایش تعدیل در محیط توسعه/ادمین — فرم عمومی نیست.</p>
          <input
            className="w-full rounded-xl border px-3 py-2 text-sm"
            value={seedName}
            onChange={(e) => setSeedName(e.target.value)}
            placeholder="نام نمایشی"
          />
          <textarea
            className="w-full rounded-xl border px-3 py-2 text-sm"
            rows={2}
            value={seedBody}
            onChange={(e) => setSeedBody(e.target.value)}
            placeholder="متن نظر"
          />
          <button
            type="button"
            className="rounded-xl bg-[#2563EB] px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-50"
            disabled={busyId === "seed"}
            onClick={() => void seedComment()}
          >
            ثبت نظر در انتظار
          </button>
        </div>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {STATUS_FILTERS.map((item) => (
          <button
            key={item.id || "all"}
            type="button"
            data-testid={`content-article-comments-filter-${item.id || "all"}`}
            className={
              status === item.id
                ? "rounded-lg bg-[#2563EB] px-2.5 py-1 text-xs font-semibold text-white"
                : "rounded-lg border px-2.5 py-1 text-xs"
            }
            onClick={() => {
              setStatus(item.id);
              setSkip(0);
            }}
          >
            {item.label}
          </button>
        ))}
      </div>

      <form
        className="flex flex-wrap gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          setSearchApplied(search);
          setSkip(0);
        }}
      >
        <input
          className="min-w-[12rem] flex-1 rounded-xl border px-3 py-2 text-sm"
          value={search}
          placeholder="جستجو در نام یا متن…"
          data-testid="content-article-comments-search"
          onChange={(e) => setSearch(e.target.value)}
        />
        <button type="submit" className="rounded-xl border px-3 py-2 text-sm">
          جستجو
        </button>
      </form>

      {loading ? (
        <p className="text-sm text-muted" data-testid="content-article-comments-loading">
          در حال بارگذاری نظرات…
        </p>
      ) : null}

      {!loading && error ? (
        <div className="rounded-xl border border-danger/30 bg-danger/5 p-3" data-testid="content-article-comments-error">
          <p className="text-sm text-danger">{error}</p>
          <button type="button" className="mt-2 rounded-lg border px-3 py-1 text-xs" onClick={() => void refresh()}>
            تلاش دوباره
          </button>
        </div>
      ) : null}

      {!loading && !error && page && page.items.length === 0 ? (
        <div
          className="rounded-xl border border-dashed p-6 text-center"
          data-testid="content-article-comments-empty"
        >
          <p className="text-sm font-medium">هنوز نظری برای این مقاله ثبت نشده است.</p>
          <p className="mt-1 text-xs text-muted">
            وقتی نظری اضافه شود، اینجا برای بررسی و تأیید نمایش داده می‌شود.
          </p>
        </div>
      ) : null}

      {!loading && !error && page && page.items.length > 0 ? (
        <ul className="space-y-3" data-testid="content-article-comments-list">
          {page.items.map((row) => (
            <li
              key={row.commentId}
              className="rounded-xl border p-3"
              data-testid={`content-article-comment-${row.commentId}`}
            >
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div className="min-w-0 space-y-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-sm font-semibold">{row.displayName}</span>
                    <span className="rounded-full border px-2 py-0.5 text-[11px]">
                      {articleCommentStatusLabel(row.status)}
                    </span>
                    <span className="text-[11px] text-muted">{formatWhen(row.createdAt)}</span>
                  </div>
                  <p className="whitespace-pre-wrap text-sm leading-6">{row.body}</p>
                  {row.moderationNote ? (
                    <p className="text-xs text-muted">یادداشت تعدیل: {row.moderationNote}</p>
                  ) : null}
                </div>
                {canModerate ? (
                  <div className="flex flex-wrap gap-1.5">
                    {row.status !== "Approved" ? (
                      <button
                        type="button"
                        className="rounded-lg border border-emerald-300 px-2.5 py-1 text-xs text-emerald-800 disabled:opacity-50"
                        disabled={busyId === row.commentId}
                        data-testid={`content-article-comment-approve-${row.commentId}`}
                        onClick={() => void runModerate(row, "approve")}
                      >
                        تأیید
                      </button>
                    ) : null}
                    {row.status !== "Rejected" ? (
                      <button
                        type="button"
                        className="rounded-lg border border-danger/40 px-2.5 py-1 text-xs text-danger disabled:opacity-50"
                        disabled={busyId === row.commentId}
                        data-testid={`content-article-comment-reject-${row.commentId}`}
                        onClick={() => setConfirm({ comment: row, action: "reject" })}
                      >
                        رد
                      </button>
                    ) : null}
                    {row.status !== "Hidden" ? (
                      <button
                        type="button"
                        className="rounded-lg border px-2.5 py-1 text-xs disabled:opacity-50"
                        disabled={busyId === row.commentId}
                        data-testid={`content-article-comment-hide-${row.commentId}`}
                        onClick={() => setConfirm({ comment: row, action: "hide" })}
                      >
                        پنهان
                      </button>
                    ) : null}
                    {row.status !== "Pending" ? (
                      <button
                        type="button"
                        className="rounded-lg border px-2.5 py-1 text-xs disabled:opacity-50"
                        disabled={busyId === row.commentId}
                        onClick={() => void runModerate(row, "pending")}
                      >
                        به انتظار
                      </button>
                    ) : null}
                  </div>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      ) : null}

      {!loading && !error && total > PAGE_SIZE ? (
        <div className="flex items-center justify-between gap-2 text-xs">
          <span className="text-muted">
            {skip + 1}–{Math.min(skip + PAGE_SIZE, total)} از {total}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              className="rounded-lg border px-2.5 py-1 disabled:opacity-40"
              disabled={!canPrev}
              onClick={() => setSkip((s) => Math.max(0, s - PAGE_SIZE))}
            >
              قبلی
            </button>
            <button
              type="button"
              className="rounded-lg border px-2.5 py-1 disabled:opacity-40"
              disabled={!canNext}
              onClick={() => setSkip((s) => s + PAGE_SIZE)}
            >
              بعدی
            </button>
          </div>
        </div>
      ) : null}

      {confirm ? (
        <div
          className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4"
          data-testid="content-article-comments-confirm"
        >
          <div className="w-full max-w-md rounded-2xl border bg-white p-4 shadow-xl">
            <p className="text-sm font-semibold">
              {confirm.action === "reject" ? "رد این نظر؟" : "پنهان کردن این نظر؟"}
            </p>
            <p className="mt-2 text-xs text-muted">
              تاریخچهٔ تعدیل حفظ می‌شود و نظر حذف سخت نمی‌شود.
            </p>
            <div className="mt-4 flex justify-end gap-2">
              <button type="button" className="rounded-xl border px-3 py-1.5 text-sm" onClick={() => setConfirm(null)}>
                انصراف
              </button>
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-3 py-1.5 text-sm font-semibold text-white"
                onClick={() => void runModerate(confirm.comment, confirm.action)}
              >
                تأیید
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
