# Permissions

`AdminPanelAccess.RequireAuthorizedAsync` on all completeness endpoints. Effective access via `AdminOrderOperationsComposer.Has`: notes write `order.handle`; notes/history/invoice/receipt read `order.view`. Legacy admin without ops-family grants remains allowed.
