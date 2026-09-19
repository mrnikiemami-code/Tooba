# Anti-Pattern Scan

| Anti-pattern | Status |
|---|---|
| Propose `Offer.IsAmazing` | **Rejected** |
| Many promotion booleans | **Rejected** |
| Duplicate pricing tables | **Rejected** |
| Duplicate inventory tables | **Rejected** |
| Store countdown every second | **Rejected** |
| Store discount percent when derivable | **Rejected as write truth** |
| New table without proving no equivalent | **Checked** — no merchandising campaign equivalent; checkout Promotion ≠ Amazing rail |
| New enum/source per future campaign type | **Avoid** — use TypeCode + one Builder source |
| Hardcoded Persian locale | **Rejected** |
| Section-specific language ≠ Page | **Rejected** |
| Full product scan for Amazing | **Rejected** |
| Raw GUID primary UI | **Rejected** |
| Massive rule engine phase 1 | **Rejected** |
| Schema changes during this audit | **None made** |
| Hidden implementation in audit | **None** |
| Unrelated cleanup/refactor | **None** |
| P11 work | **Not executed** |
| Overwriting user fixes | **Preserved** (18ca10c9 ancestor; tip polish commits untouched) |
