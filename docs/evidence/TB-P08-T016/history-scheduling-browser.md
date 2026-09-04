# TB-P08-T016 — History / Scheduling

## Code / contract (worker)

- History timeline with human FA/EN labels; distinct published/unpublished/republished events
- Schedule field: fa Jalali / en Gregorian UX; UTC persistence
- Future schedule not publicly visible before due (workflow tests)
- FE history mapping + publish date field wiring asserted

## Browser (parent)

- [ ] Publish → Unpublish → Republish yields three distinct History events
- [ ] Actor + timestamp visible where available
- [ ] fa Jalali / en Gregorian inputs; no ugly mixed native Gregorian in fa
- [ ] Scheduled article not public before due time
