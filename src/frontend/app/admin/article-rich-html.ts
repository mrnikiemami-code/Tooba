/**
 * Sanitizer for Article rich HTML — controlled tags including DAM images/files/videos only.
 *
 * Allowlist notes (TB-P08-T012-R1 / TB-P08-T016-R2 / TB-P08-T016-R4 / CKEditor 5):
 * - Tags: paragraph/headings/lists/quote/link/table/figure/img/video/source/hr/span — CKEditor emits these.
 * - Attrs: href/target/rel/style/colspan/rowspan/src/alt/title/class/data-media-asset-id/width/height/controls/preload.
 * - Styles kept: text-align, font-family, font-size, color, background-color, width, height, margin-left/right, float
 *   (alignment, indent, table/image resize, font color/highlight). Reject other style injection.
 * - font-family must match ALLOWED_FONT_FAMILIES (CKEditor stacks); font-size px 10–48 or known named sizes.
 * - img/video/a[data-media-asset-id] src/href must be `/v1/storefront/media/{guid}` only — no base64, no external hosts.
 * - Scripts, event handlers, javascript:, iframe/object/embed remain forbidden.
 */

import DOMPurify from "isomorphic-dompurify";
import { mediaPreviewUrl } from "./media-api.ts";

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

const SAFE_STYLE_PROPS = new Set([
  "font-family",
  "font-size",
  "color",
  "background-color",
  "text-align",
  "width",
  "height",
  "margin-left",
  "margin-right",
  "float",
]);

/** Normalized (lowercase, collapsed spaces, no quotes) CKEditor font stacks. */
const ALLOWED_FONT_FAMILIES = new Set([
  "arial, helvetica, sans-serif",
  "tahoma, geneva, sans-serif",
  "verdana, geneva, sans-serif",
  "times new roman, times, serif",
  "georgia, serif",
  "courier new, courier, monospace",
  "b nazanin, tahoma, arial, sans-serif",
  "vazirmatn, tahoma, arial, sans-serif",
]);

const NAMED_FONT_SIZES = new Set(["tiny", "small", "default", "big", "huge"]);

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

function decodeStyleEntities(value: string): string {
  return value
    .replace(/&quot;/gi, '"')
    .replace(/&#34;/g, '"')
    .replace(/&#39;/g, "'")
    .replace(/&apos;/gi, "'")
    .replace(/&amp;/gi, "&");
}

function normalizeFontFamily(value: string): string {
  return value
    .replace(/['"]/g, "")
    .replace(/\s*,\s*/g, ", ")
    .replace(/\s+/g, " ")
    .trim()
    .toLowerCase();
}

function isAllowedFontFamily(value: string): boolean {
  const normalized = normalizeFontFamily(value);
  if (!normalized || normalized === "inherit" || normalized === "default") return false;
  return ALLOWED_FONT_FAMILIES.has(normalized);
}

function isAllowedFontSize(value: string): boolean {
  const trimmed = value.trim().toLowerCase();
  if (NAMED_FONT_SIZES.has(trimmed)) return true;
  const px = /^(\d+(?:\.\d+)?)px$/.exec(trimmed);
  if (!px) return false;
  const n = Number(px[1]);
  return Number.isFinite(n) && n >= 10 && n <= 48;
}

/** Split CSS declarations without breaking on `;` inside quoted font-family values. */
function splitCssDeclarations(styleValue: string): string[] {
  const parts: string[] = [];
  let current = "";
  let quote: '"' | "'" | null = null;
  for (let i = 0; i < styleValue.length; i += 1) {
    const ch = styleValue[i]!;
    if (quote) {
      current += ch;
      if (ch === quote) quote = null;
      continue;
    }
    if (ch === '"' || ch === "'") {
      quote = ch;
      current += ch;
      continue;
    }
    if (ch === ";") {
      const trimmed = current.trim();
      if (trimmed) parts.push(trimmed);
      current = "";
      continue;
    }
    current += ch;
  }
  const tail = current.trim();
  if (tail) parts.push(tail);
  return parts;
}

function filterInlineStyle(styleValue: string): string {
  const decoded = decodeStyleEntities(styleValue);
  const kept: string[] = [];
  for (const part of splitCssDeclarations(decoded)) {
    const colon = part.indexOf(":");
    if (colon <= 0) continue;
    const prop = part.slice(0, colon).trim().toLowerCase();
    const rawValue = part.slice(colon + 1).trim();
    if (!SAFE_STYLE_PROPS.has(prop) || !rawValue) continue;
    if (prop === "font-family") {
      if (!isAllowedFontFamily(rawValue)) continue;
      const normalized = normalizeFontFamily(rawValue);
      // Re-emit with quotes around multi-word family names for valid CSS.
      const families = normalized.split(", ").map((family) =>
        /\s/.test(family) ? `"${family.replace(/"/g, "")}"` : family,
      );
      kept.push(`font-family:${families.join(", ")}`);
      continue;
    }
    if (prop === "font-size") {
      if (!isAllowedFontSize(rawValue)) continue;
      kept.push(`font-size:${rawValue.trim()}`);
      continue;
    }
    kept.push(`${prop}:${rawValue}`);
  }
  return kept.join("; ");
}

/**
 * Walk sanitized markup via DOM so quoted font-family values with spaces survive
 * (avoids brittle style="..." regex that breaks on embedded quotes).
 */
function filterStylesViaDom(html: string): string {
  if (!html) return "";
  try {
    const parser = new DOMParser();
    const doc = parser.parseFromString(`<div id="tooba-sanitize-root">${html}</div>`, "text/html");
    const root = doc.getElementById("tooba-sanitize-root");
    if (!root) return html;
    root.querySelectorAll("[style]").forEach((el) => {
      const filtered = filterInlineStyle(el.getAttribute("style") || "");
      if (filtered) el.setAttribute("style", filtered);
      else el.removeAttribute("style");
    });
    return root.innerHTML;
  } catch {
    // Fallback if DOMParser is unavailable — still allowlist props without quote-aware split.
    return html.replace(/\sstyle="([^"]*)"/gi, (_full, styleValue: string) => {
      const kept = filterInlineStyle(String(styleValue));
      return kept ? ` style="${kept.replace(/"/g, "&quot;")}"` : "";
    });
  }
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
    // HTML comment tokens survive DOMParser; NUL-delimited tokens were stripped and leaked as "DAMVIDEO0".
    const token = `<!--TOOBA_DAM_VIDEO_${videoHolders.length}-->`;
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
    });

  processed = filterStylesViaDom(processed);

  for (let i = 0; i < videoHolders.length; i += 1) {
    processed = processed.replace(`<!--TOOBA_DAM_VIDEO_${i}-->`, videoHolders[i]!);
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
