"use client";

/**
 * CKEditor 5 Article body editor (client-only; loaded via dynamic import).
 * Self-hosted open-source plugins only — DAM image insert via parent callback.
 */

import { useEffect, useMemo, useRef } from "react";
import { CKEditor } from "@ckeditor/ckeditor5-react";
import {
  Alignment,
  BlockQuote,
  Bold,
  ClassicEditor,
  Essentials,
  FindAndReplace,
  GeneralHtmlSupport,
  Heading,
  Image,
  ImageCaption,
  ImageResize,
  ImageStyle,
  ImageTextAlternative,
  ImageToolbar,
  Indent,
  IndentBlock,
  Italic,
  Link,
  List,
  Paragraph,
  Plugin,
  ButtonView,
  Strikethrough,
  Table,
  TableCaption,
  TableCellProperties,
  TableProperties,
  TableToolbar,
  Underline,
  Undo,
  type Editor,
  type EditorConfig,
} from "ckeditor5";
import translationsFa from "ckeditor5/translations/fa.js";
import "ckeditor5/ckeditor5.css";
import "./content-article-ckeditor.css";
import { articleDamImageSrc, sanitizeArticleRichHtml } from "./article-rich-html.ts";

export type DamImagePick = {
  mediaAssetId: string;
  alt?: string;
  title?: string;
} | null;

export type ContentArticleCkEditorProps = {
  value: string;
  onChange: (html: string) => void;
  disabled?: boolean;
  placeholder?: string;
  dir?: "rtl" | "ltr";
  testId?: string;
  onPickDamImage?: () => Promise<DamImagePick>;
};

type DamPickerConfig = () => Promise<DamImagePick>;

declare module "ckeditor5" {
  interface EditorConfig {
    damImagePicker?: DamPickerConfig;
  }
}

function escapeAttr(value: string): string {
  return value.replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");
}

function insertDamImageHtml(editor: Editor, picked: NonNullable<DamImagePick>): void {
  const src = articleDamImageSrc(picked.mediaAssetId);
  const alt = escapeAttr(picked.alt ?? "");
  const titleAttr = picked.title ? ` title="${escapeAttr(picked.title)}"` : "";
  const html = `<figure class="image"><img class="article-dam-image" src="${src}" alt="${alt}" data-media-asset-id="${picked.mediaAssetId}"${titleAttr} /></figure>`;
  const viewFragment = editor.data.processor.toView(html);
  const modelFragment = editor.data.toModel(viewFragment);
  editor.model.insertContent(modelFragment);
}

/** Toolbar action that opens Tooba Media Library via React callback (no window globals). */
class DamImageInsert extends Plugin {
  public static get pluginName() {
    return "DamImageInsert" as const;
  }

  public init(): void {
    const editor = this.editor;
    editor.ui.componentFactory.add("damImage", (locale) => {
      const view = new ButtonView(locale);
      const isRtl = locale.uiLanguageDirection === "rtl";
      view.set({
        label: isRtl ? "درج تصویر" : "Insert image",
        tooltip: true,
        withText: true,
        class: "ck-dam-image-button",
      });

      view.on("execute", () => {
        const picker = editor.config.get("damImagePicker") as DamPickerConfig | undefined;
        if (!picker) return;
        void picker().then((picked) => {
          if (!picked?.mediaAssetId) return;
          insertDamImageHtml(editor, picked);
          editor.editing.view.focus();
        });
      });

      return view;
    });
  }
}

function buildConfig(options: {
  dir: "rtl" | "ltr";
  placeholder: string;
  onPickDamImage?: () => Promise<DamImagePick>;
}): EditorConfig {
  const isRtl = options.dir === "rtl";
  return {
    licenseKey: "GPL",
    plugins: [
      Essentials,
      Paragraph,
      Heading,
      Bold,
      Italic,
      Underline,
      Strikethrough,
      List,
      Indent,
      IndentBlock,
      BlockQuote,
      Alignment,
      Link,
      Table,
      TableToolbar,
      TableProperties,
      TableCellProperties,
      TableCaption,
      Image,
      ImageToolbar,
      ImageCaption,
      ImageStyle,
      ImageResize,
      ImageTextAlternative,
      FindAndReplace,
      Undo,
      GeneralHtmlSupport,
      DamImageInsert,
    ],
    toolbar: {
      items: [
        "heading",
        "|",
        "bold",
        "italic",
        "underline",
        "strikethrough",
        "|",
        "bulletedList",
        "numberedList",
        "outdent",
        "indent",
        "|",
        "blockQuote",
        "alignment",
        "|",
        "link",
        "insertTable",
        "damImage",
        "findAndReplace",
        "|",
        "undo",
        "redo",
      ],
      shouldNotGroupWhenFull: true,
    },
    heading: {
      options: [
        { model: "paragraph", title: isRtl ? "پاراگراف" : "Paragraph", class: "ck-heading_paragraph" },
        { model: "heading2", view: "h2", title: "H2", class: "ck-heading_heading2" },
        { model: "heading3", view: "h3", title: "H3", class: "ck-heading_heading3" },
        { model: "heading4", view: "h4", title: "H4", class: "ck-heading_heading4" },
      ],
    },
    placeholder: options.placeholder,
    language: {
      ui: isRtl ? "fa" : "en",
      content: isRtl ? "fa" : "en",
    },
    translations: isRtl ? [translationsFa] : [],
    link: {
      defaultProtocol: "https://",
      decorators: {
        openInNewTab: {
          mode: "manual",
          label: isRtl ? "باز کردن در برگه جدید" : "Open in a new tab",
          attributes: {
            target: "_blank",
            rel: "noopener noreferrer",
          },
        },
      },
    },
    image: {
      toolbar: [
        "imageTextAlternative",
        "toggleImageCaption",
        "|",
        "imageStyle:inline",
        "imageStyle:block",
        "imageStyle:side",
        "|",
        "resizeImage",
      ],
    },
    table: {
      contentToolbar: ["tableColumn", "tableRow", "mergeTableCells", "tableProperties", "tableCellProperties", "toggleTableCaption"],
    },
    htmlSupport: {
      allow: [
        {
          name: "img",
          attributes: {
            "data-media-asset-id": true,
            src: true,
            alt: true,
            title: true,
            class: true,
            style: true,
            width: true,
            height: true,
          },
        },
        {
          name: /^(figure|figcaption|table|thead|tbody|tr|th|td|p|h2|h3|h4|ul|ol|li|blockquote|a|span|strong|em|u|s|i|b|br)$/,
          classes: true,
          styles: {
            "text-align": true,
            width: true,
            height: true,
            "margin-left": true,
            "margin-right": true,
            float: true,
          },
          attributes: {
            href: true,
            target: true,
            rel: true,
            colspan: true,
            rowspan: true,
          },
        },
      ],
    },
    damImagePicker: options.onPickDamImage,
  };
}

export default function ContentArticleCkEditor({
  value,
  onChange,
  disabled = false,
  placeholder,
  dir = "rtl",
  testId = "content-article-rich-editor",
  onPickDamImage,
}: ContentArticleCkEditorProps) {
  const resolvedPlaceholder =
    placeholder ?? (dir === "rtl" ? "متن مقاله را بنویسید…" : "Write article body…");
  const lastEmittedRef = useRef(sanitizeArticleRichHtml(value || ""));
  const editorRef = useRef<ClassicEditor | null>(null);
  const onChangeRef = useRef(onChange);
  const onPickRef = useRef(onPickDamImage);

  useEffect(() => {
    onChangeRef.current = onChange;
  }, [onChange]);

  useEffect(() => {
    onPickRef.current = onPickDamImage;
  }, [onPickDamImage]);

  const stablePicker = useMemo(
    () => () => {
      const picker = onPickRef.current;
      if (!picker) return Promise.resolve(null);
      return picker();
    },
    [],
  );

  const config = useMemo(
    () =>
      buildConfig({
        dir,
        placeholder: resolvedPlaceholder,
        onPickDamImage: onPickDamImage ? stablePicker : undefined,
      }),
    [dir, resolvedPlaceholder, onPickDamImage, stablePicker],
  );

  useEffect(() => {
    const editor = editorRef.current;
    if (!editor) return;
    const next = sanitizeArticleRichHtml(value || "");
    const current = sanitizeArticleRichHtml(editor.getData());
    if (next !== lastEmittedRef.current && next !== current) {
      editor.setData(next);
      lastEmittedRef.current = next;
    }
  }, [value]);

  return (
    <div
      className="content-article-ckeditor overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
      data-testid={testId}
      data-editor="ckeditor5"
      data-content-editor="article"
      data-dir={dir}
      dir={dir}
    >
      <div className="border-b border-gray-100 bg-slate-50 px-2 py-1 text-xs text-slate-500" data-testid={`${testId}-toolbar-shell`}>
        {dir === "rtl" ? "ویرایشگر بدنه مقاله" : "Article body editor"}
      </div>
      <div className="content-article-ckeditor-canvas min-h-[22rem]" data-testid={`${testId}-content`}>
        <CKEditor
          key={dir}
          editor={ClassicEditor}
          data={sanitizeArticleRichHtml(value || "")}
          disabled={disabled}
          config={config}
          onReady={(editor) => {
            editorRef.current = editor as ClassicEditor;
            lastEmittedRef.current = sanitizeArticleRichHtml(editor.getData());
            const editable = editor.ui.view.editable.element;
            if (editable) {
              editable.setAttribute("dir", dir);
              editable.setAttribute("data-testid", `${testId}-editable`);
            }
          }}
          onChange={(_event, editor) => {
            const html = sanitizeArticleRichHtml(editor.getData());
            lastEmittedRef.current = html;
            onChangeRef.current(html);
          }}
          onAfterDestroy={() => {
            editorRef.current = null;
          }}
        />
      </div>
    </div>
  );
}
