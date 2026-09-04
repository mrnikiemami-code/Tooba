"use client";

import { sanitizeArticleRichHtml } from "../admin/article-rich-html.ts";

/** رندر بدنهٔ مقاله با همان sanitization عمومی و پیش‌نمایش Admin. */
export function ArticleBodyHtml({ html, className }: { html: string; className?: string }) {
  const safe = sanitizeArticleRichHtml(html || "");
  if (!safe) {
    return <p className={className}>{html}</p>;
  }
  return (
    <div
      className={className}
      data-testid="article-body-html"
      dangerouslySetInnerHTML={{ __html: safe }}
    />
  );
}
