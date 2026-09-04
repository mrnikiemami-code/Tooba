"use client";

import { useCallback } from "react";
import { Button, Dialog } from "../../design-system";
import {
  articleReadinessCheckLabel,
  type ArticlePublicationReadiness,
} from "./content-article-publication-model.ts";

export type ArticleActionKind = "delete" | "archive" | "publish" | "unpublish" | "republish";

/** @deprecated Use ArticleActionKind */
export type ArticleDestructiveKind = ArticleActionKind;

export interface ArticleDestructiveTarget {
  articleId: string;
  title: string;
  locale: string;
}

interface CopyBundle {
  title: string;
  body: (articleTitle: string) => string;
  cancel: string;
  confirm: string;
  tone: "primary" | "danger";
}

function isFaLocale(locale: string): boolean {
  return locale.trim().toLowerCase().startsWith("fa");
}

function copyFor(kind: ArticleActionKind, locale: string, scheduled: boolean): CopyBundle {
  const fa = isFaLocale(locale);
  switch (kind) {
    case "delete":
      return fa
        ? {
            title: "حذف مقاله",
            body: (articleTitle) =>
              `آیا از حذف دائمی «${articleTitle}» مطمئن هستید؟\n\nاین عمل قابل بازگشت نیست. فایل‌های مشترک کتابخانهٔ رسانه حذف نمی‌شوند.`,
            cancel: "انصراف",
            confirm: "حذف مقاله",
            tone: "danger",
          }
        : {
            title: "Delete article",
            body: (articleTitle) =>
              `Are you sure you want to permanently delete “${articleTitle}”?\n\nThis cannot be undone. Shared Media Library files are not deleted.`,
            cancel: "Cancel",
            confirm: "Delete article",
            tone: "danger",
          };
    case "archive":
      return fa
        ? {
            title: "بایگانی مقاله",
            body: (articleTitle) =>
              `آیا «${articleTitle}» بایگانی شود؟\n\nدیگر به‌صورت عمومی در دسترس نخواهد بود و از sitemap و مسیر عمومی حذف می‌شود. در پنل مدیریت و تاریخچه باقی می‌ماند.`,
            cancel: "انصراف",
            confirm: "بایگانی مقاله",
            tone: "danger",
          }
        : {
            title: "Archive article",
            body: (articleTitle) =>
              `Archive “${articleTitle}”?\n\nIt will no longer be publicly accessible and will be removed from the sitemap and public route. It remains in Admin and history.`,
            cancel: "Cancel",
            confirm: "Archive article",
            tone: "danger",
          };
    case "publish":
    case "republish":
      return fa
        ? {
            title: kind === "republish" ? "انتشار مجدد مقاله" : "انتشار مقاله",
            body: (articleTitle) =>
              scheduled
                ? `آیا «${articleTitle}» برای آینده زمان‌بندی شود؟\n\nوضعیت منتشر می‌شود اما تا رسیدن زمان انتشار در مسیر عمومی دیده نمی‌شود.`
                : `آیا «${articleTitle}» همین حالا منتشر شود؟\n\nمقاله بلافاصله در مسیر عمومی در دسترس قرار می‌گیرد و ممکن است طبق سیاست SEO وارد sitemap شود.`,
            cancel: "انصراف",
            confirm: kind === "republish" ? "انتشار مجدد" : "انتشار",
            tone: "primary",
          }
        : {
            title: kind === "republish" ? "Republish article" : "Publish article",
            body: (articleTitle) =>
              scheduled
                ? `Schedule “${articleTitle}” for the future?\n\nIt becomes Published but stays hidden from the public route until the publish time.`
                : `Publish “${articleTitle}” now?\n\nIt becomes publicly accessible immediately and may enter the sitemap per SEO policy.`,
            cancel: "Cancel",
            confirm: kind === "republish" ? "Republish" : "Publish",
            tone: "primary",
          };
    case "unpublish":
      return fa
        ? {
            title: "لغو انتشار مقاله",
            body: (articleTitle) =>
              `آیا انتشار «${articleTitle}» لغو شود؟\n\nدیگر به‌صورت عمومی در دسترس نخواهد بود و از lookup عمومی عادی و sitemap حذف می‌شود.`,
            cancel: "انصراف",
            confirm: "لغو انتشار",
            tone: "danger",
          }
        : {
            title: "Unpublish article",
            body: (articleTitle) =>
              `Unpublish “${articleTitle}”?\n\nIt will no longer be publicly accessible and will be excluded from normal public lookup and sitemap.`,
            cancel: "Cancel",
            confirm: "Unpublish",
            tone: "danger",
          };
  }
}

/** گفتگوی تأیید کنش مقاله — با چک‌لیست مسدودکنندهٔ آمادگی برای Publish. */
export function ContentArticleDestructiveDialog({
  kind,
  target,
  open,
  pending,
  readiness,
  scheduled,
  onClose,
  onConfirm,
  onNavigate,
}: {
  kind: ArticleActionKind | null;
  target: ArticleDestructiveTarget | null;
  open: boolean;
  pending?: boolean;
  readiness?: ArticlePublicationReadiness | null;
  scheduled?: boolean;
  onClose: () => void;
  onConfirm: () => void | Promise<void>;
  onNavigate?: (tab: string) => void;
}) {
  const handleConfirm = useCallback(() => {
    if (pending) return;
    void onConfirm();
  }, [onConfirm, pending]);

  if (!kind || !target) return null;

  const isPublishKind = kind === "publish" || kind === "republish";
  const blockers = isPublishKind ? (readiness?.requiredMissing ?? []) : [];
  const blocked = isPublishKind && blockers.length > 0;
  const copy = copyFor(kind, target.locale, Boolean(scheduled));
  const dir = isFaLocale(target.locale) ? "rtl" : "ltr";
  const fa = isFaLocale(target.locale);

  return (
    <Dialog title={copy.title} open={open} onClose={onClose} showCloseButton={false}>
      <div
        data-testid={`content-article-action-dialog-${kind}`}
        dir={dir}
        role="alertdialog"
        aria-describedby="content-article-action-dialog-body"
      >
        <p id="content-article-action-dialog-body" className="whitespace-pre-line text-sm leading-7 text-muted">
          {copy.body(target.title)}
        </p>
        {blocked ? (
          <div
            className="mt-3 rounded-xl border border-danger/30 bg-danger/5 p-3 text-sm"
            data-testid="content-article-publish-blockers"
          >
            <p className="mb-2 font-semibold text-danger">
              {fa ? "موارد الزامی ناقص — انتشار انجام نمی‌شود:" : "Required blockers — publish will not run:"}
            </p>
            <ul className="space-y-1">
              {blockers.map((check) => (
                <li key={check.key}>
                  <button
                    type="button"
                    className="text-danger underline"
                    onClick={() => {
                      if (check.actionTarget && onNavigate) onNavigate(check.actionTarget);
                      onClose();
                    }}
                  >
                    {articleReadinessCheckLabel(check, target.locale)}
                  </button>
                </li>
              ))}
            </ul>
          </div>
        ) : null}
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <Button
            type="button"
            tone="secondary"
            disabled={pending}
            data-testid="content-article-action-cancel"
            onClick={onClose}
          >
            {copy.cancel}
          </Button>
          <Button
            type="button"
            tone={copy.tone}
            disabled={pending || blocked}
            data-testid={`content-article-action-confirm-${kind}`}
            onClick={handleConfirm}
          >
            {pending ? "…" : copy.confirm}
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
