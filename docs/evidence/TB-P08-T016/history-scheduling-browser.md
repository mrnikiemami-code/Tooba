# TB-P08-T016 — History / Scheduling

## Code / contract (worker)

- History timeline with human FA/EN labels; distinct published/unpublished/republished events
- Schedule field: fa Jalali / en Gregorian UX; UTC persistence
- Future schedule not publicly visible before due (workflow tests)
- FE history mapping + publish date field wiring asserted

## Browser / API smoke (parent)

- [x] Publish → Unpublish → Republish yields three distinct History events  
  API smoke: publish / unpublish / republish / **history** PASS (shell agent). Distinct History UI labels not separately screenshot-logged.
- [ ] Actor + timestamp visible where available  
  Not separately logged in browser this session.
- [ ] fa Jalali / en Gregorian inputs; no ugly mixed native Gregorian in fa  
  Not browser-exercised this session.
- [x] Scheduled article not public before due time  
  API smoke: schedule via `publishDate` **PASS**.
