# 01 — Runtime start before work (TB-P05-T025)

Predecessor verified: `fa1a44ca1cb47dd838f13756a3508f13b645cf83` on `main`, `HEAD == origin/main`.

## Backend

| Field | Value |
|---|---|
| Command | existing `Tooba.Host` already listening (started earlier for P05 evidence) |
| Process | `Tooba.Host` PID **3404** |
| URL | `http://127.0.0.1:5088` |
| Health | `GET /health` → `{"status":"ok"}` (200) |

## Frontend

| Field | Value |
|---|---|
| Initial process | `node` PID **6596** on port **3000** |
| Issue observed | intermittent `500` on `/` due to Next RSC/dev-cache (`SegmentViewNode` / `__webpack_modules__`) after long-lived HMR + prior `next build` |
| Repair action (runtime dependency) | stop FE, clear `src/frontend/.next`, restart `npm run dev -- --hostname 127.0.0.1 --port 3000` |
| URL | `http://127.0.0.1:3000` |

## Dependencies

- Host on `5088` (default `TOOBA_HOST_ORIGIN`)
- Next rewrites `/v1/*` → Host
- No new ports invented
