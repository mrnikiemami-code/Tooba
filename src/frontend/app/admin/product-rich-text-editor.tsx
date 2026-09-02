"use client";

import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import Link from "@tiptap/extension-link";
import TextAlign from "@tiptap/extension-text-align";
import { TextStyle } from "@tiptap/extension-text-style";
import FontFamily from "@tiptap/extension-font-family";
import { Table } from "@tiptap/extension-table";
import TableRow from "@tiptap/extension-table-row";
import TableCell from "@tiptap/extension-table-cell";
import TableHeader from "@tiptap/extension-table-header";
import Placeholder from "@tiptap/extension-placeholder";
import Image from "@tiptap/extension-image";
import { Extension } from "@tiptap/core";
import { useEffect, useMemo } from "react";
import { sanitizeProductRichHtml } from "./product-rich-html";
import { articleDamImageSrc } from "./article-rich-html.ts";

/** Controlled font-size marks via inline style (no free-text CSS). */
const FontSize = Extension.create({
  name: "fontSize",
  addOptions() {
    return { types: ["textStyle"] };
  },
  addGlobalAttributes() {
    return [
      {
        types: this.options.types,
        attributes: {
          fontSize: {
            default: null,
            parseHTML: (element) => (element as HTMLElement).style.fontSize || null,
            renderHTML: (attributes) => {
              if (!attributes.fontSize) return {};
              return { style: `font-size: ${attributes.fontSize}` };
            },
          },
        },
      },
    ];
  },
  addCommands() {
    return {
      setFontSize:
        (fontSize: string) =>
        ({ chain }) =>
          chain().setMark("textStyle", { fontSize }).run(),
      unsetFontSize:
        () =>
        ({ chain }) =>
          chain().setMark("textStyle", { fontSize: null }).removeEmptyTextStyle().run(),
    };
  },
});

declare module "@tiptap/core" {
  interface Commands<ReturnType> {
    fontSize: {
      setFontSize: (fontSize: string) => ReturnType;
      unsetFontSize: () => ReturnType;
    };
  }
}

export const PRODUCT_RICH_FONT_FAMILIES = [
  { value: "var(--font-fa, Tahoma, sans-serif)", label: "فونت پیش‌فرض فارسی" },
  { value: "Tahoma, sans-serif", label: "Tahoma" },
  { value: "Georgia, serif", label: "Georgia" },
  { value: "Arial, Helvetica, sans-serif", label: "Arial" },
] as const;

export const PRODUCT_RICH_FONT_SIZES = [
  { value: "14px", label: "۱۴" },
  { value: "16px", label: "۱۶" },
  { value: "18px", label: "۱۸" },
  { value: "20px", label: "۲۰" },
  { value: "24px", label: "۲۴" },
] as const;

function ToolbarButton({
  active,
  disabled,
  onClick,
  children,
  testId,
}: {
  active?: boolean;
  disabled?: boolean;
  onClick: () => void;
  children: React.ReactNode;
  testId?: string;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      data-testid={testId}
      aria-pressed={active}
      className={
        active
          ? "min-h-8 rounded-lg bg-blue-600 px-2 text-xs font-semibold text-white"
          : "min-h-8 rounded-lg border border-gray-200 bg-white px-2 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-40"
      }
      onClick={onClick}
    >
      {children}
    </button>
  );
}

/**
 * ویرایشگر غنی توضیح محصول — TipTap (جایگزین CKEditor به‌خاطر GPL).
 * خانواده/اندازه فونت فقط از allowlist؛ تصویر درون‌خطی غیرفعال تا یکپارچگی امن DAM.
 */
export function ProductRichTextEditor({
  value,
  onChange,
  disabled,
  placeholder = "توضیح کامل محصول را بنویسید…",
  dir = "rtl",
  testId = "product-rich-text-editor",
  sanitizeHtml = sanitizeProductRichHtml,
  onPickDamImage,
}: {
  value: string;
  onChange: (html: string) => void;
  disabled?: boolean;
  placeholder?: string;
  dir?: "rtl" | "ltr";
  testId?: string;
  /** تابع sanitize — پیش‌فرض محصول (بدون img). */
  sanitizeHtml?: (html: string) => string;
  /** در صورت تنظیم، دکمهٔ درج تصویر از DAM نمایش داده می‌شود. */
  onPickDamImage?: () => Promise<{ mediaAssetId: string; alt?: string; title?: string } | null>;
}) {
  const extensions = useMemo(() => {
    const base = [
      StarterKit.configure({
        heading: { levels: [2, 3, 4] },
        codeBlock: false,
        code: false,
      }),
      Underline,
      TextStyle,
      FontFamily,
      FontSize,
      TextAlign.configure({ types: ["heading", "paragraph"] }),
      Link.configure({
        openOnClick: false,
        HTMLAttributes: { rel: "noopener noreferrer", target: "_blank" },
      }),
      Table.configure({ resizable: false }),
      TableRow,
      TableHeader,
      TableCell,
      Placeholder.configure({ placeholder }),
    ];
    if (onPickDamImage) {
      base.push(
        Image.configure({
          inline: false,
          allowBase64: false,
          HTMLAttributes: { class: "article-dam-image" },
        }),
      );
    }
    return base;
  }, [onPickDamImage, placeholder]);

  const editor = useEditor({
    immediatelyRender: false,
    editable: !disabled,
    extensions,
    content: value || "",
    onUpdate: ({ editor: ed }) => {
      onChange(sanitizeHtml(ed.getHTML()));
    },
    editorProps: {
      attributes: {
        class:
          "min-h-40 max-h-80 overflow-y-auto px-3 py-2 text-sm leading-7 text-slate-800 focus:outline-none prose prose-sm max-w-none",
        dir,
        "data-testid": `${testId}-content`,
      },
    },
  });

  useEffect(() => {
    if (!editor) return;
    editor.setEditable(!disabled);
  }, [disabled, editor]);

  useEffect(() => {
    if (!editor) return;
    const current = sanitizeHtml(editor.getHTML());
    const next = sanitizeHtml(value || "");
    if (current !== next) {
      editor.commands.setContent(next || "", { emitUpdate: false });
    }
  }, [value, editor, sanitizeHtml]);

  if (!editor) {
    return (
      <div className="min-h-48 rounded-xl border border-gray-200 bg-slate-50 p-3 text-sm text-slate-500" data-testid={testId}>
        در حال آماده‌سازی ویرایشگر…
      </div>
    );
  }

  return (
    <div
      className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
      data-testid={testId}
      data-editor="tiptap"
    >
      <div
        className="flex flex-wrap gap-1 border-b border-gray-100 bg-slate-50 p-2"
        role="toolbar"
        aria-label="نوار ابزار توضیح محصول"
        data-testid={`${testId}-toolbar`}
      >
        <ToolbarButton
          active={editor.isActive("heading", { level: 2 })}
          disabled={disabled}
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
        >
          H2
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive("heading", { level: 3 })}
          disabled={disabled}
          onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
        >
          H3
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive("bold")}
          disabled={disabled}
          onClick={() => editor.chain().focus().toggleBold().run()}
        >
          Bold
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive("italic")}
          disabled={disabled}
          onClick={() => editor.chain().focus().toggleItalic().run()}
        >
          Italic
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive("underline")}
          disabled={disabled}
          onClick={() => editor.chain().focus().toggleUnderline().run()}
        >
          Underline
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive("bulletList")}
          disabled={disabled}
          onClick={() => editor.chain().focus().toggleBulletList().run()}
        >
          • List
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive("orderedList")}
          disabled={disabled}
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
        >
          1. List
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive({ textAlign: "right" })}
          disabled={disabled}
          onClick={() => editor.chain().focus().setTextAlign("right").run()}
        >
          راست
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive({ textAlign: "center" })}
          disabled={disabled}
          onClick={() => editor.chain().focus().setTextAlign("center").run()}
        >
          وسط
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive({ textAlign: "left" })}
          disabled={disabled}
          onClick={() => editor.chain().focus().setTextAlign("left").run()}
        >
          چپ
        </ToolbarButton>
        <ToolbarButton
          active={editor.isActive("blockquote")}
          disabled={disabled}
          onClick={() => editor.chain().focus().toggleBlockquote().run()}
        >
          نقل‌قول
        </ToolbarButton>
        <ToolbarButton
          disabled={disabled}
          onClick={() => {
            const href = window.prompt("آدرس پیوند (https://…)");
            if (!href) return;
            editor.chain().focus().extendMarkRange("link").setLink({ href }).run();
          }}
        >
          پیوند
        </ToolbarButton>
        <ToolbarButton
          disabled={disabled}
          onClick={() =>
            editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()
          }
        >
          جدول
        </ToolbarButton>
        {onPickDamImage ? (
          <ToolbarButton
            disabled={disabled}
            testId={`${testId}-insert-image`}
            onClick={() => {
              void onPickDamImage().then((picked) => {
                if (!picked) return;
                const src = articleDamImageSrc(picked.mediaAssetId);
                const alt = picked.alt ?? "";
                const title = picked.title ?? "";
                editor
                  .chain()
                  .focus()
                  .insertContent(
                    `<img src="${src}" alt="${alt.replace(/"/g, "&quot;")}" data-media-asset-id="${picked.mediaAssetId}"${title ? ` title="${title.replace(/"/g, "&quot;")}"` : ""} />`,
                  )
                  .run();
              });
            }}
          >
            تصویر
          </ToolbarButton>
        ) : null}
        <ToolbarButton disabled={disabled} onClick={() => editor.chain().focus().undo().run()}>
          Undo
        </ToolbarButton>
        <ToolbarButton disabled={disabled} onClick={() => editor.chain().focus().redo().run()}>
          Redo
        </ToolbarButton>
        <label className="inline-flex min-h-8 items-center gap-1 rounded-lg border border-gray-200 bg-white px-2 text-xs">
          فونت
          <select
            className="max-w-[9rem] bg-transparent text-xs"
            disabled={disabled}
            data-testid={`${testId}-font-family`}
            defaultValue=""
            onChange={(e) => {
              const v = e.target.value;
              if (!v) editor.chain().focus().unsetFontFamily().run();
              else editor.chain().focus().setFontFamily(v).run();
            }}
          >
            <option value="">پیش‌فرض</option>
            {PRODUCT_RICH_FONT_FAMILIES.map((f) => (
              <option key={f.value} value={f.value}>
                {f.label}
              </option>
            ))}
          </select>
        </label>
        <label className="inline-flex min-h-8 items-center gap-1 rounded-lg border border-gray-200 bg-white px-2 text-xs">
          اندازه
          <select
            className="bg-transparent text-xs"
            disabled={disabled}
            data-testid={`${testId}-font-size`}
            defaultValue=""
            onChange={(e) => {
              const v = e.target.value;
              if (!v) editor.chain().focus().unsetFontSize().run();
              else editor.chain().focus().setFontSize(v).run();
            }}
          >
            <option value="">پیش‌فرض</option>
            {PRODUCT_RICH_FONT_SIZES.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </label>
      </div>
      <EditorContent editor={editor} />
    </div>
  );
}
