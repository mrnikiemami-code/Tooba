# AntiPattern Scan — TB-P10-T022-R9

| Anti-pattern | Status |
|--------------|--------|
| Landing-only semantics | Rejected — Store Pages Home+Landing + rename |
| Multiple active Homes | Rejected — singleton HomePageId + atomic demotion |
| Home reset deleting data | Rejected — SetHome(null) selection only |
| Hardcoded language columns SeoTitleFa/En | Rejected — page-level Locale + translation fields |
| Duplicate SEO framework | Rejected — extended existing page SEO |
| Raw JSON structured-data editor | Rejected — typed builders only |
| SEO wizard step on every Section | Rejected — wizard remains type→appearance→source→settings→review |
| Noncanonical Landing route | Rejected — `/landing/{slug}` + legacy redirect |
| Catalog mutation during Home selection | Rejected |
| Simplified Admin table | Rejected — AppDataGrid orders-canonical |
| Polling/timeouts workarounds in product code | Rejected |

No TB-P10-T023.
