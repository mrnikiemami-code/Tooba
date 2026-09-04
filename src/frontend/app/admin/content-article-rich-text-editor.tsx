"use client";

import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import Link from "@tiptap/extension-link";
import TextAlign from "@tiptap/extension-text-align";
import { Table } from "@tiptap/extension-table";
import TableRow from "@tiptap/extension-table-row";
import TableCell from "@tiptap/extension-table-cell";
import TableHeader from "@tiptap/extension-table-header";
import Placeholder from "@tiptap/extension-placeholder";
import Image from "@tiptap/extension-image";
import { useEffect, useMemo } from "react";
import { articleDamImageSrc, sanitizeArticleRichHtml } from "./article-rich-html.ts";

function ToolbarButton({
  active,
  disabled,
  onClick,
  children,
  testId,
  title,
}: {
  active?: boolean;
  disabled?: boolean;
  onClick: () => void;
  children: React.ReactNode;
  testId?: string;
  title?: string;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      title={title}
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

function ToolbarGroup({ children, label }: { children: React.ReactNode; label: string }) {
  return (
    <div className="flex flex-wrap items-center gap-1 border-e border-gray-200 pe-2 last:border-e-0 last:pe-0" role="group" aria-label={label}>
      {children}
    </div>
  );
}

/**
 * ویرایشگر غنی بدنهٔ مقاله — ترکیب TipTap مخصوص Content (بدون ویرایشگر GPL شخص ثالث).
 * RTL/LTR از زبان مقاله؛ sanitize با article-rich-html؛ درج تصویر از DAM.
 */
export function ContentArticleRichTextEditor({
  value,
  onChange,
  disabled,
  placeholder,
  dir = "rtl",
  testId = "content-article-rich-editor",
  onPickDamImage,
}: {
  value: string;
  onChange: (html: string) => void;
  disabled?: boolean;
  placeholder?: string;
  dir?: "rtl" | "ltr";
  testId?: string;
  onPickDamImage?: () => Promise<{ mediaAssetId: string; alt?: string; title?: string } | null>;
}) {
  const resolvedPlaceholder =
    placeholder ?? (dir === "rtl" ? "متن مقاله را بنویسید…" : "Write article body…");

  const extensions = useMemo(
    () => [
      StarterKit.configure({
        heading: { levels: [2, 3, 4] },
        codeBlock: false,
        code: false,
      }),
      Underline,
      TextAlign.configure({ types: ["heading", "paragraph"] }),
      Link.configure({
        openOnClick: false,
        HTMLAttributes: { rel: "noopener noreferrer", target: "_blank" },
      }),
      Table.configure({ resizable: false }),
      TableRow,
      TableHeader,
      TableCell,
      Placeholder.configure({ placeholder: resolvedPlaceholder }),
      Image.configure({
        inline: false,
        allowBase64: false,
        HTMLAttributes: { class: "article-dam-image" },
      }),
    ],
    [resolvedPlaceholder],
  );

  const editor = useEditor({
    immediatelyRender: false,
    editable: !disabled,
    extensions,
    content: value || "",
    onUpdate: ({ editor: ed }) => {
      onChange(sanitizeArticleRichHtml(ed.getHTML()));
    },
    editorProps: {
      attributes: {
        class:
          "min-h-[22rem] max-h-[40rem] overflow-y-auto px-4 py-3 text-base leading-8 text-slate-800 focus:outline-none prose prose-neutral max-w-none",
        dir,
        "data-testid": `${testId}-content`,
      },
      transformPastedHTML: (html) => sanitizeArticleRichHtml(html),
    },
  });

  useEffect(() => {
    if (!editor) return;
    editor.setEditable(!disabled);
  }, [disabled, editor]);

  useEffect(() => {
    if (!editor) return;
    const current = sanitizeArticleRichHtml(editor.getHTML());
    const next = sanitizeArticleRichHtml(value || "");
    if (current !== next) {
      editor.commands.setContent(next || "", { emitUpdate: false });
    }
  }, [value, editor]);

  useEffect(() => {
    if (!editor) return;
    editor.view.dom.setAttribute("dir", dir);
  }, [dir, editor]);

  if (!editor) {
    return (
      <div className="min-h-48 rounded-xl border border-gray-200 bg-slate-50 p-3 text-sm text-slate-500" data-testid={testId}>
        {dir === "rtl" ? "در حال آماده‌سازی ویرایشگر…" : "Preparing editor…"}
      </div>
    );
  }

  const blockValue = editor.isActive("heading", { level: 2 })
    ? "h2"
    : editor.isActive("heading", { level: 3 })
      ? "h3"
      : editor.isActive("heading", { level: 4 })
        ? "h4"
        : "p";

  const labels =
    dir === "rtl"
      ? {
          toolbar: "نوار ابزار محتوا",
          style: "سبک",
          paragraph: "پاراگراف",
          bold: "ضخیم",
          italic: "ایتالیک",
          underline: "زیرخط",
          strike: "خط‌خورده",
          bullet: "فهرست",
          ordered: "فهرست شماره‌دار",
          quote: "نقل‌قول",
          alignRight: "راست‌چین",
          alignCenter: "وسط‌چین",
          alignLeft: "چپ‌چین",
          link: "پیوند",
          table: "جدول",
          image: "درج تصویر",
          undo: "بازگردانی",
          redo: "از نو",
        }
      : {
          toolbar: "Content toolbar",
          style: "Style",
          paragraph: "Paragraph",
          bold: "Bold",
          italic: "Italic",
          underline: "Underline",
          strike: "Strike",
          bullet: "Bullet list",
          ordered: "Ordered list",
          quote: "Quote",
          alignRight: "Align right",
          alignCenter: "Align center",
          alignLeft: "Align left",
          link: "Link",
          table: "Table",
          image: "Insert image",
          undo: "Undo",
          redo: "Redo",
        };

  return (
    <div
      className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
      data-testid={testId}
      data-editor="tiptap"
      data-content-editor="article"
    >
      <div
        className="flex flex-wrap items-center gap-2 border-b border-gray-100 bg-slate-50 p-2"
        role="toolbar"
        aria-label={labels.toolbar}
        data-testid={`${testId}-toolbar`}
        dir={dir}
      >
        <ToolbarGroup label={labels.style}>
          <label className="inline-flex min-h-8 items-center gap-1 rounded-lg border border-gray-200 bg-white px-2 text-xs">
            <span className="text-muted">{labels.style}</span>
            <select
              className="bg-transparent text-xs"
              disabled={disabled}
              value={blockValue}
              data-testid={`${testId}-block-style`}
              onChange={(e) => {
                const next = e.target.value;
                if (next === "p") editor.chain().focus().setParagraph().run();
                else if (next === "h2") editor.chain().focus().toggleHeading({ level: 2 }).run();
                else if (next === "h3") editor.chain().focus().toggleHeading({ level: 3 }).run();
                else if (next === "h4") editor.chain().focus().toggleHeading({ level: 4 }).run();
              }}
            >
              <option value="p">{labels.paragraph}</option>
              <option value="h2">H2</option>
              <option value="h3">H3</option>
              <option value="h4">H4</option>
            </select>
          </label>
        </ToolbarGroup>

        <ToolbarGroup label="marks">
          <ToolbarButton
            active={editor.isActive("bold")}
            disabled={disabled}
            title={labels.bold}
            onClick={() => editor.chain().focus().toggleBold().run()}
          >
            {labels.bold}
          </ToolbarButton>
          <ToolbarButton
            active={editor.isActive("italic")}
            disabled={disabled}
            title={labels.italic}
            onClick={() => editor.chain().focus().toggleItalic().run()}
          >
            {labels.italic}
          </ToolbarButton>
          <ToolbarButton
            active={editor.isActive("underline")}
            disabled={disabled}
            title={labels.underline}
            onClick={() => editor.chain().focus().toggleUnderline().run()}
          >
            {labels.underline}
          </ToolbarButton>
          <ToolbarButton
            active={editor.isActive("strike")}
            disabled={disabled}
            title={labels.strike}
            testId={`${testId}-strike`}
            onClick={() => editor.chain().focus().toggleStrike().run()}
          >
            {labels.strike}
          </ToolbarButton>
        </ToolbarGroup>

        <ToolbarGroup label="lists">
          <ToolbarButton
            active={editor.isActive("bulletList")}
            disabled={disabled}
            title={labels.bullet}
            onClick={() => editor.chain().focus().toggleBulletList().run()}
          >
            •
          </ToolbarButton>
          <ToolbarButton
            active={editor.isActive("orderedList")}
            disabled={disabled}
            title={labels.ordered}
            onClick={() => editor.chain().focus().toggleOrderedList().run()}
          >
            1.
          </ToolbarButton>
          <ToolbarButton
            active={editor.isActive("blockquote")}
            disabled={disabled}
            title={labels.quote}
            onClick={() => editor.chain().focus().toggleBlockquote().run()}
          >
            “”
          </ToolbarButton>
        </ToolbarGroup>

        <ToolbarGroup label="align">
          <ToolbarButton
            active={editor.isActive({ textAlign: "right" })}
            disabled={disabled}
            title={labels.alignRight}
            onClick={() => editor.chain().focus().setTextAlign("right").run()}
          >
            راست
          </ToolbarButton>
          <ToolbarButton
            active={editor.isActive({ textAlign: "center" })}
            disabled={disabled}
            title={labels.alignCenter}
            onClick={() => editor.chain().focus().setTextAlign("center").run()}
          >
            وسط
          </ToolbarButton>
          <ToolbarButton
            active={editor.isActive({ textAlign: "left" })}
            disabled={disabled}
            title={labels.alignLeft}
            onClick={() => editor.chain().focus().setTextAlign("left").run()}
          >
            چپ
          </ToolbarButton>
        </ToolbarGroup>

        <ToolbarGroup label="insert">
          <ToolbarButton
            disabled={disabled}
            title={labels.link}
            onClick={() => {
              const href = window.prompt(dir === "rtl" ? "آدرس پیوند (https://…)" : "Link URL (https://…)");
              if (!href) return;
              editor.chain().focus().extendMarkRange("link").setLink({ href }).run();
            }}
          >
            {labels.link}
          </ToolbarButton>
          <ToolbarButton
            disabled={disabled}
            title={labels.table}
            onClick={() =>
              editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()
            }
          >
            {labels.table}
          </ToolbarButton>
          {onPickDamImage ? (
            <ToolbarButton
              disabled={disabled}
              testId={`${testId}-insert-image`}
              title={labels.image}
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
              {labels.image}
            </ToolbarButton>
          ) : null}
        </ToolbarGroup>

        <ToolbarGroup label="history">
          <ToolbarButton disabled={disabled} title={labels.undo} onClick={() => editor.chain().focus().undo().run()}>
            {labels.undo}
          </ToolbarButton>
          <ToolbarButton disabled={disabled} title={labels.redo} onClick={() => editor.chain().focus().redo().run()}>
            {labels.redo}
          </ToolbarButton>
        </ToolbarGroup>
      </div>
      <EditorContent editor={editor} />
    </div>
  );
}
