/**
 * Sanitizer for Article rich HTML — controlled tags including DAM images only.
 */

import DOMPurify from "isomorphic-dompurify";
import { mediaPreviewUrl } from "../admin/media-api.ts";

const ALLOWED_TAGS = [
  "p",
  "br",
  "strong",
  "b",
  "em",
  "i",
  "u",
  "s",
  "h2",
  "h3",
  "h4",
  "ul",
  "ol",
  "li",
  "blockquote",
  "a",
  "table",
  "thead",
  "tbody",
  "tr",
  "th",
  "td",
  "span",
  "img",
  "figure",
  "figcaption",
];

const ALLOWED_ATTR = [
  "href",
  "target",
  "rel",
  "style",
  "colspan",
  "rowspan",
  "src",
  "alt",
  "title",
  "data-media-asset-id",
  "class",
];

const MEDIA_SRC_RE = /^\/v1\/storefront\/media\/[0-9a-f-]{36}$/i;

function isAllowedMediaSrc(src: string): boolean {
  const trimmed = src.trim();
  if (!MEDIA_SRC_RE.test(trimmed)) return false;
  return mediaPreviewUrl(trimmed.slice(trimmed.lastIndexOf("/") + 1)) === trimmed;
}

/** Sanitize article body HTML — فقط تصاویر DAM با URL عمومی مجاز. */
export function sanitizeArticleRichHtml(html: string): string {
  const raw = (html ?? "").trim();
  if (!raw) return "";
  if (/data:image/i.test(raw) || /data:application/i.test(raw)) return "";

  const cleaned = DOMPurify.sanitize(raw, {
    ALLOWED_TAGS,
    ALLOWED_ATTR,
    ALLOW_DATA_ATTR: false,
    FORBID_TAGS: ["script", "style", "iframe", "object", "embed", "form", "input"],
  });

  return cleaned.replace(/<img\b[^>]*>/gi, (tag) => {
    const srcMatch = /\bsrc=["']([^"']+)["']/i.exec(tag);
    const src = srcMatch?.[1] ?? "";
    if (!isAllowedMediaSrc(src)) return "";
    const idMatch = /\bdata-media-asset-id=["']([^"']+)["']/i.exec(tag);
    const assetId = idMatch?.[1] ?? src.slice(src.lastIndexOf("/") + 1);
    const altMatch = /\balt=["']([^"']*)["']/i.exec(tag);
    const alt = altMatch?.[1] ?? "";
    const titleMatch = /\btitle=["']([^"']*)["']/i.exec(tag);
    const title = titleMatch?.[1] ?? "";
    const safeAlt = alt.replace(/"/g, "&quot;");
    const safeTitle = title.replace(/"/g, "&quot;");
    const titleAttr = safeTitle ? ` title="${safeTitle}"` : "";
    return `<img src="${src}" alt="${safeAlt}" data-media-asset-id="${assetId}"${titleAttr} />`;
  }).replace(/\sstyle="([^"]*)"/gi, (_full, styleValue: string) => {
    const kept = String(styleValue)
      .split(";")
      .map((part) => part.trim())
      .filter((part) => /^(font-family|font-size|text-align)\s*:/i.test(part))
      .join("; ");
    return kept ? ` style="${kept}"` : "";
  });
}

/** URL تصویر درج‌شده در ویرایشگر مقاله. */
export function articleDamImageSrc(mediaAssetId: string): string {
  return mediaPreviewUrl(mediaAssetId) ?? `/v1/storefront/media/${mediaAssetId}`;
}
