# Preview Placeholder Contract

PreviewPlaceholderSurface is render-only (no DB writes).
Gated by preview && previewSource==='store' && config.previewPlaceholder.
Published Storefront calls do not pass Store preview flags → placeholders never publish.

