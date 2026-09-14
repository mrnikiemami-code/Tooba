# TB-P10-T007 — Status color isolation

`globals.css` danger `185 28 28`, success `21 128 61`, warning `180 83 9` unchanged. Appearance CSS vars still omit status tokens.

Shipping/checkout validation uses `text-red-600`. Story report Flag uses `text-danger`. Pending hide/cancel stays `text-red-600`.

Palette switch must not change those classes. Host/FE appearance tests still assert no `--color-danger` in brand vars.
