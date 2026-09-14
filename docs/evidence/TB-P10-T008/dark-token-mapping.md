# TB-P10-T008 — Dark token mapping

Canonical `.dark` tokens: background `12 12 14`, surface `28 28 32`, elevated `44 44 50`, foreground `250 250 250`, muted `180 180 188`, border `72 72 80`. Status lightens for dark readability and stays off PaletteKey.

Brand `--color-primary*` stay inline from the curated registry (not remapped by `.dark`).

Leftover Tailwind neutrals (`bg-white`, `bg-gray-50`, `text-gray-900`, `border-gray-200`) remap once in `globals.css` under `html.dark`.
