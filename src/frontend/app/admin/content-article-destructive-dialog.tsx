"use client";

import { useCallback } from "react";
import { Button, Dialog } from "../../design-system";

export type ArticleActionKind = "delete" | "archive" | "publish" | "unpublish";

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

function copyFor(kind: ArticleActionKind, locale: string): CopyBundle {
  const fa = isFaLocale(locale);
  switch (kind) {
    case "delete":
      return fa
        ? {
            title: "حذف مقاله",
            body: (articleTitle) =>
              `آیا از حذف دائمی «${articleTitle}» مطمئن هستید؟\n\nاین عمل قابل بازگشت نیست. دارایی‌های مشترک Media DAM حذف نمی‌شوند.`,
            cancel: "انصراف",
            confirm: "حذف مقاله",
            tone: "danger",
          }
        : {
            title: "Delete article",
            body: (articleTitle) =>
              `Are you sure you want to permanently delete “${articleTitle}”?\n\nThis cannot be undone. Shared Media DAM assets are not deleted.`,
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
      return fa
        ? {
            title: "انتشار مقاله",
            body: (articleTitle) =>
              `آیا «${articleTitle}» منتشر شود؟\n\nمقاله به‌صورت عمومی در دسترس قرار می‌گیرد و ممکن است طبق سیاست SEO وارد sitemap و ایندکس شود.`,
            cancel: "انصراف",
            confirm: "انتشار مقاله",
            tone: "primary",
          }
        : {
            title: "Publish article",
            body: (articleTitle) =>
              `Publish “${articleTitle}”?\n\nThe article becomes publicly accessible and may enter the sitemap/indexing according to SEO policy.`,
            cancel: "Cancel",
            confirm: "Publish article",
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

/** گفتگوی تأیید کنش مقاله (انتشار/لغو/حذف/بایگانی) — Dialog کاننیکال design-system. */
export function ContentArticleDestructiveDialog({
  kind,
  target,
  open,
  pending,
  onClose,
  onConfirm,
}: {
  kind: ArticleActionKind | null;
  target: ArticleDestructiveTarget | null;
  open: boolean;
  pending?: boolean;
  onClose: () => void;
  onConfirm: () => void | Promise<void>;
}) {
  const handleConfirm = useCallback(() => {
    if (pending) return;
    void onConfirm();
  }, [onConfirm, pending]);

  if (!kind || !target) return null;

  const copy = copyFor(kind, target.locale);
  const dir = isFaLocale(target.locale) ? "rtl" : "ltr";

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
            disabled={pending}
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
