# Tooba — Workspace Interaction Patterns

Status:

```text
Foundation implemented — blueprints only, not domain workspace ACCEPT
```

Task:

```text
TB-P04-T004
```

A Workspace is a task-oriented, multi-domain operational surface. It is not a CRUD screen per backend module. Backend/module boundary is not a UI boundary.

## Shell

`WorkspaceShell` (`src/frontend/design-system/workspace/`) composes header, breadcrumbs, status strip, command hierarchy, section navigation, summary, main panel, inspector/activity/audit, unsaved-change guard, loading/empty/error/permission/conflict, and compact mobile navigation.

## Actions

Primary, secondary, destructive, overflow, contextual. Each action carries permission (`allowed` / `denied` / `hidden`), busy, and optional confirmation. Components never call SpiceDB.

## Navigation

Section ids are serializable (`serializeWorkspaceNavigation`). Deep links belong to a feature adapter, not the primitive.

## Edit / dirty / conflict

Dirty sections are a `Set<string>`. Navigation to another section while dirty is blocked. Conflict is a visible reload/review state. Silent overwrite is forbidden.

## Permissions

`resolveWorkspaceAction` maps canView/canEdit/canExecute onto action permission. Read-only is a shell flag.

## Activity vs audit

Activity = business timeline. Audit = technical/business audit events. They are separate lists.

## Inspector

Desktop aside; mobile Drawer/Sheet.

## Data Grid

Embedded grid is a composition pattern: list → row → workspace, workspace → related grid, return state via `serializeMasterDetailReturn`. No domain APIs in this task.

## Mobile

Compact header, section `<select>`, sticky primary action, inspector drawer. Not a scaled-down desktop dump.

## Accessibility

Header/nav landmarks, tablist for sections, dialog/drawer focus via existing overlay primitives, `min-h-11` controls, `aria-busy` on loading.

## i18n

`WorkspaceMessages` catalogs (fa/en). ErrorState retry uses `retryLabel` (bounded fix of hardcoded Persian).

## Blueprints

Product/Order/Seller/Customer/Content/Tenant/Return are documented under `docs/evidence/TB-P04-T004/`. They are not implemented screens.

## Showcase

`/design-system` remains `robots: noindex`. Synthetic scenarios only.
