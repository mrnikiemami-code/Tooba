# Menu model

StoreMenu: MenuId, Title, Locale, MenuKey, IsEnabled, timestamps.
StoreMenuItem: MenuItemId, MenuId, ParentMenuItemId, Label, LinkType, TargetId, ExternalUrl, SortOrder, IsEnabled.

Link types: Home, LandingPage, Product, Category, Brand, Article, External, Group.
Max depth 3. Same-menu parent. Cycle rejected. No HTML/CSS/JS.
