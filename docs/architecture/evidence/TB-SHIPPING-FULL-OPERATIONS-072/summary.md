# TB-SHIPPING-FULL-OPERATIONS-072 — Evidence Index

Mode: `BOUNDED_EXISTING_ADMIN_WEEKLY_SCHEDULE_EDITOR`
Status: **PASS** (Worker) — `Worker PASS != Architect ACCEPT`
Base-SHA: `5ec3a37e1dbdfab723d093a3ec6e9c4edf18975c`
Code-SHA = Evidence-SHA: `40d953be5ee107cefc006de38587c918c000b265`

## Deliverable

One focused Persian/RTL dialog «برنامه کاری ارسال» inside the existing `/admin/shipping-services`
screen, opened from the existing service row next to «نرخ و پوشش», genuinely wired to the shipped 071
operating-schedule endpoints. No new route, no backend change, no migration, no new permission.

## Evidence files

| File | What it proves |
| --- | --- |
| `log-browser-proof.txt` | The full live interaction transcript on the existing Development environment: read/display of the saved tipax/standard Sunday fixture, edit/update with read-after-write, active toggle, validation refusals, option isolation, 401 handling and the restored final state — with the captured wire bodies. |
| `ui-01-schedule-dialog-open.png` | Dialog opened from the tipax row; saved schedule and editor visible. |
| `ui-02-overlap-refused.png` | Overlapping Sunday windows refused locally. |
| `ui-03-overlap-error-fa.png` | The field-adjacent Persian overlap message. |
| `ui-04-capacity-restored.png` | Capacity edited through the UI, saved, re-read and restored to 25. |
| `ui-05-fixture-restored.png` | Final state: two Sunday windows, capacity 25, active. |
| `ui-06-unauthorized-denied.png` | Visible denied state when the admin actor header is absent (no bypass). |
| `probe-schedules.ps1` | Read-only isolation probe script (GET only; never writes). |
| `log-isolation-probe.txt` | Probe output: tipax 200 with the single standard schedule; post / snapp_courier / store_courier / in_person all `schedules: []`; tipax without actor header 401 `admin.actor.missing`; Development provenance descriptor. |
| `log-focused-tests.txt` | 23/23 pass for the new model + API tests. |
| `log-existing-shipping-tests.txt` | 36/36 pass for the existing shipping frontend tests. |
| `log-architecture-guards.txt` | 7/8 architecture guards pass; the one failure is the pre-existing shrink-only size baseline over five files this task does not touch. |
| `RESULT.bridge.txt` | The exact result string posted to Bridge. |
| `post-result.js` | The poster used for `/api/results`, `/api/tasks/{row}/complete` and the worker heartbeat. |
| `bridge-task-row.txt` | The Bridge task row id claimed for this task. |

## Verified live values

```text
target        : existing Development frontend 127.0.0.1:3000 + host 127.0.0.1:5088 (no restart)
tenant / DB   : store-alpha / tooba_alpha (disposable Development data)
provenance    : GET /__platform-commerce => deploymentId dev-singlestore, resolvedHost 127.0.0.1
actor         : 01a036c2-970e-7000-8eb7-94bf5cc2d8db
scheduleId    : 01a13deb-cd7c-7000-a11e-641338237a1a
identity      : tipax / standard (optionCode standard)
timezone      : Asia/Tehran
windows       : weekday 7 (یکشنبه) 13:00:00-14:00:00 ; weekday 7 16:30:00-18:00:00 capacity 25
capacity total: 25 (configured only — not enforced)
isActive      : true (restored)
```

## Not claimed here

- Capacity enforcement, reservation counting or locking: **not implemented**.
- Customer delivery promise / slot / order / checkout change: **not touched**.
- Architect ACCEPT: **not claimed**; the Worker stops for Architect review.
