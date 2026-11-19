# TB-282-R11 COMPLETION LEDGER

| Gate | Method | Observed |
| --- | --- | --- |
| A admin shop delete replay | real AdminDeleteAsync EF | First delete + exact retry tombstone; occurrence SellerPartyId=shop; no conflict |
| A platform delete | AdminDeleteAsync | Replay after gone OK |
| A seller delete | SellerDeleteAsync | Own OK; foreign Missing; stale VersionStale; foreign action OccurrenceConflict |
| A list+detail delete UI | StoryLiveGrid onRequestDelete + detail Dialog | Cancel 0 DELETE; confirm durable envelope |
| A mounted delete | 282r11 mount | Cancel 0; unknown retains; replay same body; navigates off detail |
| B displayState | mounted Active+past end | «منقضی»; not «در ویترین» |
| B EndAt>StartAt | validators + domain | Strict >; equal refused |
| B requireEnd | parseOptionalPublicationWindow | Unchecked no-end refuses empty end |
| C preview | mount blob URL | story-slide-local-preview img blob: |
| C media recovery | URL-only seller slide | story-slide-media-recovery + mediaRequiredHint |
| C player | story-modal.tsx | ended listener retained |
| C Next build | npm run build | Compile OK; Html/404 prerender unrelated disclosed |
| Serve | Host5088 skip; Next3000 dev | health/ready/store-alpha; routes 200 |
| Zero | — | No migration; no live write |
