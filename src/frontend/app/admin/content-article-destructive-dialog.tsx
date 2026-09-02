"use client";

import { useCallback } from "react";
import { Button, Dialog } from "../../design-system";

export type ArticleDestructiveKind = "delete" | "archive";

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
}

function isFaLocale(locale: string): boolean {
  return locale.trim().toLowerCase().startsWith("fa");
}

function copyFor(kind: ArticleDestructiveKind, locale: string): CopyBundle {
  const fa = isFaLocale(locale);
  if (kind === "delete") {
    return fa
      ? {
          title: "حذف مقاله",
          body: (articleTitle) =>
            `آیا از حذف دائمی «${articleTitle}» مطمئن هستید؟\n\nاین عمل قابل بازگشت نیست. دارایی‌های مشترک Media DAM حذف نمی‌شوند.`,
          cancel: "انصراف",
          confirm: "حذف مقاله",
        }
      : {
          title: "Delete article",
          body: (articleTitle) =>
            `Are you sure you want to permanently delete “${articleTitle}”?\n\nThis cannot be undone. Shared Media DAM assets are not deleted.`,
          cancel: "Cancel",
          confirm: "Delete article",
        };
  }
  return fa
    ? {
        title: "بایگانی مقاله",
        body: (articleTitle) =>
          `آیا «${articleTitle}» بایگانی شود؟\n\nدیگر به‌صورت عمومی در دسترس نخواهد بود و از sitemap و مسیر عمومی حذف می‌شود. در پنل مدیریت و تاریخچه باقی می‌ماند.`,
        cancel: "انصراف",
        confirm: "بایگانی مقاله",
      }
    : {
        title: "Archive article",
        body: (articleTitle) =>
          `Archive “${articleTitle}”?\n\nIt will no longer be publicly accessible and will be removed from the sitemap and public route. It remains in Admin and history.`,
        cancel: "Cancel",
        confirm: "Archive article",
      };
}

/** گفتگوی تأیید تخریب‌پذیر مقاله — بدون window.confirm. */
export function ContentArticleDestructiveDialog({
  kind,
  target,
  open,
  pending,
  onClose,
  onConfirm,
}: {
  kind: ArticleDestructiveKind | null;
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
        data-testid={`content-article-destructive-dialog-${kind}`}
        dir={dir}
        role="alertdialog"
        aria-describedby="content-article-destructive-dialog-body"
      >
        <p id="content-article-destructive-dialog-body" className="whitespace-pre-line text-sm leading-7 text-muted">
          {copy.body(target.title)}
        </p>
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <Button
            type="button"
            tone="secondary"
            disabled={pending}
            data-testid="content-article-destructive-cancel"
            onClick={onClose}
          >
            {copy.cancel}
          </Button>
          <Button
            type="button"
            tone="danger"
            disabled={pending}
            data-testid={`content-article-destructive-confirm-${kind}`}
            onClick={handleConfirm}
          >
            {pending ? "…" : copy.confirm}
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
