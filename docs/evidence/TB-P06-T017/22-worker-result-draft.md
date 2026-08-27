# 22 — Worker Result draft (TB-P06-T017)

Draft only — Worker posts the complete contract through Bridge after validate/commit/push. Fill SHA / test counts from final runtime if still placeholders.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P06-T017

Channel:
tooba-main

Bridge-UUID:
43fff592-fb07-476a-9485-c8a526d79a13

Worker:
tooba-worker-01

Predecessor:
9b4ff5c26981aa876565a6812da91bebafec169d

Status:
PASS

Summary:
Built real Story module (Domain/Application/Infrastructure, schema story, InitialStory migration) with Draft/Scheduled/Active/Expired/Disabled lifecycle; public GET /v1/storefront/stories and admin CRUD/enable/disable/schedule/reorder/items; StoreAlpha seed (موبایل fa, بازی locale-null video, English rail en). Host wired MapStoryEndpoints + migrate/seed. Frontend removed fake STORY_IMAGES; ported exact Shopeiva rail+modal (#E53935, Swiper, progress/tap RTL) with live story-api; AddStory omitted (admin creates); composition section stories still binds HomeStoriesSection; admin /admin/stories DataGrid. Evidence under docs/evidence/TB-P06-T017/.

Commit-Message:
feat connect live stories to Shopeiva UI [TB-P06-T017]

Commit-SHA:
see final runtime / CI commands

Evidence:
docs/evidence/TB-P06-T017/

Proof-Script:
scripts/prove-t06-t017-stories.mjs

Runtime:
Host http://127.0.0.1:5088 health ok; stories?locale=fa → 2 (موبایل, بازی); en includes English rail; fa excludes English rail; FE :3000; Shopeiva :3001

Readiness:
STORY_LIVE_WITH_EXACT_SHOPEIVA_UI

Not-Claimed:
PRODUCT_FULLY_READY

Shopeiva-Source:
FrontStarter/shopeiva stories.jsx + storyModal.jsx (NOT in monorepo); AddStory intentionally omitted

Zero-Redesign:
YES — exact chrome port + live data only

Tests:
StoryFoundationTests + story-api.test.ts + home-structure.guard — see final runtime / CI commands

END_TOOBA_WORKER_RESULT
```

`Worker PASS != Architect ACCEPT`.
