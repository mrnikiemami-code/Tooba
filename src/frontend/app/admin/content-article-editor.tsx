"use client";

/**
 * Canonical Article body editor — CKEditor 5 (self-hosted), client-only via dynamic import.
 * Product TipTap remains separate; this component is Article-only.
 */

import dynamic from "next/dynamic";
import type { ContentArticleCkEditorProps } from "./content-article-ckeditor.tsx";

const ContentArticleCkEditor = dynamic(() => import("./content-article-ckeditor.tsx"), {
  ssr: false,
  loading: () => (
    <div
      className="min-h-48 rounded-xl border border-gray-200 bg-slate-50 p-3 text-sm text-slate-500"
      data-testid="content-article-rich-editor"
      data-editor="ckeditor5"
      data-content-editor="article"
    >
      در حال آماده‌سازی ویرایشگر…
    </div>
  ),
});

export type ContentArticleEditorProps = ContentArticleCkEditorProps;

/** Article CMS body editor (CKEditor 5 + Tooba DAM image/file/video insert). */
export function ContentArticleEditor(props: ContentArticleEditorProps) {
  return <ContentArticleCkEditor {...props} />;
}
