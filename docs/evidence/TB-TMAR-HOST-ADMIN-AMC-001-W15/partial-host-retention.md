# Partial Host retention — W15

## Host/Admin count

52 → 52 (ProductWorkspace* files retained)

## ProductWorkspaceEndpoints.cs

- Removed history route registration + GetHistoryAsync
- Retained all other workspace routes

## ProductWorkspaceComposer.cs

- Removed GetHistoryPageAsync + ToHistoryItemView
- Retained BuildHistoryShellListsAsync for aggregate GetAsync Activity/Audit
- Retained AppendProductHistoryAsync call sites on other mutations

## ProductWorkspaceModels.cs

- Removed ProductHistoryPageView + ProductHistoryItemView
- Retained ProductHistoryItem + ProductWorkspaceView.Activity/Audit

## StoreAppearance

Deferred (not moved).
