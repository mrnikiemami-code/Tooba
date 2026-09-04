"use client";

/**
 * CKEditor 5 Article body editor (client-only; loaded via dynamic import).
 * Self-hosted open-source plugins only — DAM image/file/video insert via parent callbacks.
 * Cloud media adapters and base64 upload adapters are intentionally unused.
 */

import { useEffect, useMemo, useRef, useState } from "react";
import { CKEditor } from "@ckeditor/ckeditor5-react";
import {
  Alignment,
  BlockQuote,
  Bold,
  ClassicEditor,
  Essentials,
  FindAndReplace,
  FontBackgroundColor,
  FontColor,
  FontFamily,
  FontSize,
  GeneralHtmlSupport,
  Heading,
  Highlight,
  HorizontalLine,
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
  RemoveFormat,
  SpecialCharacters,
  SpecialCharactersEssentials,
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
import { sanitizeArticleRichHtml } from "./article-rich-html.ts";

export type DamImagePick = {
  mediaAssetId: string;
  alt?: string;
  title?: string;
} | null;

export type DamFilePick = {
  mediaAssetId: string;
  fileName?: string;
  title?: string;
} | null;

export type DamVideoPick = {
  mediaAssetId: string;
  fileName?: string;
  title?: string;
} | null;

export type ContentArticleCkEditorProps = {
  value: string;
  onChange: (html: string) => void;
  disabled?: boolean;
  placeholder?: string;
  dir?: "rtl" | "ltr";
  testId?: string;
  className?: string;
  onPickDamImage?: () => Promise<DamImagePick>;
  onPickDamFile?: () => Promise<DamFilePick>;
  onPickDamVideo?: () => Promise<DamVideoPick>;
};

type DamImagePickerConfig = () => Promise<DamImagePick>;
type DamFilePickerConfig = () => Promise<DamFilePick>;
type DamVideoPickerConfig = () => Promise<DamVideoPick>;

declare module "ckeditor5" {
  interface EditorConfig {
    damImagePicker?: DamImagePickerConfig;
    damFilePicker?: DamFilePickerConfig;
    damVideoPicker?: DamVideoPickerConfig;
  }
}

function damStorefrontSrc(mediaAssetId: string): string {
  const id = mediaAssetId.trim();
  return `/v1/storefront/media/${id}`;
}

function escapeAttr(value: string): string {
  return value.replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");
}

function escapeHtmlText(value: string): string {
  return value.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

function insertDamImageHtml(editor: Editor, picked: NonNullable<DamImagePick>): void {
  const src = damStorefrontSrc(picked.mediaAssetId);
  const alt = escapeAttr(picked.alt ?? "");
  const titleAttr = picked.title ? ` title="${escapeAttr(picked.title)}"` : "";
  const html = `<figure class="image"><img class="article-dam-image" src="${src}" alt="${alt}" data-media-asset-id="${picked.mediaAssetId}"${titleAttr} /></figure>`;
  const viewFragment = editor.data.processor.toView(html);
  const modelFragment = editor.data.toModel(viewFragment);
  editor.model.insertContent(modelFragment);
}

function insertDamFileHtml(editor: Editor, picked: NonNullable<DamFilePick>): void {
  const src = damStorefrontSrc(picked.mediaAssetId);
  const name = escapeHtmlText(picked.fileName || picked.title || "file");
  const html = `<p><a class="article-dam-file" href="${src}" data-media-asset-id="${picked.mediaAssetId}" target="_blank" rel="noopener noreferrer">${name}</a></p>`;
  const viewFragment = editor.data.processor.toView(html);
  const modelFragment = editor.data.toModel(viewFragment);
  editor.model.insertContent(modelFragment);
}

function insertDamVideoHtml(editor: Editor, picked: NonNullable<DamVideoPick>): void {
  const src = damStorefrontSrc(picked.mediaAssetId);
  const html = `<figure class="article-dam-video"><video class="article-dam-video" controls preload="metadata" src="${src}" data-media-asset-id="${picked.mediaAssetId}"></video></figure>`;
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
        const picker = editor.config.get("damImagePicker") as DamImagePickerConfig | undefined;
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

class DamFileInsert extends Plugin {
  public static get pluginName() {
    return "DamFileInsert" as const;
  }

  public init(): void {
    const editor = this.editor;
    editor.ui.componentFactory.add("damFile", (locale) => {
      const view = new ButtonView(locale);
      const isRtl = locale.uiLanguageDirection === "rtl";
      view.set({
        label: isRtl ? "افزودن فایل" : "Insert file",
        tooltip: true,
        withText: true,
        class: "ck-dam-file-button",
      });

      view.on("execute", () => {
        const picker = editor.config.get("damFilePicker") as DamFilePickerConfig | undefined;
        if (!picker) return;
        void picker().then((picked) => {
          if (!picked?.mediaAssetId) return;
          insertDamFileHtml(editor, picked);
          editor.editing.view.focus();
        });
      });

      return view;
    });
  }
}

class DamVideoInsert extends Plugin {
  public static get pluginName() {
    return "DamVideoInsert" as const;
  }

  public init(): void {
    const editor = this.editor;
    editor.ui.componentFactory.add("damVideo", (locale) => {
      const view = new ButtonView(locale);
      const isRtl = locale.uiLanguageDirection === "rtl";
      view.set({
        label: isRtl ? "افزودن ویدیو" : "Insert video",
        tooltip: true,
        withText: true,
        class: "ck-dam-video-button",
      });

      view.on("execute", () => {
        const picker = editor.config.get("damVideoPicker") as DamVideoPickerConfig | undefined;
        if (!picker) return;
        void picker().then((picked) => {
          if (!picked?.mediaAssetId) return;
          insertDamVideoHtml(editor, picked);
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
  onPickDamImage: () => Promise<DamImagePick>;
  onPickDamFile: () => Promise<DamFilePick>;
  onPickDamVideo: () => Promise<DamVideoPick>;
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
      RemoveFormat,
      FontFamily,
      FontSize,
      FontColor,
      FontBackgroundColor,
      Highlight,
      List,
      Indent,
      IndentBlock,
      BlockQuote,
      Alignment,
      HorizontalLine,
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
      SpecialCharacters,
      SpecialCharactersEssentials,
      Undo,
      GeneralHtmlSupport,
      DamImageInsert,
      DamFileInsert,
      DamVideoInsert,
    ],
    toolbar: {
      items: [
        "heading",
        "|",
        "bold",
        "italic",
        "underline",
        "strikethrough",
        "removeFormat",
        "|",
        "fontFamily",
        "fontSize",
        "fontColor",
        "fontBackgroundColor",
        "highlight",
        "|",
        "bulletedList",
        "numberedList",
        "outdent",
        "indent",
        "|",
        "blockQuote",
        "alignment",
        "horizontalLine",
        "|",
        "link",
        "insertTable",
        "|",
        "damImage",
        "damFile",
        "damVideo",
        "|",
        "findAndReplace",
        "specialCharacters",
        "|",
        "undo",
        "redo",
      ],
      // Group overflow on narrow widths so the full toolbar remains usable.
      shouldNotGroupWhenFull: false,
    },
    heading: {
      options: [
        { model: "paragraph", title: isRtl ? "پاراگراف" : "Paragraph", class: "ck-heading_paragraph" },
        { model: "heading2", view: "h2", title: "H2", class: "ck-heading_heading2" },
        { model: "heading3", view: "h3", title: "H3", class: "ck-heading_heading3" },
        { model: "heading4", view: "h4", title: "H4", class: "ck-heading_heading4" },
      ],
    },
    fontFamily: {
      options: [
        "default",
        "Arial, Helvetica, sans-serif",
        "Tahoma, Geneva, sans-serif",
        "Verdana, Geneva, sans-serif",
        "Times New Roman, Times, serif",
        "Georgia, serif",
        "Courier New, Courier, monospace",
        "B Nazanin, Tahoma, Arial, sans-serif",
        "Vazirmatn, Tahoma, Arial, sans-serif",
      ],
    },
    fontSize: {
      options: ["default", 12, 14, 16, 18, 20, 24, 28],
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
          name: "video",
          attributes: {
            "data-media-asset-id": true,
            src: true,
            class: true,
            controls: true,
            preload: true,
          },
          classes: true,
        },
        {
          name: "source",
          attributes: {
            src: true,
          },
        },
        {
          name: "hr",
          classes: true,
        },
        {
          name: /^(figure|figcaption|table|thead|tbody|tr|th|td|p|h2|h3|h4|ul|ol|li|blockquote|a|span|strong|em|u|s|i|b|br)$/,
          classes: true,
          styles: {
            "text-align": true,
            "font-family": true,
            "font-size": true,
            color: true,
            "background-color": true,
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
            "data-media-asset-id": true,
            class: true,
            title: true,
          },
        },
      ],
    },
    damImagePicker: options.onPickDamImage,
    damFilePicker: options.onPickDamFile,
    damVideoPicker: options.onPickDamVideo,
  };
}

export default function ContentArticleCkEditor({
  value,
  onChange,
  disabled = false,
  placeholder,
  dir = "rtl",
  testId = "content-article-rich-editor",
  className,
  onPickDamImage,
  onPickDamFile,
  onPickDamVideo,
}: ContentArticleCkEditorProps) {
  const [initError, setInitError] = useState<string | null>(null);
  const resolvedPlaceholder =
    placeholder ?? (dir === "rtl" ? "متن مقاله را بنویسید…" : "Write article body…");
  const lastEmittedRef = useRef(sanitizeArticleRichHtml(value || ""));
  const editorRef = useRef<ClassicEditor | null>(null);
  const onChangeRef = useRef(onChange);
  const onPickImageRef = useRef(onPickDamImage);
  const onPickFileRef = useRef(onPickDamFile);
  const onPickVideoRef = useRef(onPickDamVideo);

  useEffect(() => {
    onChangeRef.current = onChange;
  }, [onChange]);

  useEffect(() => {
    onPickImageRef.current = onPickDamImage;
  }, [onPickDamImage]);

  useEffect(() => {
    onPickFileRef.current = onPickDamFile;
  }, [onPickDamFile]);

  useEffect(() => {
    onPickVideoRef.current = onPickDamVideo;
  }, [onPickDamVideo]);

  const stableImagePicker = useMemo(
    () => () => {
      const picker = onPickImageRef.current;
      if (!picker) return Promise.resolve(null);
      return picker();
    },
    [],
  );

  const stableFilePicker = useMemo(
    () => () => {
      const picker = onPickFileRef.current;
      if (!picker) return Promise.resolve(null);
      return picker();
    },
    [],
  );

  const stableVideoPicker = useMemo(
    () => () => {
      const picker = onPickVideoRef.current;
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
        onPickDamImage: stableImagePicker,
        onPickDamFile: stableFilePicker,
        onPickDamVideo: stableVideoPicker,
      }),
    [dir, resolvedPlaceholder, stableImagePicker, stableFilePicker, stableVideoPicker],
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
      <div
        className={
          className
            ? `content-article-ckeditor-canvas ${className}`
            : "content-article-ckeditor-canvas min-h-[22rem]"
        }
        data-testid={`${testId}-content`}
      >
        {initError ? (
          <p className="border-b border-red-100 bg-red-50 px-3 py-2 text-sm text-red-700" data-testid={`${testId}-init-error`} role="alert">
            {dir === "rtl" ? `ویرایشگر آماده نشد: ${initError}` : `Editor failed to start: ${initError}`}
          </p>
        ) : null}
        <CKEditor
          key={dir}
          editor={ClassicEditor}
          data={sanitizeArticleRichHtml(value || "")}
          disabled={disabled}
          config={config}
          onReady={(editor) => {
            setInitError(null);
            editorRef.current = editor as ClassicEditor;
            lastEmittedRef.current = sanitizeArticleRichHtml(editor.getData());
            const editable = editor.ui.view.editable.element;
            if (editable) {
              editable.setAttribute("dir", dir);
              editable.setAttribute("data-testid", `${testId}-editable`);
            }
          }}
          onError={(error) => {
            const message =
              error instanceof Error
                ? error.message
                : typeof error === "string"
                  ? error
                  : "unknown editor error";
            setInitError(message);
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
