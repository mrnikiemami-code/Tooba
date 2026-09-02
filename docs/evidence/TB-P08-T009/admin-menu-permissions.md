# admin-menu-permissions

Content group (between Ops and Finance): مقالات، دسته‌بندی مقالات، نویسندگان — live, `content.view`.
Languages remain under System/Settings (not moved into Content).
Article list actions gated: create→`content.create`, edit/delete/archive→`content.edit`, publish/unpublish→`content.publish` (caps null ⇒ allow-all, same as nav).
Workspace EDIT gated by `content.edit`.
Backend Content endpoints remain `AdminPanelAccess` / tenant#view (catalog content.* codes exist for ACC UI; finer BE enforcement is established Admin convention, not redesigned here).
