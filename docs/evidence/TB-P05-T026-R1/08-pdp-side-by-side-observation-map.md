# 08 — PDP side-by-side observation map (TB-P05-T026-R1)

No speculative PDP redesign in this Repair Task. Compare live captures `05-original-shopeiva-pdp-live.png` vs `06-current-tooba-pdp-live.png`.

| Area | Original Shopeiva | Current Tooba | Observed difference | Severity | User confirmation |
|---|---|---|---|---|---|
| Top 3-column block | Gallery / info / buy panel | Same structural seam (locked) | Accent blue vs Shopeiva red | MINOR | NO |
| Gallery | Swiper thumbnails | Live media binding | Largely aligned | MINOR | NO |
| Variants | Template variant chips | Live variant seam when present | Product-dependent | MINOR | NO |
| Buy panel | Template price/CTA | Offer-based amount from Host | Correct authority; styling blue | MINOR | NO |
| Sticky tabs | 6 tabs, sticky strip | Sticky tabs preserved (T017) | Structure aligned | NONE–MINOR | NO |
| Tab bodies | Distinct per tab | Distinct bodies bound live | No generic flatten | NONE | NO |
| Other sellers | Shopeiva block | Live when Host exposes | Honest empty when none | NONE | NO |
| Related products | Rail | Live related products | Aligned | MINOR | NO |
| Mobile | Responsive stack | Responsive | Not re-captured mobile in R1 | MINOR | NO |

## Runtime status

Both PDP URLs HTTP 200 at capture and after frontend build smoke.

**PDP visual changes in R1: NONE**

No user complaint recorded for PDP in Architect repair decision; observation only.
