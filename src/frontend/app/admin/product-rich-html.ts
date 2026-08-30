/**
 * Sanitizer for Product rich HTML descriptions.
 * Controlled tags/attrs only — no script/style injection.
 */

import DOMPurify from "isomorphic-dompurify";

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
];

const ALLOWED_ATTR = ["href", "target", "rel", "style", "colspan", "rowspan"];

/** Sanitize rich description HTML for persistence and display. */
export function sanitizeProductRichHtml(html: string): string {
  const raw = (html ?? "").trim();
  if (!raw) return "";
  const cleaned = DOMPurify.sanitize(raw, {
    ALLOWED_TAGS,
    ALLOWED_ATTR,
    ALLOW_DATA_ATTR: false,
    FORBID_TAGS: ["script", "style", "iframe", "object", "embed", "form", "input", "img"],
  });
  return cleaned.replace(/\sstyle="([^"]*)"/gi, (_full, styleValue: string) => {
    const kept = String(styleValue)
      .split(";")
      .map((part) => part.trim())
      .filter((part) => /^(font-family|font-size|text-align)\s*:/i.test(part))
      .join("; ");
    return kept ? ` style="${kept}"` : "";
  });
}

/** True when sanitized HTML has meaningful text content. */
export function richHtmlHasText(html: string): boolean {
  const plain = sanitizeProductRichHtml(html)
    .replace(/<[^>]+>/g, " ")
    .replace(/&nbsp;/g, " ")
    .trim();
  return plain.length > 0;
}
