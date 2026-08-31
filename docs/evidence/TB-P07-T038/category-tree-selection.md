# Category tree selection (TB-P07-T038)

- Canonical selectedKeys from route `categoryId` when node exists in tree; otherwise empty (no stale selection).
- Ancestor expand via `collectAncestorIds` on categoryId/flatNodes.
- `AppCategoryTree` scrolls `.ant-tree-treenode-selected` into view after selection/expand.
- Search clear keeps route selection; invalid/missing Category clears tree selection.
