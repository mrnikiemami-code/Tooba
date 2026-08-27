# 09 — Admin review in existing panel (TB-P06-T019-R1)

Approve / reject live in the **same** `/admin/stories` management surface — not a separate moderation app.

## In-panel behaviors (`StoryManagementScreen` + admin capabilities)

- Filter: all vs pending review (`Submitted`)
- Origin + seller owner columns when `showOrigin` / `showSellerOwner`
- Detail: review badge, approve / reject actions when `canReview`
- Reject prompts for reason (`STORY_COPY.rejectPrompt`); empty reason blocked client-side and server-side
- After Approved: schedule / activate using existing admin lifecycle controls

No visually separate review shell; seller submissions appear as rows in the existing Story list/editor.
