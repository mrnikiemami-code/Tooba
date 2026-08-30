# Assignment Level Rule

- Domain: `CatalogCategoryTreeRules.ProductAssignableLevel=3` + `AssignmentLevelInvalidErrorCode=catalog.category.assignment.level.invalid`
- Enforced in Assign/Additional/ReplacePrimary/Preview + create Product primary
- Host maps Persian level message → stable machine code; FE `admin-error-map` localizes (no raw Bad Request)
- Live proof: L1/L2 display + primary migration + workspace assign all return `catalog.category.assignment.level.invalid`
