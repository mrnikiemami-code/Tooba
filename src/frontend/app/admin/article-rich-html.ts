/**
 * Sanitizer for Article rich HTML — controlled tags including DAM images/files/videos only.
 *
 * Allowlist notes (TB-P08-T012-R1 / TB-P08-T016-R2 / CKEditor 5):
 * - Tags: paragraph/headings/lists/quote/link/table/figure/img/video/source/hr/span — CKEditor emits these.
 * - Attrs: href/target/rel/style/colspan/rowspan/src/alt/title/class/data-media-asset-id/width/height/controls/preload.
 * - Styles kept: text-align, font-family, font-size, color, background-color, width, height, margin-left/right, float
 *   (alignment, indent, table/image resize, font color/highlight). Reject other style injection.
 * - img/video/a[data-media-asset-id] src/href must be `/v1/storefront/media/{guid}` only — no base64, no external hosts.
 * - Scripts, event handlers, javascript:, iframe/object/embed remain forbidden.
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
  "video",
  "source",
  "hr",
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
  "width",
  "height",
  "controls",
  "preload",
];

/** Safe CKEditor / alignment / table / DAM classes only (prefix or exact). */
const SAFE_CLASS_RE =
  /^(article-dam-image|article-dam-file|article-dam-video|image([_-][\w-]+)?|image-style-[\w-]+|table|ck-table-[\w-]+|ck-[\w-]+|text-[\w-]+|marker-[\w-]+|pen-[\w-]+)$/i;

const MEDIA_SRC_RE = /^\/v1\/storefront\/media\/[0-9a-f-]{36}$/i;

const SAFE_STYLE_RE =
  /^(font-family|font-size|color|background-color|text-align|width|height|margin-left|margin-right|float)\s*:/i;

function isAllowedMediaSrc(src: string): boolean {
  const trimmed = src.trim();
  if (!MEDIA_SRC_RE.test(trimmed)) return false;
  return mediaPreviewUrl(trimmed.slice(trimmed.lastIndexOf("/") + 1)) === trimmed;
}

function filterClasses(classValue: string): string {
  return classValue
    .split(/\s+/)
    .map((c) => c.trim())
    .filter((c) => c && SAFE_CLASS_RE.test(c))
    .join(" ");
}

function escapeAttrValue(value: string): string {
  return value.replace(/"/g, "&quot;");
}

function rebuildDamVideoTag(attrs: string): string {
  const srcMatch = /\bsrc=["']([^"']+)["']/i.exec(attrs);
  const src = srcMatch?.[1] ?? "";
  if (!isAllowedMediaSrc(src)) return "";
  const idMatch = /\bdata-media-asset-id=["']([^"']+)["']/i.exec(attrs);
  const assetId = idMatch?.[1] ?? src.slice(src.lastIndexOf("/") + 1);
  const classMatch = /\bclass=["']([^"']*)["']/i.exec(attrs);
  const safeClass = filterClasses(classMatch?.[1] ?? "article-dam-video") || "article-dam-video";
  const preloadMatch = /\bpreload=["']([^"']+)["']/i.exec(attrs);
  const preload =
    preloadMatch?.[1] && /^(none|metadata|auto)$/i.test(preloadMatch[1])
      ? preloadMatch[1].toLowerCase()
      : "metadata";
  return `<video class="${safeClass}" controls preload="${preload}" src="${src}" data-media-asset-id="${assetId}"></video>`;
}

/** Sanitize article body HTML — فقط رسانهٔ DAM با URL عمومی مجاز. */
export function sanitizeArticleRichHtml(html: string): string {
  const raw = (html ?? "").trim();
  if (!raw) return "";
  if (/data:image/i.test(raw) || /data:application/i.test(raw) || /data:video/i.test(raw)) return "";

  const cleaned = DOMPurify.sanitize(raw, {
    ALLOWED_TAGS,
    ALLOWED_ATTR,
    ALLOW_DATA_ATTR: false,
    FORBID_TAGS: ["script", "style", "iframe", "object", "embed", "form", "input"],
    FORBID_ATTR: ["srcdoc"],
  });

  const videoHolders: string[] = [];
  const holdVideo = (markup: string): string => {
    if (!markup) return "";
    const token = `\u0000DAMVIDEO${videoHolders.length}\u0000`;
    videoHolders.push(markup);
    return token;
  };

  let processed = cleaned
    .replace(/<img\b[^>]*>/gi, (tag) => {
      const srcMatch = /\bsrc=["']([^"']+)["']/i.exec(tag);
      const src = srcMatch?.[1] ?? "";
      if (!isAllowedMediaSrc(src)) return "";
      const idMatch = /\bdata-media-asset-id=["']([^"']+)["']/i.exec(tag);
      const assetId = idMatch?.[1] ?? src.slice(src.lastIndexOf("/") + 1);
      const altMatch = /\balt=["']([^"']*)["']/i.exec(tag);
      const alt = altMatch?.[1] ?? "";
      const titleMatch = /\btitle=["']([^"']*)["']/i.exec(tag);
      const title = titleMatch?.[1] ?? "";
      const classMatch = /\bclass=["']([^"']*)["']/i.exec(tag);
      const safeClass = filterClasses(classMatch?.[1] ?? "article-dam-image") || "article-dam-image";
      const widthMatch = /\bwidth=["']([^"']+)["']/i.exec(tag);
      const heightMatch = /\bheight=["']([^"']+)["']/i.exec(tag);
      const safeAlt = escapeAttrValue(alt);
      const safeTitle = escapeAttrValue(title);
      const titleAttr = safeTitle ? ` title="${safeTitle}"` : "";
      const widthAttr = widthMatch?.[1] && /^\d+(\.\d+)?%?$/.test(widthMatch[1]) ? ` width="${widthMatch[1]}"` : "";
      const heightAttr =
        heightMatch?.[1] && /^\d+(\.\d+)?%?$/.test(heightMatch[1]) ? ` height="${heightMatch[1]}"` : "";
      return `<img class="${safeClass}" src="${src}" alt="${safeAlt}" data-media-asset-id="${assetId}"${titleAttr}${widthAttr}${heightAttr} />`;
    })
    .replace(/<video\b([^>]*)>([\s\S]*?)<\/video>/gi, (_full, attrs: string) => holdVideo(rebuildDamVideoTag(attrs)))
    .replace(/<video\b([^>]*)\s*\/>/gi, (_full, attrs: string) => holdVideo(rebuildDamVideoTag(attrs)))
    .replace(/<video\b[^>]*>/gi, "")
    .replace(/<\/video>/gi, "")
    .replace(/<source\b[^>]*>/gi, (tag) => {
      const srcMatch = /\bsrc=["']([^"']+)["']/i.exec(tag);
      const src = srcMatch?.[1] ?? "";
      if (!isAllowedMediaSrc(src)) return "";
      return `<source src="${src}" />`;
    })
    .replace(/<a\b([^>]*)>/gi, (full, attrs: string) => {
      const idMatch = /\bdata-media-asset-id=["']([^"']+)["']/i.exec(attrs);
      if (!idMatch) return full;
      const hrefMatch = /\bhref=["']([^"']+)["']/i.exec(attrs);
      const href = hrefMatch?.[1] ?? "";
      if (!isAllowedMediaSrc(href)) return "";
      const assetId = idMatch[1];
      const classMatch = /\bclass=["']([^"']*)["']/i.exec(attrs);
      const safeClass = filterClasses(classMatch?.[1] ?? "article-dam-file") || "article-dam-file";
      const titleMatch = /\btitle=["']([^"']*)["']/i.exec(attrs);
      const titleAttr = titleMatch?.[1] ? ` title="${escapeAttrValue(titleMatch[1])}"` : "";
      return `<a class="${safeClass}" href="${href}" data-media-asset-id="${assetId}" target="_blank" rel="noopener noreferrer"${titleAttr}>`;
    })
    .replace(/\sclass="([^"]*)"/gi, (_full, classValue: string) => {
      const kept = filterClasses(classValue);
      return kept ? ` class="${kept}"` : "";
    })
    .replace(/\sstyle="([^"]*)"/gi, (_full, styleValue: string) => {
      const kept = String(styleValue)
        .split(";")
        .map((part) => part.trim())
        .filter((part) => SAFE_STYLE_RE.test(part))
        .join("; ");
      return kept ? ` style="${kept}"` : "";
    });

  for (let i = 0; i < videoHolders.length; i += 1) {
    processed = processed.replace(`\u0000DAMVIDEO${i}\u0000`, videoHolders[i]);
  }
  return processed;
}

/** URL تصویر درج‌شده در ویرایشگر مقاله. */
export function articleDamImageSrc(mediaAssetId: string): string {
  return mediaPreviewUrl(mediaAssetId) ?? `/v1/storefront/media/${mediaAssetId}`;
}

/** URL رسانهٔ DAM (تصویر / فایل / ویدیو) — همان مسیر عمومی. */
export function articleDamMediaSrc(mediaAssetId: string): string {
  return articleDamImageSrc(mediaAssetId);
}
