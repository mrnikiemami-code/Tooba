# Invoice display — TB-P09-T014-R1

No invoice redesign.

Print/HTML:

- `تعداد اقلام` = integer header count.
- `جمع مقدار` = `QuantityDisplay` decimal without trailing zeros, only when all lines share the same unit snapshot.
- Mixed units omit `جمع مقدار` so a shared-unit total is not implied.
- No hardcoded unit on the header total.

Detail KPI `تعداد اقلام` uses integer `detail.lineCount`.
